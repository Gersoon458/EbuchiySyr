using Content.Server.Disposal.Unit.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Tube.Components;

[RegisterComponent]
[Access(typeof(DisposalTubeSystem), typeof(DisposableSystem))]
public sealed partial class DisposalTubeComponent : Component
{
    [DataField]
    public string ContainerId = "DisposalTube";

    [ViewVariables]
    public bool Connected;

    [DataField]
    public SoundSpecifier ClangSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg", AudioParams.Default.WithVolume(-5f));

    /// <summary>
    ///     Container of entities that are currently inside this tube
    /// </summary>
    [ViewVariables]
    public Container Contents = default!;

    /// <summary>
    /// Damage dealt to containing entities on every turn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamageOnTurn = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0 },
        }
    };
    /// <summary>
    /// Time in seconds for entities to transit through this tube
    /// </summary>
    [DataField("transitTime"), ViewVariables(VVAccess.ReadWrite)]
    public float TransitTime { get; set; } = 0.1f;

    /// <summary>
    /// Multiplier for throw speed when entities exit this tube
    /// </summary>
    [DataField("throwSpeedMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float ThrowSpeedMultiplier { get; set; } = 0.8f;

    /// <summary>
    /// Multiplier for throw distance when entities exit this tube
    /// </summary>
    [DataField("throwForceMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float ThrowForceMultiplier { get; set; } = 1.0f;
}
