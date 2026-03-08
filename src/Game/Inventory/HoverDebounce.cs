using System;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Timer-based hover debounce that delays the "hide" action after mouse-exit,
/// giving the user a brief window to re-enter the hovered element without
/// losing context. No Godot dependencies — call Process(delta) each frame.
/// </summary>
public class HoverDebounce
{
    private readonly float _hideDelay;
    private float _timer;
    private bool _isShowing;
    private bool _timerActive;

    public event Action? OnShow;
    public event Action? OnHide;

    public bool IsShowing => _isShowing;
    public bool IsTimerActive => _timerActive;

    public HoverDebounce(float hideDelaySeconds = 0.25f)
    {
        _hideDelay = hideDelaySeconds;
    }

    /// <summary>
    /// Call when the mouse enters the hoverable element.
    /// Cancels any pending hide and fires OnShow if not already showing.
    /// </summary>
    public void OnMouseEntered()
    {
        _timerActive = false;
        _timer = 0f;

        if (!_isShowing)
        {
            _isShowing = true;
            OnShow?.Invoke();
        }
    }

    /// <summary>
    /// Call when the mouse exits the hoverable element.
    /// Starts the hide countdown instead of hiding immediately.
    /// </summary>
    public void OnMouseExited()
    {
        if (!_isShowing) return;

        _timerActive = true;
        _timer = _hideDelay;
    }

    /// <summary>
    /// Tick the debounce timer. Call from _Process(delta).
    /// Fires OnHide when the countdown elapses.
    /// </summary>
    public void Process(float delta)
    {
        if (!_timerActive) return;

        _timer -= delta;
        if (_timer <= 0f)
        {
            _timerActive = false;
            _isShowing = false;
            OnHide?.Invoke();
        }
    }

    /// <summary>
    /// Force an immediate hide, bypassing the timer.
    /// </summary>
    public void ForceHide()
    {
        _timerActive = false;
        _timer = 0f;
        if (_isShowing)
        {
            _isShowing = false;
            OnHide?.Invoke();
        }
    }

    /// <summary>
    /// Reset to initial state without firing events.
    /// </summary>
    public void Reset()
    {
        _timerActive = false;
        _timer = 0f;
        _isShowing = false;
    }
}
