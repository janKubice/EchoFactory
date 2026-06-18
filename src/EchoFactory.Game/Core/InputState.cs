using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

/// <summary>Per-frame mouse + keyboard snapshot with edge detection.</summary>
internal sealed class InputState
{
    private KeyboardState _prevKeyboard;
    private KeyboardState _keyboard;
    private MouseState _prevMouse;
    private MouseState _mouse;

    public void Update()
    {
        _prevKeyboard = _keyboard;
        _keyboard = Keyboard.GetState();
        _prevMouse = _mouse;
        _mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
    }

    public Vector2 Mouse => new(_mouse.X, _mouse.Y);

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
