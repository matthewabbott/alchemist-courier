using Godot;

namespace AlchemistCourier.Game.Testing;

public static class ScreenshotHelper
{
    private const string OutputDir = "res://test_output";

    public static string GetOutputDirectory()
    {
        var absPath = ProjectSettings.GlobalizePath(OutputDir);
        DirAccess.MakeDirRecursiveAbsolute(absPath);
        return absPath;
    }

    public static string CaptureViewport(Viewport viewport, string filename)
    {
        var dir = GetOutputDirectory();
        var path = System.IO.Path.Combine(dir, filename);

        var image = viewport.GetTexture().GetImage();
        var error = image.SavePng(path);

        if (error != Error.Ok)
        {
            GD.PrintErr($"[SCREENSHOT_ERROR] Failed to save {path}: {error}");
            return "";
        }

        GD.Print($"[SCREENSHOT] {path}");
        return path;
    }
}
