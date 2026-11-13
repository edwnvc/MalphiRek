using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Core.Game.Party.PartyRole;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;
using StatusId = AnoMech.Scenarios.Umad.UmadConstants.StatusId;

namespace AnoMech.Tests;

// Black Hole's pinned rolls seat Accretion on the shield healer and the caster; the player is the
// White Mage, whose heals are handed to the practice directly.
public class UmadP3AccretionHealerScenarioTests
{
    private const uint CureII = 135;
    private const uint Benediction = 140;

    private static ScenarioRun Run(Action<ScenarioProbe, UmadP3AccretionHealing>? heal = null)
        => ScenarioRun.Execute(typeof(UmadP3AccretionHealerScenario), 0, 1, new ScenarioRunOptions
        {
            PlayerRole = RegenHealer,
            WriteArtifactsOnFailure = false,
            Overrides = o => UmadP3BlackHoleScenarioTests.Pin((UmadP3BlackHoleStateOverrides)o),
            Probe = p =>
            {
                if (p.Game.ActiveScenario is not UmadP3AccretionHealerScenario s) return;
                s.Healing.BotPlayerHeals = false;
                heal?.Invoke(p, s.Healing);
            },
        });

    [Test]
    public void LeftUnhealedBothHoldersArePetrified()
    {
        var run = Run();

        Assert.Multiple(() =>
        {
            Assert.That(run.Deaths, Is.Empty);
            Assert.That(run.MechanicFailures, Has.Count.EqualTo(2));
            Assert.That(run.Elapsed, Is.LessThan(UmadP3AccretionHealerScenario.EndsAtSeconds + 10f), "the run stops after Accretion");
        });
    }

    [Test]
    public void HealedToFullOneAtATimeTheyPopSafely()
    {
        var run = Run((p, healing) =>
        {
            if (p.Crossed(9f)) healing.PlayerHeal(Benediction, p.Member(ShieldHealer));
            if (p.Crossed(13f)) healing.PlayerHeal(Benediction, p.Member(CasterDps));
        });

        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void BothPoppedTogetherTheSecondEarthquakeKills()
    {
        var run = Run((p, healing) =>
        {
            if (!p.Crossed(9f)) return;
            healing.PlayerHeal(Benediction, p.Member(ShieldHealer));
            healing.PlayerHeal(Benediction, p.Member(CasterDps));
        });

        Assert.That(run.Deaths.Select(d => d.ActionId), Has.Some.EqualTo(ActionId.Earthquake_Cleanse));
    }

    [Test]
    public void OneCureIiLeavesAHolderShortOfFull()
    {
        float? hp = null;
        var holds = false;
        Run((p, healing) =>
        {
            if (p.Crossed(9f)) healing.PlayerHeal(CureII, p.Member(CasterDps));
            if (!p.Crossed(9.1f)) return;
            hp = healing.HpOf(p.Member(CasterDps)!);
            holds = p.Member(CasterDps)!.HasStatus(StatusId.Accretion);
        });

        var expected = UmadP3AccretionHealing.HolderStartHp
                       + 800f * UmadP3AccretionHealing.HpPerPotency / 205200f * UmadP3AccretionHealing.AccretionHealingTaken;
        Assert.Multiple(() =>
        {
            Assert.That(hp, Is.EqualTo(expected).Within(0.001f));
            Assert.That(holds, Is.True);
        });
    }
}
