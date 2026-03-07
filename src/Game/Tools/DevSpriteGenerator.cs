using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Development tool that procedurally generates placeholder sprite sheet PNGs
/// for all ReagentCatalog entries using Godot's Image API. Each sheet has
/// 7 state rows x 4 frame columns at 32x32 per cell.
/// Outputs to art/sprites/reagents/ plus sprite_specs.json.
/// </summary>
public partial class DevSpriteGenerator : Control
{
    private const int CellSize = 32;
    private const int MaxFrames = 4;
    private const int TotalRows = 7;
    private const int SheetWidth = MaxFrames * CellSize;  // 128
    private const int SheetHeight = TotalRows * CellSize;  // 224

    private Label _statusLabel = null!;
    private Label _progressLabel = null!;
    private int _generated;
    private int _total;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = new Color(0.12f, 0.12f, 0.15f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 12);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);

        var title = new Label { Text = "Dev Sprite Generator" };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        _statusLabel = new Label { Text = "Generating..." };
        _statusLabel.AddThemeFontSizeOverride("font_size", 16);
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(_statusLabel);

        _progressLabel = new Label { Text = "" };
        _progressLabel.AddThemeFontSizeOverride("font_size", 14);
        _progressLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        vbox.AddChild(_progressLabel);

        margin.AddChild(vbox);
        AddChild(margin);

        Generate();
        GenerateFrameTextures();
    }

    private void Generate()
    {
        var outputDir = ProjectSettings.GlobalizePath("res://art/sprites/reagents");
        Directory.CreateDirectory(outputDir);

        var allDefs = ReagentCatalog.GetAll();
        _total = allDefs.Count;
        _generated = 0;

        var specEntries = new Dictionary<string, object>();

        foreach (var def in allDefs)
        {
            var aspectId = def.PrimaryAspectId ?? "";
            var baseColor = GridConstants.GetAspectColor(aspectId);

            var image = Image.CreateEmpty(SheetWidth, SheetHeight, false, Image.Format.Rgba8);

            DrawNormalRow(image, 0, baseColor, aspectId);
            DrawHeatedRow(image, 1, baseColor);
            DrawCooledRow(image, 2, baseColor);
            DrawThermalStressRow(image, 3, baseColor);
            DrawCrushedRow(image, 4, baseColor);
            DrawBreakingRow(image, 5, baseColor);
            DrawExplodingRow(image, 6, baseColor);

            var filePath = Path.Combine(outputDir, $"{def.Id}.png");
            image.SavePng(filePath);

            specEntries[def.Id] = new
            {
                cellWidth = CellSize,
                cellHeight = CellSize,
                totalRows = TotalRows,
                maxFrames = MaxFrames,
                states = new Dictionary<string, object>
                {
                    ["Normal"] = new { row = 0, frames = 1 },
                    ["Heated"] = new { row = 1, frames = 1 },
                    ["Cooled"] = new { row = 2, frames = 1 },
                    ["ThermalStress"] = new { row = 3, frames = 1 },
                    ["Crushed"] = new { row = 4, frames = 1 },
                    ["Breaking"] = new { row = 5, frames = 4 },
                    ["Exploding"] = new { row = 6, frames = 4 },
                }
            };

            _generated++;
        }

        // Write sprite_specs.json
        var specsPath = Path.Combine(outputDir, "sprite_specs.json");
        var json = JsonSerializer.Serialize(specEntries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(specsPath, json);

        _statusLabel.Text = $"Done! Generated {_generated}/{_total} sprite sheets.";
        _progressLabel.Text = $"Output: {outputDir}\nSpec: {specsPath}";

        GD.Print($"[SPRITE_GEN] Generated {_generated} sprite sheets to {outputDir}");
        GD.Print($"[SPRITE_GEN] Wrote sprite_specs.json");
    }

    private void GenerateFrameTextures()
    {
        var outputDir = ProjectSettings.GlobalizePath("res://art/sprites/frames/dev");
        Directory.CreateDirectory(outputDir);

        const int borderWidth = 4;
        const int innerWidth = 1;
        int frameCount = 0;

        // All 8 standard aspect IDs
        string[] aspects = { AspectIds.Hot, AspectIds.Cold, AspectIds.Volatile, AspectIds.Toxic,
                             AspectIds.Heavy, AspectIds.Stable, AspectIds.Sharp, AspectIds.Fragile };

        foreach (var shapeName in ReagentCatalog.ShapeNames)
        {
            var shapeMatrix = ReagentCatalog.GetShapeMatrix(shapeName);
            if (shapeMatrix == null) continue;

            int shapeRows = shapeMatrix.GetLength(0);
            int shapeCols = shapeMatrix.GetLength(1);
            var perimeterEdges = HoverFrameResolver.GetPerimeterEdges(shapeMatrix);

            foreach (var aspectId in aspects)
            {
                var aspectColor = GridConstants.GetAspectColor(aspectId);
                int imgWidth = shapeCols * CellSize;
                int imgHeight = shapeRows * CellSize;
                var image = Image.CreateEmpty(imgWidth, imgHeight, false, Image.Format.Rgba8);

                // Draw perimeter frame on each cell
                foreach (var (pos, edges) in perimeterEdges)
                {
                    int cellX = pos.Col * CellSize;
                    int cellY = pos.Row * CellSize;

                    DrawFrameEdges(image, cellX, cellY, edges, aspectColor, borderWidth, innerWidth);
                }

                var filePath = Path.Combine(outputDir, $"{aspectId}_{shapeName}.png");
                image.SavePng(filePath);
                frameCount++;
            }
        }

        GD.Print($"[SPRITE_GEN] Generated {frameCount} frame textures to {outputDir}");
        _progressLabel.Text += $"\nFrames: {outputDir} ({frameCount} PNGs)";
    }

    private static void DrawFrameEdges(Image image, int cellX, int cellY,
        CellEdge edges, Color color, int borderWidth, int innerWidth)
    {
        var white = new Color(1f, 1f, 1f, 0.8f);

        if (edges.HasFlag(CellEdge.Top))
        {
            // Outer colored border
            FillRect(image, cellX, cellY, CellSize, borderWidth, color);
            // Inner white edge
            FillRect(image, cellX, cellY + borderWidth, CellSize, innerWidth, white);
        }

        if (edges.HasFlag(CellEdge.Bottom))
        {
            FillRect(image, cellX, cellY + CellSize - borderWidth, CellSize, borderWidth, color);
            FillRect(image, cellX, cellY + CellSize - borderWidth - innerWidth, CellSize, innerWidth, white);
        }

        if (edges.HasFlag(CellEdge.Left))
        {
            FillRect(image, cellX, cellY, borderWidth, CellSize, color);
            FillRect(image, cellX + borderWidth, cellY, innerWidth, CellSize, white);
        }

        if (edges.HasFlag(CellEdge.Right))
        {
            FillRect(image, cellX + CellSize - borderWidth, cellY, borderWidth, CellSize, color);
            FillRect(image, cellX + CellSize - borderWidth - innerWidth, cellY, innerWidth, CellSize, white);
        }
    }

    // --- Row drawing methods ---

    private static void DrawNormalRow(Image image, int row, Color baseColor, string aspectId)
    {
        // Frame 0: clean colored rectangle + 1px border + aspect icon silhouette
        int y0 = row * CellSize;
        FillRect(image, 0, y0, CellSize, CellSize, baseColor);
        DrawBorder(image, 0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));
        DrawAspectIcon(image, 0, y0, aspectId);
    }

    private static void DrawHeatedRow(Image image, int row, Color baseColor)
    {
        // Base color + orange corner quadrant overlay + flame glyph
        int y0 = row * CellSize;
        FillRect(image, 0, y0, CellSize, CellSize, baseColor);
        DrawBorder(image, 0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));

        // Orange overlay in top-right corner quadrant
        var orange = new Color(0.95f, 0.5f, 0.1f, 0.5f);
        FillRect(image, CellSize / 2, y0, CellSize / 2, CellSize / 2, orange);

        // Small flame glyph (5px tall centered in corner)
        var flameColor = new Color(1f, 0.6f, 0.1f);
        int fx = CellSize - 8;
        int fy = y0 + 3;
        SetPixelSafe(image, fx + 2, fy, flameColor);
        SetPixelSafe(image, fx + 1, fy + 1, flameColor);
        SetPixelSafe(image, fx + 2, fy + 1, flameColor);
        SetPixelSafe(image, fx + 3, fy + 1, flameColor);
        SetPixelSafe(image, fx + 1, fy + 2, flameColor);
        SetPixelSafe(image, fx + 2, fy + 2, flameColor);
        SetPixelSafe(image, fx + 3, fy + 2, flameColor);
        SetPixelSafe(image, fx + 1, fy + 3, flameColor);
        SetPixelSafe(image, fx + 2, fy + 3, flameColor);
        SetPixelSafe(image, fx + 3, fy + 3, flameColor);
        SetPixelSafe(image, fx + 2, fy + 4, flameColor);
    }

    private static void DrawCooledRow(Image image, int row, Color baseColor)
    {
        // Base color + blue corner quadrant overlay + snowflake glyph
        int y0 = row * CellSize;
        FillRect(image, 0, y0, CellSize, CellSize, baseColor);
        DrawBorder(image, 0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));

        // Blue overlay in bottom-left corner quadrant
        var blue = new Color(0.15f, 0.35f, 0.85f, 0.5f);
        FillRect(image, 0, y0 + CellSize / 2, CellSize / 2, CellSize / 2, blue);

        // Small snowflake glyph
        var snowColor = new Color(0.7f, 0.85f, 1f);
        int sx = 5;
        int sy = y0 + CellSize - 11;
        // Cross pattern
        SetPixelSafe(image, sx + 3, sy + 1, snowColor);
        SetPixelSafe(image, sx + 3, sy + 2, snowColor);
        SetPixelSafe(image, sx + 3, sy + 3, snowColor);
        SetPixelSafe(image, sx + 3, sy + 4, snowColor);
        SetPixelSafe(image, sx + 3, sy + 5, snowColor);
        SetPixelSafe(image, sx + 1, sy + 3, snowColor);
        SetPixelSafe(image, sx + 2, sy + 3, snowColor);
        SetPixelSafe(image, sx + 4, sy + 3, snowColor);
        SetPixelSafe(image, sx + 5, sy + 3, snowColor);
        // Diagonal accents
        SetPixelSafe(image, sx + 1, sy + 1, snowColor);
        SetPixelSafe(image, sx + 5, sy + 1, snowColor);
        SetPixelSafe(image, sx + 1, sy + 5, snowColor);
        SetPixelSafe(image, sx + 5, sy + 5, snowColor);
    }

    private static void DrawThermalStressRow(Image image, int row, Color baseColor)
    {
        // Red-blue diagonal split + crack lines
        int y0 = row * CellSize;
        var red = new Color(0.85f, 0.2f, 0.15f, 0.6f);
        var blue = new Color(0.15f, 0.35f, 0.85f, 0.6f);

        for (int py = 0; py < CellSize; py++)
        {
            for (int px = 0; px < CellSize; px++)
            {
                // Diagonal split: top-left = red, bottom-right = blue
                var overlay = px + py < CellSize ? red : blue;
                var blended = BlendColors(baseColor, overlay);
                SetPixelSafe(image, px, y0 + py, blended);
            }
        }
        DrawBorder(image, 0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));

        // Crack lines: jagged diagonal
        var crackColor = new Color(0.15f, 0.15f, 0.15f);
        for (int i = 0; i < CellSize; i++)
        {
            int cx = i;
            int cy = y0 + i + ((i % 3 == 0) ? 1 : (i % 3 == 1) ? -1 : 0);
            SetPixelSafe(image, cx, cy, crackColor);
        }
    }

    private static void DrawCrushedRow(Image image, int row, Color baseColor)
    {
        // Darkened base + downward arrow + crack lines from top
        int y0 = row * CellSize;
        var darkened = DarkenColor(baseColor, 0.5f);
        FillRect(image, 0, y0, CellSize, CellSize, darkened);
        DrawBorder(image, 0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.6f));

        // Downward arrow glyph (centered)
        var arrowColor = new Color(0.9f, 0.9f, 0.9f);
        int ax = CellSize / 2;
        int ay = y0 + 8;
        // Shaft
        for (int i = 0; i < 12; i++)
            SetPixelSafe(image, ax, ay + i, arrowColor);
        // Head
        SetPixelSafe(image, ax - 3, ay + 9, arrowColor);
        SetPixelSafe(image, ax - 2, ay + 10, arrowColor);
        SetPixelSafe(image, ax - 1, ay + 11, arrowColor);
        SetPixelSafe(image, ax + 1, ay + 11, arrowColor);
        SetPixelSafe(image, ax + 2, ay + 10, arrowColor);
        SetPixelSafe(image, ax + 3, ay + 9, arrowColor);

        // Crack lines from top edge
        var crackColor = new Color(0.1f, 0.1f, 0.1f);
        SetPixelSafe(image, 8, y0 + 1, crackColor);
        SetPixelSafe(image, 9, y0 + 2, crackColor);
        SetPixelSafe(image, 8, y0 + 3, crackColor);
        SetPixelSafe(image, 7, y0 + 4, crackColor);
        SetPixelSafe(image, 22, y0 + 1, crackColor);
        SetPixelSafe(image, 21, y0 + 2, crackColor);
        SetPixelSafe(image, 22, y0 + 3, crackColor);
        SetPixelSafe(image, 23, y0 + 4, crackColor);
    }

    private static void DrawBreakingRow(Image image, int row, Color baseColor)
    {
        // 4 frames: progressive cracks -> fragment separation -> scatter -> fade
        int y0 = row * CellSize;
        var crackColor = new Color(0.1f, 0.1f, 0.1f);

        // Frame 0: Base with initial cracks
        int x0 = 0;
        FillRect(image, x0, y0, CellSize, CellSize, baseColor);
        DrawBorder(image, x0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));
        // Single crack
        for (int i = 0; i < 10; i++)
        {
            int cx = x0 + 12 + (i % 3 == 0 ? 1 : 0);
            SetPixelSafe(image, cx, y0 + 10 + i, crackColor);
        }

        // Frame 1: More cracks, widening
        x0 = CellSize;
        FillRect(image, x0, y0, CellSize, CellSize, baseColor);
        DrawBorder(image, x0, y0, CellSize, CellSize, DarkenColor(baseColor, 0.4f));
        // Two cracks
        for (int i = 0; i < 16; i++)
        {
            SetPixelSafe(image, x0 + 10 + (i % 3 == 0 ? 1 : 0), y0 + 8 + i, crackColor);
            SetPixelSafe(image, x0 + 20 + (i % 4 == 0 ? -1 : 0), y0 + 6 + i, crackColor);
        }

        // Frame 2: Fragments separating (gaps appearing)
        x0 = CellSize * 2;
        var transparent = new Color(0, 0, 0, 0);
        FillRect(image, x0, y0, CellSize, CellSize, transparent);
        // Draw 4 fragments
        FillRect(image, x0 + 2, y0 + 2, 12, 12, baseColor);
        FillRect(image, x0 + 18, y0 + 2, 12, 12, baseColor);
        FillRect(image, x0 + 2, y0 + 18, 12, 12, baseColor);
        FillRect(image, x0 + 18, y0 + 18, 12, 12, baseColor);

        // Frame 3: Scattered fragments fading
        x0 = CellSize * 3;
        FillRect(image, x0, y0, CellSize, CellSize, transparent);
        var faded = new Color(baseColor.R, baseColor.G, baseColor.B, 0.35f);
        FillRect(image, x0 + 1, y0 + 1, 8, 8, faded);
        FillRect(image, x0 + 22, y0 + 3, 6, 6, faded);
        FillRect(image, x0 + 4, y0 + 22, 7, 7, faded);
        FillRect(image, x0 + 24, y0 + 24, 5, 5, faded);
    }

    private static void DrawExplodingRow(Image image, int row, Color baseColor)
    {
        // 4 frames: flash -> burst -> particles -> fade
        int y0 = row * CellSize;
        var transparent = new Color(0, 0, 0, 0);

        // Frame 0: Flash to white
        var flash = new Color(1f, 1f, 0.9f);
        FillRect(image, 0, y0, CellSize, CellSize, flash);

        // Frame 1: Orange-red burst (expanding circle)
        int x1 = CellSize;
        FillRect(image, x1, y0, CellSize, CellSize, transparent);
        var burstOuter = new Color(0.95f, 0.3f, 0.1f, 0.8f);
        var burstInner = new Color(1f, 0.7f, 0.2f, 0.9f);
        int cx = x1 + CellSize / 2;
        int cy = y0 + CellSize / 2;
        for (int py = 0; py < CellSize; py++)
        {
            for (int px = 0; px < CellSize; px++)
            {
                float dist = Mathf.Sqrt((x1 + px - cx) * (x1 + px - cx) + (y0 + py - cy) * (y0 + py - cy));
                if (dist < 6)
                    SetPixelSafe(image, x1 + px, y0 + py, burstInner);
                else if (dist < 12)
                    SetPixelSafe(image, x1 + px, y0 + py, burstOuter);
            }
        }

        // Frame 2: Particle scatter
        int x2 = CellSize * 2;
        FillRect(image, x2, y0, CellSize, CellSize, transparent);
        var particleColor = new Color(0.95f, 0.5f, 0.15f, 0.7f);
        // Scattered particles at pseudo-random positions
        int[][] particles = { new[]{4,5}, new[]{12,3}, new[]{24,7}, new[]{8,14},
                              new[]{20,12}, new[]{28,18}, new[]{6,24}, new[]{16,22},
                              new[]{26,26}, new[]{14,28} };
        foreach (var p in particles)
        {
            FillRect(image, x2 + p[0], y0 + p[1], 3, 3, particleColor);
        }

        // Frame 3: Fade to transparent (very faint remnants)
        int x3 = CellSize * 3;
        FillRect(image, x3, y0, CellSize, CellSize, transparent);
        var faintColor = new Color(0.9f, 0.4f, 0.1f, 0.15f);
        FillRect(image, x3 + 8, y0 + 8, 16, 16, faintColor);
    }

    // --- Drawing helpers ---

    private static void FillRect(Image image, int x, int y, int w, int h, Color color)
    {
        image.FillRect(new Rect2I(x, y, w, h), color);
    }

    private static void DrawBorder(Image image, int x, int y, int w, int h, Color color)
    {
        // Top and bottom edges
        for (int px = x; px < x + w; px++)
        {
            SetPixelSafe(image, px, y, color);
            SetPixelSafe(image, px, y + h - 1, color);
        }
        // Left and right edges
        for (int py = y; py < y + h; py++)
        {
            SetPixelSafe(image, x, py, color);
            SetPixelSafe(image, x + w - 1, py, color);
        }
    }

    private static void SetPixelSafe(Image image, int x, int y, Color color)
    {
        if (x >= 0 && x < image.GetWidth() && y >= 0 && y < image.GetHeight())
        {
            if (color.A < 1f)
            {
                var existing = image.GetPixel(x, y);
                image.SetPixel(x, y, BlendColors(existing, color));
            }
            else
            {
                image.SetPixel(x, y, color);
            }
        }
    }

    private static Color BlendColors(Color bg, Color fg)
    {
        float a = fg.A + bg.A * (1f - fg.A);
        if (a < 0.001f) return new Color(0, 0, 0, 0);
        float r = (fg.R * fg.A + bg.R * bg.A * (1f - fg.A)) / a;
        float g = (fg.G * fg.A + bg.G * bg.A * (1f - fg.A)) / a;
        float b = (fg.B * fg.A + bg.B * bg.A * (1f - fg.A)) / a;
        return new Color(r, g, b, a);
    }

    private static Color DarkenColor(Color c, float factor)
    {
        return new Color(c.R * (1f - factor), c.G * (1f - factor), c.B * (1f - factor), c.A);
    }

    private static void DrawAspectIcon(Image image, int x, int y, string aspectId)
    {
        // 8x8 icon silhouette centered in the cell
        int ix = x + (CellSize - 8) / 2;
        int iy = y + (CellSize - 8) / 2;
        var iconColor = new Color(1f, 1f, 1f, 0.7f);

        switch (aspectId)
        {
            case AspectIds.Hot:
                DrawFlameIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Cold:
                DrawSnowflakeIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Volatile:
                DrawWarningIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Toxic:
                DrawSkullIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Heavy:
                DrawAnvilIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Stable:
                DrawShieldIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Sharp:
                DrawBladeIcon(image, ix, iy, iconColor);
                break;
            case AspectIds.Fragile:
                DrawCrackedIcon(image, ix, iy, iconColor);
                break;
            default:
                DrawDiamondIcon(image, ix, iy, iconColor);
                break;
        }
    }

    // --- 8x8 Icon drawing ---

    private static void DrawFlameIcon(Image image, int x, int y, Color c)
    {
        //   ..##....
        //   .###....
        //   .####...
        //   .#####..
        //   .#####..
        //   ..####..
        //   ..###...
        //   ...#....
        SetPixelSafe(image, x+2, y+0, c); SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+1, y+1, c); SetPixelSafe(image, x+2, y+1, c); SetPixelSafe(image, x+3, y+1, c);
        SetPixelSafe(image, x+1, y+2, c); SetPixelSafe(image, x+2, y+2, c); SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+4, y+2, c);
        SetPixelSafe(image, x+1, y+3, c); SetPixelSafe(image, x+2, y+3, c); SetPixelSafe(image, x+3, y+3, c); SetPixelSafe(image, x+4, y+3, c); SetPixelSafe(image, x+5, y+3, c);
        SetPixelSafe(image, x+1, y+4, c); SetPixelSafe(image, x+2, y+4, c); SetPixelSafe(image, x+3, y+4, c); SetPixelSafe(image, x+4, y+4, c); SetPixelSafe(image, x+5, y+4, c);
        SetPixelSafe(image, x+2, y+5, c); SetPixelSafe(image, x+3, y+5, c); SetPixelSafe(image, x+4, y+5, c); SetPixelSafe(image, x+5, y+5, c);
        SetPixelSafe(image, x+2, y+6, c); SetPixelSafe(image, x+3, y+6, c); SetPixelSafe(image, x+4, y+6, c);
        SetPixelSafe(image, x+3, y+7, c);
    }

    private static void DrawSnowflakeIcon(Image image, int x, int y, Color c)
    {
        // Cross + diagonals
        SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+1, y+1, c); SetPixelSafe(image, x+3, y+1, c); SetPixelSafe(image, x+5, y+1, c);
        SetPixelSafe(image, x+2, y+2, c); SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+4, y+2, c);
        for (int i = 0; i < 7; i++) SetPixelSafe(image, x+i, y+3, c);
        SetPixelSafe(image, x+2, y+4, c); SetPixelSafe(image, x+3, y+4, c); SetPixelSafe(image, x+4, y+4, c);
        SetPixelSafe(image, x+1, y+5, c); SetPixelSafe(image, x+3, y+5, c); SetPixelSafe(image, x+5, y+5, c);
        SetPixelSafe(image, x+3, y+6, c);
    }

    private static void DrawWarningIcon(Image image, int x, int y, Color c)
    {
        // Triangle with exclamation
        SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+2, y+1, c); SetPixelSafe(image, x+4, y+1, c);
        SetPixelSafe(image, x+2, y+2, c); SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+4, y+2, c);
        SetPixelSafe(image, x+1, y+3, c); SetPixelSafe(image, x+3, y+3, c); SetPixelSafe(image, x+5, y+3, c);
        SetPixelSafe(image, x+1, y+4, c); SetPixelSafe(image, x+3, y+4, c); SetPixelSafe(image, x+5, y+4, c);
        SetPixelSafe(image, x+0, y+5, c); SetPixelSafe(image, x+6, y+5, c);
        SetPixelSafe(image, x+0, y+6, c); SetPixelSafe(image, x+3, y+6, c); SetPixelSafe(image, x+6, y+6, c);
        for (int i = 0; i <= 6; i++) SetPixelSafe(image, x+i, y+7, c);
    }

    private static void DrawSkullIcon(Image image, int x, int y, Color c)
    {
        // Simple skull
        SetPixelSafe(image, x+2, y+0, c); SetPixelSafe(image, x+3, y+0, c); SetPixelSafe(image, x+4, y+0, c);
        SetPixelSafe(image, x+1, y+1, c); SetPixelSafe(image, x+5, y+1, c);
        SetPixelSafe(image, x+1, y+2, c); SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+5, y+2, c);
        SetPixelSafe(image, x+1, y+3, c); SetPixelSafe(image, x+5, y+3, c);
        SetPixelSafe(image, x+2, y+4, c); SetPixelSafe(image, x+3, y+4, c); SetPixelSafe(image, x+4, y+4, c);
        SetPixelSafe(image, x+1, y+5, c); SetPixelSafe(image, x+3, y+5, c); SetPixelSafe(image, x+5, y+5, c);
        SetPixelSafe(image, x+2, y+6, c); SetPixelSafe(image, x+4, y+6, c);
    }

    private static void DrawAnvilIcon(Image image, int x, int y, Color c)
    {
        // Anvil shape
        for (int i = 1; i <= 5; i++) SetPixelSafe(image, x+i, y+0, c);
        SetPixelSafe(image, x+2, y+1, c); SetPixelSafe(image, x+3, y+1, c); SetPixelSafe(image, x+4, y+1, c);
        SetPixelSafe(image, x+3, y+2, c);
        SetPixelSafe(image, x+3, y+3, c);
        for (int i = 1; i <= 5; i++) SetPixelSafe(image, x+i, y+4, c);
        for (int i = 0; i <= 6; i++) SetPixelSafe(image, x+i, y+5, c);
        SetPixelSafe(image, x+1, y+6, c); SetPixelSafe(image, x+5, y+6, c);
        for (int i = 0; i <= 6; i++) SetPixelSafe(image, x+i, y+7, c);
    }

    private static void DrawShieldIcon(Image image, int x, int y, Color c)
    {
        // Shield shape
        for (int i = 1; i <= 5; i++) SetPixelSafe(image, x+i, y+0, c);
        SetPixelSafe(image, x+0, y+1, c); SetPixelSafe(image, x+6, y+1, c);
        SetPixelSafe(image, x+0, y+2, c); SetPixelSafe(image, x+6, y+2, c);
        SetPixelSafe(image, x+0, y+3, c); SetPixelSafe(image, x+6, y+3, c);
        SetPixelSafe(image, x+1, y+4, c); SetPixelSafe(image, x+5, y+4, c);
        SetPixelSafe(image, x+1, y+5, c); SetPixelSafe(image, x+5, y+5, c);
        SetPixelSafe(image, x+2, y+6, c); SetPixelSafe(image, x+4, y+6, c);
        SetPixelSafe(image, x+3, y+7, c);
    }

    private static void DrawBladeIcon(Image image, int x, int y, Color c)
    {
        // Blade / dagger
        SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+3, y+1, c);
        SetPixelSafe(image, x+2, y+2, c); SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+4, y+2, c);
        SetPixelSafe(image, x+3, y+3, c);
        SetPixelSafe(image, x+3, y+4, c);
        SetPixelSafe(image, x+1, y+5, c); SetPixelSafe(image, x+3, y+5, c); SetPixelSafe(image, x+5, y+5, c);
        SetPixelSafe(image, x+3, y+6, c);
        SetPixelSafe(image, x+2, y+7, c); SetPixelSafe(image, x+3, y+7, c); SetPixelSafe(image, x+4, y+7, c);
    }

    private static void DrawCrackedIcon(Image image, int x, int y, Color c)
    {
        // Cracked glass / diamond with crack
        SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+2, y+1, c); SetPixelSafe(image, x+4, y+1, c);
        SetPixelSafe(image, x+1, y+2, c); SetPixelSafe(image, x+5, y+2, c);
        SetPixelSafe(image, x+0, y+3, c); SetPixelSafe(image, x+6, y+3, c);
        SetPixelSafe(image, x+1, y+4, c); SetPixelSafe(image, x+3, y+4, c); SetPixelSafe(image, x+5, y+4, c);
        SetPixelSafe(image, x+2, y+5, c); SetPixelSafe(image, x+4, y+5, c);
        SetPixelSafe(image, x+3, y+6, c);
        // Crack line through center
        SetPixelSafe(image, x+3, y+2, c); SetPixelSafe(image, x+4, y+3, c); SetPixelSafe(image, x+2, y+3, c);
    }

    private static void DrawDiamondIcon(Image image, int x, int y, Color c)
    {
        // Simple diamond
        SetPixelSafe(image, x+3, y+0, c);
        SetPixelSafe(image, x+2, y+1, c); SetPixelSafe(image, x+4, y+1, c);
        SetPixelSafe(image, x+1, y+2, c); SetPixelSafe(image, x+5, y+2, c);
        SetPixelSafe(image, x+0, y+3, c); SetPixelSafe(image, x+6, y+3, c);
        SetPixelSafe(image, x+1, y+4, c); SetPixelSafe(image, x+5, y+4, c);
        SetPixelSafe(image, x+2, y+5, c); SetPixelSafe(image, x+4, y+5, c);
        SetPixelSafe(image, x+3, y+6, c);
    }
}
