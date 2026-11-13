using System.Collections.Generic;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Integrations.BossMod;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// Black Hole's opening up to the end of Accretion, for practising its heals as the White Mage: the
// player's heals pop each Accretion (UmadP3AccretionHealing). Settings and strats are Black Hole's.
public sealed class UmadP3AccretionHealerScenario : IScenario
{
    // Accretion runs out at 22.37s; the slap and the first tethers after it are left out.
    public const float EndsAtSeconds = 23f;

    private readonly UmadP3BlackHoleScenario blackHole;

    public UmadP3AccretionHealerScenario()
    {
        Healing = new UmadP3AccretionHealing(this);
        blackHole = new UmadP3BlackHoleScenario(fromStompAMole: false, Healing);
    }

    internal UmadP3AccretionHealing Healing { get; }

    public string Name => "Accretion – Healer (beta)";
    public IPhase Phase => blackHole.Phase;
    public float? EndsAt => EndsAtSeconds;
    public BossModStart BossModStart => blackHole.BossModStart;
    public IReadOnlyList<IScenarioAi> AiStrats => blackHole.AiStrats;

    public void Run(SimWorld world, int? selectedAi) => blackHole.Run(world, selectedAi);
    public void Tick(float delta, float elapsed) => blackHole.Tick(delta, elapsed);
    public void RunInstanceEvents(SimWorld world) => blackHole.RunInstanceEvents(world);

    public void OnActionResolved(ActionResolved resolved) => blackHole.OnActionResolved(resolved);
    public void OnStatusAdded(StatusAdded added) => ((IWorldEventListener)blackHole).OnStatusAdded(added);
    public void OnStatusLost(StatusLost lost) => blackHole.OnStatusLost(lost);
    public void OnTetherEnded(TetherEnded ended) => ((IWorldEventListener)blackHole).OnTetherEnded(ended);

    public void DrawSettings() => blackHole.DrawSettings();
    public bool HasPerPlayerSettings => blackHole.HasPerPlayerSettings;
    public void DrawPerPlayerSettings() => blackHole.DrawPerPlayerSettings();
    public object SettingsOverrides => blackHole.SettingsOverrides;
    public IReadOnlyList<string> SettingsConflicts => blackHole.SettingsConflicts;
}
