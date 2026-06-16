namespace EchoFactory.Game;

/// <summary>A screen of the game (menu, level select, gameplay).</summary>
internal interface IScene
{
    void Update(float dt, InputState input);

    void Draw(Renderer r);
}

/// <summary>Holds the active scene and shared services scenes use to navigate.</summary>
internal sealed class SceneManager
{
    private IScene? _current;

    public required LevelCatalog Catalog { get; init; }

    public required GameSettings Settings { get; init; }

    public required AudioManager Audio { get; init; }

    public required Action Quit { get; init; }

    public int ScreenW { get; set; }

    public int ScreenH { get; set; }

    public void Switch(IScene next) => _current = next;

    /// <summary>Play a sound at the current effective SFX volume.</summary>
    public void Play(Sfx sfx) => Audio.Play(sfx, Settings.EffectiveSfx);

    public void Update(float dt, InputState input) => _current?.Update(dt, input);

    public void Draw(Renderer r) => _current?.Draw(r);
}
