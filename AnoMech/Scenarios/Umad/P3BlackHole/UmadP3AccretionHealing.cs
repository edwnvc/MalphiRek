using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Core.Game.Party;
using FFXIVClientStructs.FFXIV.Client.Game;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// Accretion – Healer: the party's HP from Accretion to the last cleanse, moved by the player's own
// White Mage heals. Accretion cuts the heals its holder takes and goes once they're back to full,
// and each one gone sets off an Earthquake on everyone else, with the Earth vuln that kills anyone
// a second one catches. The shield healer bot only tops the others up, so the player alone decides
// when each Accretion pops.
internal sealed class UmadP3AccretionHealing(IScenario owner)
{
    // UNVERIFIED tuning, until checked against a pull's HP.
    // DMU healing per potency point: Divine Caress' 400-potency barrier is 32,563 HP (Mitigation.cs).
    internal const float HpPerPotency = 81f;
    internal const float AccretionHealingTaken = 0.5f;
    // A holder's HP when Accretion lands; everyone else starts full.
    internal const float HolderStartHp = 0.7f;
    // The mitigation sheet's raw Earthquake (UmadP3BlackHoleMitigation).
    private const float EarthquakeRaw = 80000f;
    // The Spreadlo and Zoe barrier from The Decisive Battle, before the scenario, which soaks part of
    // the first Earthquake each member takes (Galvanize's size in Mitigation.cs).
    private const float DecisiveBattleBarrierHp = 32200f;
    // Accretion gone to the cleanse's cast, as in the pull (12.38s -> 13.40s).
    private const float CleanseDelay = 1.02f;
    private const float BotHealDelay = 1.2f;
    private const float BotHealPotency = 400f;
    // When Accretion lands (Run_OtherDebuffs), plus a beat so its statuses are on.
    private const float StartsAt = 8.40f;

    private readonly record struct Heal(float Potency, float Radius = 0f, bool AroundTarget = false, bool Full = false);

    // shortcut: direct heals only; Regen, Medica's regen, Asylum and Liturgy of the Bell heal nothing
    // here, and Temperance doesn't boost. Add them if the practice needs them.
    private static readonly IReadOnlyDictionary<uint, Heal> WhiteMageHeals = new Dictionary<uint, Heal>
    {
        [120] = new(500f),                                 // Cure
        [135] = new(800f),                                 // Cure II
        [16531] = new(800f),                               // Afflatus Solace
        [3570] = new(700f),                                // Tetragrammaton
        [140] = new(0f, Full: true),                       // Benediction
        [131] = new(600f, Radius: 10f, AroundTarget: true), // Cure III
        [124] = new(400f, Radius: 15f),                    // Medica
        [133] = new(250f, Radius: 20f),                    // Medica II
        [37010] = new(250f, Radius: 20f),                  // Medica III
        [16534] = new(400f, Radius: 20f),                  // Afflatus Rapture
        [3571] = new(400f, Radius: 15f),                   // Assize
    };

    private readonly Dictionary<SimCharacter, float> hp = [];
    private readonly HashSet<SimCharacter> holders = [];
    private readonly HashSet<SimCharacter> barriered = [];
    private SimWorld world = null!;
    private bool started;
    private bool subscribed;

    // Black Hole's invisible Chaos that casts each cleanse.
    internal Func<SimEnemy?>? CleanseCaster { get; set; }

    internal float HpOf(SimCharacter member) => hp.GetValueOrDefault(member, 1f);
    internal IReadOnlyCollection<SimCharacter> Holders => holders;
    // Off, a bot in the player's seat heals nothing, as tests driving the heals themselves need.
    internal bool BotPlayerHeals { get; set; } = true;

    internal void Begin(SimWorld worldParam, params Func<SimCharacter?>[] holderSeats)
    {
        world = worldParam;
        started = false;
        hp.Clear();
        holders.Clear();
        barriered.Clear();
        if (!subscribed && Natives.UserActions is { } actions)
        {
            actions.Acted += OnActed;
            subscribed = true;
        }
        world.Events.Add(StartsAt, () =>
        {
            foreach (var seat in holderSeats)
                if (seat() is { } holder && holder.HasStatus(StatusId.Accretion))
                    holders.Add(holder);
            foreach (var member in world.Party.AllMembers().Where(m => m is ISimPartyMember))
            {
                hp[member] = holders.Contains(member) ? HolderStartHp : 1f;
                barriered.Add(member);
                member.WriteHp(hp[member]);
            }
            started = true;
            if (world.Party.Player is not { } player) return;
            if (world.Party.IsBotDriven(player))
            {
                if (BotPlayerHeals) HealAsTheReferenceWhiteMage();
            }
            else if ((JobId)player.ClassJob != JobId.WhiteMage)
                world.Announce("Accretion – Healer (beta) só conta curas de White Mage: troque para WHM.");
        });
    }

