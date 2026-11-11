using AnoMech.Integrations.BossMod;

namespace AnoMech.Scenarios.Umad;

// Where a Dancing Mad scenario begins in Boss Mod's UMAD module: its phase (0 = P1 .. 4 = P5) and
// the state that waits for the scenario's opening cast. State ids are Boss Mod's UMADStates.
internal static class UmadBossMod
{
    private const string Module = "BossMod.Dawntrail.Ultimate.UMAD.UMAD";

    // Boss Mod only loads the module for an actor with the P1/P3 Kefka's id, which retail keeps
    // from the pull on; P2, P4 and P5 scenarios spawn no such actor, so they bring an invisible one.
    public static BossModStart At(int phase, params uint[] statePath)
        => new(Module, phase, statePath, phase is 1 or 3 or 4 ? UmadConstants.BNpcBaseId.KefkaP3 : null);
}
