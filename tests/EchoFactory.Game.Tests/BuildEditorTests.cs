using EchoFactory.Core;
using EchoFactory.Game;
using Xunit;

namespace EchoFactory.Game.Tests;

public class BuildEditorTests
{
    private static LevelDefinition Level() => new()
    {
        Id = "t",
        Grid = new GridSize(5, 3),
        MaxTicks = 10,
        Generators = [new GeneratorSpec { Id = "g", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 1)] }],
        Sinks = [new SinkSpec { Id = "s", Position = new GridPoint(4, 1), Expected = [1] }],
    };

    [Fact]
    public void Place_Remove_Undo_Redo()
    {
        var editor = new BuildEditor(Level());
        var cell = new GridPoint(1, 1);

        Assert.True(editor.Place(PlacedNode.Belt(cell, Direction.Right)));
        Assert.Equal(1, editor.Count);

        Assert.True(editor.Undo());
        Assert.Equal(0, editor.Count);

        Assert.True(editor.Redo());
        Assert.Equal(1, editor.Count);

        Assert.True(editor.Remove(cell));
        Assert.Equal(0, editor.Count);

        Assert.True(editor.Undo());
        Assert.Equal(1, editor.Count);
    }

    [Fact]
    public void Place_IdenticalNode_IsNoOp()
    {
        var editor = new BuildEditor(Level());
        var cell = new GridPoint(2, 1);

        Assert.True(editor.Place(PlacedNode.Belt(cell, Direction.Right)));
        Assert.False(editor.Place(PlacedNode.Belt(cell, Direction.Right))); // same → no change
        Assert.True(editor.Place(PlacedNode.Belt(cell, Direction.Up)));     // different dir → change
    }

    [Fact]
    public void CannotPlace_OnFixedCells()
    {
        var editor = new BuildEditor(Level());
        Assert.False(editor.CanPlace(new GridPoint(0, 1))); // generator
        Assert.False(editor.CanPlace(new GridPoint(4, 1))); // sink
        Assert.False(editor.Place(PlacedNode.Belt(new GridPoint(0, 1), Direction.Right)));
    }
}