    // A bot in the player's seat (debug bot control, the headless runs): Benediction on the first,
    // then Tetragrammaton and Cure II on the second once the first Earthquake's vuln is gone.
    private void HealAsTheReferenceWhiteMage()
    {
        var order = holders.ToList();
        if (order.Count == 0) return;
        world.Events.Add(0.6f, () => PlayerHeal(140, order[0]));
        if (order.Count < 2) return;
        world.Events.Add(3.6f, () => PlayerHeal(3570, order[1]));
        for (var at = 2.6f; at < 14f; at += 2.5f)
            world.Events.Add(at, () => { if (holders.Contains(order[1])) PlayerHeal(135, order[1]); });
    }

    private void OnActed(PlayerAction action)
    {
        if (action.Step != PlayerActionStep.Resolved || action.ActionType != ActionType.Action) return;
        if (!ReferenceEquals(Plugin.Game?.ActiveScenario, owner)) return;
        var target = world.Party.AllMembers().FirstOrDefault(m => (ulong)m.GameObjectId == action.TargetId);
        PlayerHeal(action.ActionId, target);
    }

    // `target` null: the heal went on the player (or on no party member, which a heal can't).
    internal void PlayerHeal(uint actionId, SimCharacter? target)
    {
        if (!started || !WhiteMageHeals.TryGetValue(actionId, out var heal) || world.Party.Player is not { } player) return;
        var aimed = target ?? player;
        if (heal.Radius <= 0f)
        {
            Restore(aimed, heal);
            return;
        }
        var center = heal.AroundTarget ? aimed.Position : player.Position;
        foreach (var member in hp.Keys.ToList())
            if (Vector3.Distance(member.Position, center) <= heal.Radius)
                Restore(member, heal);
    }

    private void Restore(SimCharacter member, Heal heal)
    {
        if (!member.IsAlive() || !hp.TryGetValue(member, out var current)) return;
        var holder = holders.Contains(member);
        var gain = heal.Full ? 1f : heal.Potency * HpPerPotency / MaxHp(member) * (holder ? AccretionHealingTaken : 1f);
        hp[member] = MathF.Min(1f, current + gain);
        member.WriteHp(hp[member]);
        if (holder && hp[member] >= 0.999f) Pop(member);
    }

    private void Pop(SimCharacter holder)
    {
        holders.Remove(holder);
        holder.RemoveStatus(StatusId.Accretion);
        world.Events.Add(CleanseDelay, () => CleanseCaster?.Invoke()?.Cast(UmadActions.EarthquakeCleanse, holder));
    }

    internal void OnActionResolved(ActionResolved resolved)
    {
        if (!started || !ReferenceEquals(resolved.Action, UmadActions.EarthquakeCleanse)) return;
        foreach (var member in resolved.Hits)
        {
            if (member == resolved.Target || !member.IsAlive() || !hp.TryGetValue(member, out var current)) continue;
            var job = (JobId)((ISimPartyMember)member).ClassJob;
            var loss = EarthquakeRaw * UmadMitigation.Profile.Taken(MitProfile.ClassOf(job));
            if (barriered.Remove(member)) loss = MathF.Max(0f, loss - DecisiveBattleBarrierHp);
            hp[member] = current - loss / MaxHp(member);
            member.WriteHp(MathF.Max(0f, hp[member]));
            if (hp[member] <= 0f)
                member.Die(ActionId.Earthquake_Cleanse, "o HP não foi recuperado entre os Earthquakes");
        }
        world.Events.Add(BotHealDelay, BotHeal);
    }

    internal void OnStatusLost(StatusLost lost)
    {
        if (!started || lost.StatusId != StatusId.Accretion || lost.Reason != StatusLossReason.Expired) return;
        if (!holders.Remove(lost.Holder) || !lost.Holder.IsAlive()) return;
        lost.Holder.AddStatus(StatusId.DamageDown, 180f);
        var who = lost.Holder is SimPlayer ? "Você" : ((ISimPartyMember)lost.Holder).Role.ShortLabel();
        world.Fail($"{who} ficou com Accretion até o fim e foi petrificado (o HP não chegou a 100%)");
    }

    // The bot shield healer's plain AoE heal after each Earthquake, on everyone but the holders.
    private void BotHeal()
    {
        if (world.Party.Get(PartyRole.ShieldHealer) is not SimPartyNpc bot || !bot.IsAlive()) return;
        bot.UseAction((JobId)bot.ClassJob switch
        {
            JobId.Scholar => 186u,        // Succor
            JobId.Sage => 24286u,         // Prognosis
            JobId.Astrologian => 3600u,   // Helios
            _ => 124u,                    // Medica
        });
        foreach (var member in hp.Keys.ToList())
            if (!holders.Contains(member))
                Restore(member, new Heal(BotHealPotency));
    }

    private static float MaxHp(SimCharacter member)
        => UmadMitigation.Profile.MaxHp(MitProfile.ClassOf((JobId)((ISimPartyMember)member).ClassJob));
}
