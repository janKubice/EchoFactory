using EchoFactory.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoFactory.Game;

/// <summary>MonoGame host: owns the renderer, input and the active scene.</summary>
public sealed class EchoGame : Microsoft.Xna.Framework.Game
{
    // Everything is drawn to a fixed virtual resolution, then letterboxed to the window/screen.
    // Scene layouts stay resolution-independent and fullscreen never breaks input mapping.
    private const int VirtualW = 1280;
    private const int VirtualH = 720;

    private static string LeaderboardPath => Path.Combine(AppContext.BaseDirectory, "echofactory-leaderboard.json");

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private readonly GameSettings _settings = SettingsStore.Load();
    private readonly Leaderboard _leaderboard = LeaderboardStore.Load(LeaderboardPath);
    private Renderer _renderer = null!;
    private SpriteBatch _blit = null!;
    private RenderTarget2D _target = null!;
    private AudioManager _audio = null!;
    private SceneManager? _scenes;
    private Rectangle _viewport = new(0, 0, VirtualW, VirtualH);
    private string? _loadError;

    public EchoGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = VirtualW,
            PreferredBackBufferHeight = VirtualH,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = false;
        Window.Title = "EchoFactory";
        Window.TextInput += (_, e) => _input.EnqueueText(e.Character);
    }

    protected override void LoadContent()
    {
        _renderer = new Renderer(GraphicsDevice);
        _blit = new SpriteBatch(GraphicsDevice);
        _target = new RenderTarget2D(GraphicsDevice, VirtualW, VirtualH);
        _audio = new AudioManager();

        try
        {
            var catalog = new LevelCatalog(DataLocator.FindDataDir());
            _scenes = new SceneManager
            {
                Catalog = catalog,
                Settings = _settings,
                Audio = _audio,
                Leaderboard = _leaderboard,
                Quit = Exit,
                SetFullscreen = ApplyFullscreen,
                ScreenW = VirtualW,
                ScreenH = VirtualH,
            };
            _scenes.Switch(_settings.ShowIntro ? new SplashScene(_scenes) : new MainMenuScene(_scenes));
        }
        catch (Exception e)
        {
            _loadError = e.Message;
        }

        ApplyFullscreen(_settings.Fullscreen);
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        if (_loadError is null)
        {
            _scenes?.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _input);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // 1) Render the game to the fixed virtual-resolution target.
        GraphicsDevice.SetRenderTarget(_target);
        GraphicsDevice.Clear(Palette.Background);

        _renderer.Begin();
        if (_loadError is not null)
        {
            _renderer.Text("DATA LOAD ERROR:", new Vector2(40, 40), 4f, Palette.Paradox);
            _renderer.Text(_loadError, new Vector2(40, 80), 2.5f, Palette.Text);
        }
        else
        {
            _scenes?.Draw(_renderer);
        }

        _renderer.End();

        // 2) Letterbox-blit the target into the actual back buffer.
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _blit.Begin(samplerState: SamplerState.LinearClamp);
        _blit.Draw(_target, _viewport, Color.White);
        _blit.End();

        base.Draw(gameTime);
    }

    private void ApplyFullscreen(bool fullscreen)
    {
        _graphics.HardwareModeSwitch = false; // borderless windowed fullscreen
        _graphics.IsFullScreen = fullscreen;
        if (fullscreen)
        {
            DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = dm.Width;
            _graphics.PreferredBackBufferHeight = dm.Height;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = VirtualW;
            _graphics.PreferredBackBufferHeight = VirtualH;
        }

        _graphics.ApplyChanges();
        RecomputeViewport();
    }

    private void RecomputeViewport()
    {
        int bw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float scale = MathF.Min((float)bw / VirtualW, (float)bh / VirtualH);
        int w = (int)(VirtualW * scale);
        int h = (int)(VirtualH * scale);
        _viewport = new Rectangle((bw - w) / 2, (bh - h) / 2, w, h);
        _input.SetViewport(_viewport, VirtualW, VirtualH);
    }

    protected override void UnloadContent()
    {
        SettingsStore.Save(_settings);
        LeaderboardStore.Save(LeaderboardPath, _leaderboard);
        _target?.Dispose();
        _blit?.Dispose();
        _renderer?.Dispose();
        _audio?.Dispose();
        base.UnloadContent();
    }
}
