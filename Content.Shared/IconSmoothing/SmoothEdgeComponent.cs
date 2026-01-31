using Robust.Shared.GameStates;

namespace Content.Shared.IconSmoothing;

/// <summary>
/// Applies an edge sprite to <see cref="IconSmoothComponent"/> for non-smoothed directions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmoothEdgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("edgeSubKeys")]
    public List<string?> EdgeSubKeys { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("requireMatchingKey")]
    public bool RequireMatchingKey = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("drawdepth")]
    public int? DrawDepth;
}
