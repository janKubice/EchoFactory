namespace EchoFactory.Core;

/// <summary>
/// Configuration for a time portal. An item entering the portal at tick T is removed from the
/// flow and re-emitted at the exit cell at apply-tick <c>T - TimeOffset</c>.
/// <list type="bullet">
///   <item><see cref="TimeOffset"/> &gt; 0 — into the past (causal loop, resolved by fixed point).</item>
///   <item><see cref="TimeOffset"/> &lt; 0 — into the future (delay buffer).</item>
/// </list>
/// </summary>
public sealed class PortalConfig
{
    public required int TimeOffset { get; init; }

    /// <summary>Direction of the exit cell relative to the portal (where re-emitted items appear).</summary>
    public required Direction Output { get; init; }
}
