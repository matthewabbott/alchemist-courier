import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ErrorCode,
  ListToolsRequestSchema,
  McpError,
} from "@modelcontextprotocol/sdk/types.js";
import dotenv from "dotenv";
import fs from "fs";
import path from "path";
import { createHash } from "crypto";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Load .env from project root
const projectRoot = path.resolve(__dirname, "../..");
dotenv.config({ path: path.join(projectRoot, ".env") });

const API_BASE = "https://api.retrodiffusion.ai/v1";
const GENERATED_DIR = path.join(projectRoot, "art", "generated");
const STYLES_FILE = path.join(projectRoot, "art", "retrodiffusion-styles.json");
const CREDIT_THRESHOLD = 1.0;

class RetroDiffusionServer {
  constructor() {
    this.creditExhausted = false;
    this.lastKnownBalance = null;

    this.server = new Server(
      { name: "retrodiffusion-mcp", version: "1.0.0" },
      { capabilities: { tools: {} } }
    );

    this.setupToolHandlers();
    this.server.onerror = (error) =>
      console.error("[RetroDiffusion MCP Error]", error);
  }

  getApiKey() {
    const key = process.env.RETRODIFFUSION_API_KEY;
    if (!key) {
      throw new McpError(
        ErrorCode.InternalError,
        "RETRODIFFUSION_API_KEY not set. Add it to .env in the project root."
      );
    }
    return key;
  }

