using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Integrations.BossMod;

// Moves BossMod's fight module to where a scenario starts (see BossModStart), so its hints and
// everything built on them (NyaDraw's BossMod AOEs) cover the practiced mechanic. BossMod may
// drop a module and build a new one mid-run (e.g. when another actor with the primary OID
// appears), and that one starts from the pull again, so every new instance is moved, for the
// whole run.
internal sealed class BossModBridge
{
    private const float WarnAfterSeconds = 15f;

    private BossModStart? pending;
    private float waited;
    private bool warned;
    private readonly HashSet<object> moved = new(ReferenceEqualityComparer.Instance);

    public string? LastResult { get; private set; }

    public void OnScenarioStarted(BossModStart? start)
    {
        pending = Plugin.Config.SplatoonCompat && Plugin.Config.SyncBossModModule ? start : null;
        waited = 0f;
        warned = false;
        moved.Clear();
        if (pending != null) LastResult = $"waiting for BossMod's {ShortName(pending.ModuleType)} module";
    }

    public void OnScenarioStopped()
    {
        // Where BossMod got to shows whether it followed the fight from the moved state.
        foreach (var module in moved)
        {
            var machine = BossModReflection.Read(module, "StateMachine");
            var state = machine == null ? null : BossModReflection.Read(machine, "ActiveState");
            Core.DiagnosticLog.Info($"[BossModBridge] run ended with {ShortName(module.GetType().FullName ?? "?")} in phase {(machine == null ? "?" : BossModReflection.Read(machine, "ActivePhaseIndex"))}, state 0x{(state == null ? 0 : BossModReflection.Read(state, "ID")):X} '{(state == null ? "inactive" : BossModReflection.Read(state, "Name"))}'.");
        }
        pending = null;
        moved.Clear();
    }

    // Every frame, after Game.Tick.
    public void Tick(float deltaSeconds)
    {
        if (pending is not { } start) return;
        waited += deltaSeconds;
        if (!warned && moved.Count == 0 && waited > WarnAfterSeconds)
        {
            warned = true;
            Report($"no {ShortName(start.ModuleType)} module after {WarnAfterSeconds:F0}s, still watching; saw {BossModReflection.LastScan}");
        }

        foreach (var (plugin, manager) in BossModReflection.FindManagers())
        {
            try
            {
                if (TrySync(manager, start, moved))
                    Report($"{plugin} {ShortName(start.ModuleType)} moved to phase {start.Phase}, state 0x{start.StatePath[^1]:X} ({moved.Count} instance(s) this run)");
            }
            catch (Exception e)
            {
                pending = null;
                Report($"{plugin}: moving the module threw, left as BossMod set it: {e.InnerException?.Message ?? e.Message}");
                return;
            }
        }
    }

    // False while no loaded module of the type is left to move. A module BossMod hasn't pulled is
    // started first.
    internal static bool TrySync(object manager, BossModStart start, ISet<object>? moved = null)
    {
        if (BossModReflection.Read(manager, "LoadedModules") is not IEnumerable modules) return false;
        var module = modules.Cast<object>()
            .FirstOrDefault(m => m.GetType().FullName == start.ModuleType && moved?.Contains(m) != true);
        if (module == null || BossModReflection.Read(module, "StateMachine") is not { } machine) return false;
        if (BossModReflection.Read(machine, "ActivePhaseIndex") is not int phaseIndex) return false;
        // BossMod pulls only once the primary actor is targetable and in combat, which a
        // mid-fight start may never give it (UMAD's Kefka stays untargetable through P3).
        // Start it on BossMod's own clock so state timers don't count from DateTime.MinValue.
        if (phaseIndex < 0)
        {
            if (BossModReflection.Read(module, "WorldState") is not { } world
                || BossModReflection.Read(world, "CurrentTime") is not DateTime now)
                return false;
            BossModReflection.Invoke(machine, "Start", now);
        }

        if (BossModReflection.Read(machine, "Phases") is not IList phases || start.Phase >= phases.Count)
            throw new InvalidOperationException($"the module has no phase {start.Phase}");
        var initial = BossModReflection.Read(phases[start.Phase]!, "InitialState")
                      ?? throw new InvalidOperationException($"phase {start.Phase} has no initial state");
        var states = start.StatePath
            .Select(id => FindState(initial, id) ?? throw new InvalidOperationException($"no state 0x{id:X} in phase {start.Phase}"))
            .ToList();

        BossModReflection.Invoke(machine, "TransitionToPhase", start.Phase);
        foreach (var state in states)
            BossModReflection.Invoke(machine, "TransitionToState", state);
        moved?.Add(module);
        return true;
    }

    internal static object? FindState(object initial, uint id)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<object>([initial]);
        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            if (!seen.Add(state)) continue;
            if (BossModReflection.Read(state, "ID") is uint stateId && stateId == id) return state;
            if (BossModReflection.Read(state, "NextStates") is IEnumerable next)
                foreach (var successor in next)
                    if (successor != null) queue.Enqueue(successor);
        }
        return null;
    }

    private void Report(string result)
    {
        LastResult = result;
        Core.DiagnosticLog.Info($"[BossModBridge] {result}.");
    }

    private static string ShortName(string moduleType) => moduleType[(moduleType.LastIndexOf('.') + 1)..];
}
