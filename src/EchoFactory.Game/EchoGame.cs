using EchoFactory.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoFactory.Game;

/// <summary>MonoGame host: owns the renderer, input and the active scene.</summary>
public sealed class EchoGame : Microsoft.Xna.Framework.Game
{
    private static string LeaderboardPath => Path.Combine(AppContext.BaseDirectory, "echofactory-leaderboard.json");

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private readonly GameSettings _settings = SettingsStore.Load();
    private readonly Leaderboard _leaderboard = LeaderboardStore.Load(LeaderboardPath);
    private Renderer _renderer = null!;
    private AudioManager _audio = null!;
    private SceneManager? _scenes;
    private string? _loadError;

    public EchoGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = false;
        Window.Title = "EchoFactory";
    }

    protected override void LoadContent()
    {
        _renderer = new Renderer(GraphicsDevice);
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
                ScreenW = GraphicsDevice.Viewport.Width,
                ScreenH = GraphicsDevice.Viewport.Height,
            };
            _scenes.Switch(_settings.ShowIntro ? new SplashScene(_scenes) : new MainMenuScene(_scenes));
        }
        catch (Exception e)
        {
            _loadError = e.Message;
        }

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
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        SettingsStore.Save(_settings);
        LeaderboardStore.Save(LeaderboardPath, _leaderboard);
        _renderer?.Dispose();
        _audio?.Dispose();
        base.UnloadContent();
    }
}
