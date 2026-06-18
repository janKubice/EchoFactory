using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

/// <summary>Per-frame mouse + keyboard snapshot with edge detection.</summary>
internal sealed class InputState
{
    private readonly System.Text.StringBuilder _pendingText = new();
    private KeyboardState _prevKeyboard;
    private KeyboardState _keyboard;
    private MouseState _prevMouse;
    private MouseState _mouse;
    private string _typed = string.Empty;
    private Rectangle _viewport = new(0, 0, 1280, 720);
    private float _virtualW = 1280f;
    private float _virtualH = 720f;

    /// <summary>Maps raw window mouse coords into the virtual (letterboxed) screen space.</summary>
    public void SetViewport(Rectangle dest, float virtualW, float virtualH)
    {
        _viewport = dest;
        _virtualW = virtualW;
        _virtualH = virtualH;
    }

    /// <summary>Printable characters typed since the last frame (for text fields). Empty when none.</summary>
    public string Typed => _typed;

    /// <summary>Called from the window's TextInput event to buffer typed characters.</summary>
    public void EnqueueText(char c)
    {
        if (!char.IsControl(c))
        {
            _pendingText.Append(c);
        }
    }

    public void Update()
    {
        _prevKeyboard = _keyboard;
        _keyboard = Keyboard.GetState();
        _prevMouse = _mouse;
        _mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

        _typed = _pendingText.ToString();
        _pendingText.Clear();
    }

    public Vector2 Mouse
    {
        get
        {
            float sx = _viewport.Width > 0 ? _virtualW / _viewport.Width : 1f;
            float sy = _viewport.Height > 0 ? _virtualH / _viewport.Height : 1f;
            return new Vector2((_mouse.X - _viewport.X) * sx, (_mouse.Y - _viewport.Y) * sy);
        }
    }

    public bool LeftClick => _mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

    public bool LeftDown => _mouse.LeftButton == ButtonState.Pressed;

    public bool RightClick => _mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

    public int ScrollDelta => _mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;

    public bool KeyPressed(Keys key) => _keyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

    public bool KeyDown(Keys key) => _keyboard.IsKeyDown(key);

    /// <summary>True on the frame any key transitions from up to down.</summary>
    public bool AnyKeyPressed
    {
        get
        {
            foreach (Keys k in _keyboard.GetPressedKeys())
            {
                if (_prevKeyboard.IsKeyUp(k))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
