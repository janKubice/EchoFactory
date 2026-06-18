using Microsoft.Xna.Framework;

namespace EchoFactory.Game;

/// <summary>Dark-mode palette with neon accents (Mini Metro inspired).</summary>
internal static class Palette
{
    public static readonly Color Background = new(18, 18, 22);
    public static readonly Color Grid = new(30, 30, 38);
    public static readonly Color GridLine = new(46, 46, 58);
    public static readonly Color Belt = new(120, 130, 140);
    public static readonly Color Item = new(240, 240, 245);
    public static readonly Color ItemText = new(16, 16, 20);
    public static readonly Color Generator = new(52, 152, 219);
    public static readonly Color Sink = new(231, 76, 60);
    public static readonly Color Math = new(46, 204, 113);
    public static readonly Color Splitter = new(243, 156, 18);
    public static readonly Color Portal = new(232, 67, 147);
    public static readonly Color Filter = new(26, 188, 156);
    public static readonly Color Router = new(155, 89, 182);
    public static readonly Color Accumulator = new(230, 126, 34);
    public static readonly Color Text = new(224, 224, 234);
    public static readonly Color TextDim = new(128, 128, 145);
    public static readonly Color Accent = new(0, 200, 255);
    public static readonly Color Solved = new(46, 204, 113);
    public static readonly Color Failed = new(243, 156, 18);
    public static readonly Color Paradox = new(231, 76, 60);
    public static readonly Color Panel = new(26, 26, 32);
    public static readonly Color PanelHi = new(42, 42, 54);

    public static Color Node(NodeColorKind kind) => kind switch
    {
        NodeColorKind.Generator => Generator,
        NodeColorKind.Sink => Sink,
        NodeColorKind.Math => Math,
        NodeColorKind.Splitter => Splitter,
        NodeColorKind.Portal => Portal,
        _ => Belt,
    };
}

internal enum NodeColorKind
{
    Belt,
    Generator,
    Sink,
    Math,
    Splitter,
    Portal,
}
