using Robust.Shared.GameStates;

namespace Content.Shared.IconSmoothing;

/// <summary>
/// Applies an edge sprite to <see cref="IconSmoothComponent"/> for non-smoothed directions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmoothEdgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("edgeAdditionalKeys")]
    public List<string> EdgeAdditionalKeys { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("requireMatchingKey")]
    public bool RequireMatchingKey = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("drawdepth")]
    public int? DrawDepth;
}
