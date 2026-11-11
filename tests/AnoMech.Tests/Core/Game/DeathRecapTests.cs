using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using ActionId = AnoMech.Scenarios.Umad.P1Shared.UmadP1GravenConstants.ActionId;

namespace AnoMech.Tests;

public class DeathRecapTests
{
    private static readonly Coordinates Origin = new(() => new Vector3(100f, 0f, 100f));

    [SetUp]
    public void InstallGame() => FakeGame.Install();

    private static StratTarget Spot(float x, float z, float? deadline = null)
        => new(PartyRole.RegenHealer, new Vector3(x, 0f, z), 10f, deadline, "Wave Cannon lineup", "why", "NAUR");

    [TestCase(0f, -5f, "norte")]
    [TestCase(5f, -5f, "nordeste")]
    [TestCase(5f, 0f, "leste")]
    [TestCase(5f, 5f, "sudeste")]
    [TestCase(0f, 5f, "sul")]
    [TestCase(-5f, 5f, "sudoeste")]
    [TestCase(-5f, 0f, "oeste")]
    [TestCase(-3f, -3f, "noroeste")]
    public void CompassReadsNorthAsMinusZ(float x, float z, string expected)
        => Assert.That(DeathRecap.Compass(Vector3.Zero, new Vector3(x, 0f, z)), Is.EqualTo(expected));

    [Test, SetCulture("pt-BR")]
    public void WhereTextGivesTheMissInWorldTerms()
    {
        var missed = DeathRecap.Build(PartyRole.RegenHealer, "cause", new Vector3(-4f, 0f, -4f), 18.2f, Spot(0f, 0f, 17.9f), null, Origin);
        Assert.That(missed.MissedBy, Is.EqualTo(5.657f).Within(0.01f));
        Assert.That(missed.WhereText, Is.EqualTo("Você estava a 5,7y ao noroeste do seu lugar."));
        Assert.That(missed.TimingText, Is.EqualTo("Esteja lá até 17,9s; você morreu em 18,2s."));
        Assert.That(missed.DiedAt, Is.EqualTo(new Vector3(96f, 0f, 96f)));

        var onSpot = DeathRecap.Build(PartyRole.RegenHealer, "cause", new Vector3(0.5f, 0f, 0f), 18.2f, Spot(0f, 0f), null, Origin);
        Assert.That(onSpot.WhereText, Is.EqualTo("Você estava no seu lugar."));
        Assert.That(onSpot.TimingText, Is.Null);

        var noStrat = DeathRecap.Build(PartyRole.RegenHealer, "cause", Vector3.Zero, 3f, null, null, Origin);
        Assert.That(noStrat.Spot, Is.Null);
        Assert.That(noStrat.WhereText, Is.EqualTo("A strat não tinha um lugar para você neste momento."));
    }

    [Test]
    public void KillingAoeTakesItsShapeFromTheSheet()
    {
        var query = new AoeQuery(ActionId.WaveCannon, new Placement(new Vector3(0f, 0f, -35f), 0.3f));
        var recap = DeathRecap.Build(PartyRole.RegenHealer, "cause", Vector3.Zero, 18.2f, null, query, Origin);
        Assert.That(recap.Aoe, Is.Not.Null);
        var aoe = recap.Aoe!;
        Assert.That((aoe.Name, aoe.CastType, aoe.Range, aoe.HalfWidth), Is.EqualTo(("Wave Cannon", (byte)12, 100f, 3f)));
        Assert.That(aoe.Origin, Is.EqualTo(new Vector3(100f, 0f, 65f)));
        Assert.That(aoe.Rotation, Is.EqualTo(0.3f));
    }
}
