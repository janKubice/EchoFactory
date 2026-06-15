namespace EchoFactory.Core;

/// <summary>
/// The built-in node implementations. JSON-defined node *types* map onto these
/// (content-format.md). Splitter / Math / Portal arrive in later milestones.
/// </summary>
public enum NodeKind
{
    Generator,
    Sink,
    Belt,
    Math,
    Splitter,
}
