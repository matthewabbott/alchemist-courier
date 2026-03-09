using Godot;

namespace AlchemistCourier.Game.Testing;

/// <summary>
/// Reusable assertions for UI layout verification in the test harness.
/// </summary>
public static class LayoutTestHelper
{
    /// <summary>
    /// Verifies that the control's rect size meets minimum thresholds.
    /// </summary>
    public static bool CheckMinSize(Control control, float minWidth, float minHeight, out string reason)
    {
        var size = control.GetRect().Size;
        if (size.X < minWidth || size.Y < minHeight)
        {
            reason = $"{control.Name ?? control.GetType().Name} size ({size.X:F0}x{size.Y:F0}) " +
                     $"below minimum ({minWidth}x{minHeight})";
            return false;
        }
        reason = "";
        return true;
    }

    /// <summary>
    /// Verifies that the control is visible and has non-zero size.
    /// </summary>
    public static bool CheckVisible(Control control, out string reason)
    {
        if (!control.Visible)
        {
            reason = $"{control.Name ?? control.GetType().Name} is not visible";
            return false;
        }
        var size = control.GetRect().Size;
        if (size.X <= 0 || size.Y <= 0)
        {
            reason = $"{control.Name ?? control.GetType().Name} has zero size ({size.X:F0}x{size.Y:F0})";
            return false;
        }
        reason = "";
        return true;
    }

    /// <summary>
    /// Verifies that two controls' global rects do not overlap.
    /// </summary>
    public static bool CheckNoOverlap(Control a, Control b, out string reason)
    {
        var rectA = a.GetGlobalRect();
        var rectB = b.GetGlobalRect();
        if (rectA.Intersects(rectB))
        {
            reason = $"{a.Name ?? a.GetType().Name} ({rectA}) overlaps " +
                     $"{b.Name ?? b.GetType().Name} ({rectB})";
            return false;
        }
        reason = "";
        return true;
    }

    /// <summary>
    /// Verifies that the child control fits within the parent's bounds.
    /// </summary>
    public static bool CheckContainedIn(Control child, Control parent, out string reason)
    {
        var childRect = child.GetGlobalRect();
        var parentRect = parent.GetGlobalRect();
        if (!parentRect.Encloses(childRect))
        {
            reason = $"{child.Name ?? child.GetType().Name} ({childRect}) " +
                     $"not contained in {parent.Name ?? parent.GetType().Name} ({parentRect})";
            return false;
        }
        reason = "";
        return true;
    }
}