  async apiPost(endpoint, body) {
    const resp = await fetch(`${API_BASE}${endpoint}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-RD-Token": this.getApiKey(),
      },
      body: JSON.stringify(body),
    });

    if (!resp.ok) {
      const text = await resp.text();
      throw new McpError(
        ErrorCode.InternalError,
        `RetroDiffusion API error (${resp.status}): ${text}`
      );
    }

    return resp.json();
  }

  checkBalance(data) {
    const balance = data.remaining_balance ?? data.remainingBalance;
    if (balance !== undefined) {
      this.lastKnownBalance = balance;
      if (balance < CREDIT_THRESHOLD) {
        this.creditExhausted = true;
      }
    }
  }

  ensureCreditsAvailable() {
    if (this.creditExhausted) {
      return {
        content: [
          {
            type: "text",
            text: `Credit balance low (remaining: ${this.lastKnownBalance}). Refusing further generation. Please top up your account.`,
          },
        ],
        isError: true,
      };
    }
    return null;
  }

  setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: "generate_sprite",
          description:
            "Generate pixel art sprite(s) via RetroDiffusion API. Saves to art/generated/ with metadata sidecar.",
          inputSchema: {
            type: "object",
            properties: {
              prompt: {
                type: "string",
                description: "Text prompt describing the sprite to generate",
              },
              width: {
                type: "number",
                description: "Image width in pixels (e.g. 32, 64)",
              },
              height: {
                type: "number",
                description: "Image height in pixels (e.g. 32, 64)",
              },
              style: {
                type: "string",
                description:
                  "Prompt style identifier (e.g. 'pixel_art_sprite')",
              },
              seed: {
                type: "number",
                description: "Optional random seed for reproducibility",
              },
              num_images: {
                type: "number",
                description: "Number of images to generate (default: 1)",
              },
              remove_bg: {
                type: "boolean",
                description:
                  "Whether to remove background from generated image",
              },
            },
            required: ["prompt", "width", "height", "style"],
          },
        },
        {
          name: "list_styles",
          description:
            "List available RetroDiffusion prompt_style identifiers grouped by model.",
          inputSchema: {
            type: "object",
            properties: {
              model: {
                type: "string",
                description:
                  "Optional: filter to a specific model (RD_PRO, RD_FAST, RD_PLUS)",
              },
            },
            required: [],
          },
        },
        {
          name: "create_custom_style",
          description:
            "Create a custom RetroDiffusion style from reference images. Saves style_id to art/retrodiffusion-styles.json.",
          inputSchema: {
            type: "object",
            properties: {
              name: {
                type: "string",
                description: "Name for the custom style",
              },
              reference_image_paths: {
                type: "array",
                items: { type: "string" },
                description:
                  "Array of file paths to reference images (will be base64-encoded)",
              },
            },
            required: ["name", "reference_image_paths"],
          },
        },
      ],
    }));

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      switch (name) {
        case "generate_sprite":
          return this.handleGenerateSprite(args);
        case "list_styles":
          return this.handleListStyles(args);
        case "create_custom_style":
          return this.handleCreateCustomStyle(args);
        default:
          throw new McpError(
            ErrorCode.MethodNotFound,
            `Unknown tool: ${name}`
          );
      }
    });
  }

  async handleGenerateSprite(args) {
    const blocked = this.ensureCreditsAvailable();
    if (blocked) return blocked;

    const { prompt, width, height, style, seed, num_images, remove_bg } = args;

    const body = {
      prompt,
      width,
      height,
      prompt_style: style,
      ...(seed !== undefined && { seed }),
      ...(num_images !== undefined && { num_images }),
      ...(remove_bg !== undefined && { remove_bg }),
    };

    const data = await this.apiPost("/inferences", body);
    this.checkBalance(data);

    // Credit guard after API call
    const postBlocked = this.ensureCreditsAvailable();

    // Ensure output directory exists
    fs.mkdirSync(GENERATED_DIR, { recursive: true });

    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const promptHash = createHash("md5")
      .update(prompt)
      .digest("hex")
      .slice(0, 8);
    const savedFiles = [];

    const images = data.base64_images || data.images || [];
    for (let i = 0; i < images.length; i++) {
      const imgData = images[i];
      const suffix = images.length > 1 ? `-${i}` : "";
      const baseName = `${timestamp}-${promptHash}${suffix}`;
      const pngPath = path.join(GENERATED_DIR, `${baseName}.png`);
      const metaPath = path.join(GENERATED_DIR, `${baseName}.json`);

      // Write image (base64 -> binary)
      const imgBuffer = Buffer.from(imgData, "base64");
      fs.writeFileSync(pngPath, imgBuffer);

      // Write metadata sidecar
      const metadata = {
        prompt,
        style,
        seed: data.seed ?? seed ?? null,
        model: data.model ?? null,
        width,
        height,
        cost: data.cost ?? null,
        remaining_balance: this.lastKnownBalance,
        generated_at: new Date().toISOString(),
      };
      fs.writeFileSync(metaPath, JSON.stringify(metadata, null, 2));

      savedFiles.push(pngPath);
    }

    const resultLines = [
      `Generated ${savedFiles.length} image(s):`,
      ...savedFiles.map((f) => `  ${f}`),
      `Remaining balance: ${this.lastKnownBalance ?? "unknown"}`,
    ];

    if (postBlocked) {
      resultLines.push(
        "",
        `WARNING: ${postBlocked.content[0].text}`
      );
    }

    return {
      content: [{ type: "text", text: resultLines.join("\n") }],
    };
  }

  async handleListStyles(args) {
    // Known RetroDiffusion styles grouped by model
    // The API doesn't have a list endpoint, so we maintain a static list
    // that can be updated manually as new styles become available.
    const styles = {
      RD_FAST: [
        "pixel_art_sprite",
        "pixel_art_landscape",
        "chibi",
        "anime",
        "realistic",
        "comic",
        "watercolor",
        "oil_painting",
      ],
      RD_PRO: [
        "pixel_art_sprite",
        "pixel_art_landscape",
        "chibi",
        "anime",
        "realistic",
        "comic",
        "watercolor",
        "oil_painting",
        "concept_art",
        "isometric",
      ],
      RD_PLUS: [
        "pixel_art_sprite",
        "pixel_art_landscape",
        "chibi",
        "anime",
        "realistic",
        "comic",
        "watercolor",
        "oil_painting",
        "concept_art",
        "isometric",
        "3d_render",
      ],
    };

    const { model } = args || {};
    let result;
    if (model && styles[model]) {
      result = { [model]: styles[model] };
    } else if (model && !styles[model]) {
      return {
        content: [
          {
            type: "text",
            text: `Unknown model: ${model}. Available: ${Object.keys(styles).join(", ")}`,
          },
        ],
        isError: true,
      };
    } else {
      result = styles;
    }

    // Also include any custom styles
    let customStyles = [];
    if (fs.existsSync(STYLES_FILE)) {
      try {
        customStyles = JSON.parse(fs.readFileSync(STYLES_FILE, "utf-8"));
      } catch {
        // ignore parse errors
      }
    }

    const output = {
      built_in: result,
      ...(customStyles.length > 0 && { custom: customStyles }),
    };

    return {
      content: [
        { type: "text", text: JSON.stringify(output, null, 2) },
      ],
    };
  }

  async handleCreateCustomStyle(args) {
    const blocked = this.ensureCreditsAvailable();
    if (blocked) return blocked;

    const { name, reference_image_paths } = args;

    // Base64-encode reference images
    const referenceImages = [];
    for (const imgPath of reference_image_paths) {
      const absPath = path.isAbsolute(imgPath)
        ? imgPath
        : path.join(projectRoot, imgPath);
      if (!fs.existsSync(absPath)) {
        return {
          content: [
            {
              type: "text",
              text: `Reference image not found: ${absPath}`,
            },
          ],
          isError: true,
        };
      }
      const imgBuffer = fs.readFileSync(absPath);
      referenceImages.push(imgBuffer.toString("base64"));
    }

    const data = await this.apiPost("/styles", {
      name,
      reference_images: referenceImages,
    });
    this.checkBalance(data);

    const styleId = data.style_id || data.id;

    // Save to styles file
    let existingStyles = [];
    if (fs.existsSync(STYLES_FILE)) {
      try {
        existingStyles = JSON.parse(fs.readFileSync(STYLES_FILE, "utf-8"));
      } catch {
        existingStyles = [];
      }
    }

    const styleDir = path.dirname(STYLES_FILE);
    fs.mkdirSync(styleDir, { recursive: true });

    existingStyles.push({
      name,
      style_id: styleId,
      reference_images: reference_image_paths,
      created_at: new Date().toISOString(),
    });

    fs.writeFileSync(STYLES_FILE, JSON.stringify(existingStyles, null, 2));

    return {
      content: [
        {
          type: "text",
          text: `Custom style created:\n  Name: ${name}\n  Style ID: ${styleId}\n  Reference images: ${reference_image_paths.length}\n  Saved to: ${STYLES_FILE}\n  Remaining balance: ${this.lastKnownBalance ?? "unknown"}`,
        },
      ],
    };
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error("RetroDiffusion MCP server running on stdio");
  }
}

const server = new RetroDiffusionServer();
server.run().catch((error) => {
  console.error("Failed to run server:", error);
  process.exit(1);
});
