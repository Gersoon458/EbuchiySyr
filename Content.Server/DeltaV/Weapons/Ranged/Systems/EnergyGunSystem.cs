using Content.Server.Popups;
using Content.Server.DeltaV.Weapons.Ranged.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Item;
using Content.Shared.DeltaV.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.Guns;

namespace Content.Server.DeltaV.Weapons.Ranged.Systems;

public sealed class EnergyGunSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GunSystem _gun = default!; // WWDP EDIT

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyGunComponent, ComponentInit>(OnComponentInit); // WWDP EDIT
        SubscribeLocalEvent<EnergyGunComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<EnergyGunComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<EnergyGunComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, EnergyGunComponent component, ExaminedEvent args)
    {
        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        if (component.CurrentFireMode == null)
        {
            SetFireMode(uid, component, component.FireModes.First());
        }

        if (component.CurrentFireMode?.Prototype == null)
            return;

        if (!_prototypeManager.TryIndex<EntityPrototype>(component.CurrentFireMode.Prototype, out var proto))
            return;

        // WWDP edit start - locale
        var firemode = "battery-fire-mode-" + component.CurrentFireMode.Name;
        var mode = Loc.GetString(firemode);

        if (component.CurrentFireMode.Name == string.Empty)
            mode = proto.Name;

        var color = "crimson";
        if (component.CurrentFireMode.Name == "disable")
            color = "lightblue";

        if (component.CurrentFireMode.Name == "ion")
            color = "blue";

        args.PushMarkup(Loc.GetString("energygun-examine-fire-mode", ("mode", mode), ("color", color)));
        // WWDP edit end
    }

    private void OnGetVerb(EntityUid uid, EnergyGunComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        if (component.CurrentFireMode == null)
        {
            SetFireMode(uid, component, component.FireModes.First());
        }

        foreach (var fireMode in component.FireModes)
        {
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = fireMode == component.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, component, fireMode, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    // WWDP EDIT START
    private void OnComponentInit(EntityUid uid, EnergyGunComponent component, ComponentInit args)
    {
        if(component.FireModes.Count > 0)
            SetFireMode(uid, component, component.FireModes[0]);
    }
    // WWDP EDIT END

    private void OnInteractHandEvent(EntityUid uid, EnergyGunComponent component, ActivateInWorldEvent args)
    {
        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        CycleFireMode(uid, component, args.User);
    }

    private void CycleFireMode(EntityUid uid, EnergyGunComponent component, EntityUid user)
    {
        int index = (component.CurrentFireMode != null) ?
            Math.Max(component.FireModes.IndexOf(component.CurrentFireMode), 0) + 1 : 1;

        EnergyWeaponFireMode? fireMode;

        if (index >= component.FireModes.Count)
        {
            fireMode = component.FireModes.FirstOrDefault();
        }

        else
        {
            fireMode = component.FireModes[index];
        }

        SetFireMode(uid, component, fireMode, user);
    }

    private void SetFireMode(EntityUid uid, EnergyGunComponent component, EnergyWeaponFireMode? fireMode, EntityUid? user = null)
    {
        if (fireMode?.Prototype == null)
            return;

        component.CurrentFireMode = fireMode;

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                return;

            projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;
            // WWDP EDIT START
            if (TryComp<GunOverheatComponent>(uid, out var overheat))
            {
                overheat.HeatCost = fireMode.HeatCost;
                Dirty(uid, overheat);
            }
            _gun.UpdateShots(uid, projectileBatteryAmmoProvider);

            if (user != null)
            {
                var firemode = "battery-fire-mode-" + fireMode.Name;
                var mode = Loc.GetString(firemode);

                if (fireMode.Name == string.Empty)
                    mode = prototype.Name;

                _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", mode)), uid, user.Value);
            }
            // WWDP EDIT END

            if (component.CurrentFireMode.State == string.Empty)
                return;

            if (TryComp<AppearanceComponent>(uid, out var _) && TryComp<ItemComponent>(uid, out var item))
            {
                _item.SetHeldPrefix(uid, component.CurrentFireMode.State, false, item);
                switch (component.CurrentFireMode.State)
                {
                    case "disabler":
                        UpdateAppearance(uid, EnergyGunFireModeState.Disabler);
                        break;
                    case "lethal":
                        UpdateAppearance(uid, EnergyGunFireModeState.Lethal);
                        break;
                    case "special":
                        UpdateAppearance(uid, EnergyGunFireModeState.Special);
                        break;
                }
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, EnergyGunFireModeState state)
    {
        _appearance.SetData(uid, EnergyGunFireModeVisuals.State, state);
    }
}
