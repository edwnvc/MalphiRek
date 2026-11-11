using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P2Trines;

using Geometry = UmadP2TrinesConstants.Geometry;

// What UmadP2TrinesAi's moves are for, per role, for the death recap.
internal static class UmadP2TrinesPlaybook
{
    private const string LightOfJudgment = "NAUR §2.11 (0:24:55)";
    private const string Trine = "NAUR §2.12 (0:28:34)";
    private const string Wings = "NAUR §2.13 (0:29:10)";
    private const string Busters = "NAUR §2.14 (0:29:26)";
    private const string Embrace = "NAUR §2.15 (0:30:08)";

    private static readonly string[] Bearings = ["o meio", "norte", "leste-nordeste", "sudeste", "sul", "oeste-sudoeste", "noroeste"];

    private static string Where(int triangle) => $"{Geometry.Name(triangle)} ({Bearings[triangle]})";

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static readonly StratCue Opening = new(
        "Light of Judgment II",
        role => role == PartyRole.MainTank
            ? "Segure o Kefka virado para o norte, a 6y ao norte dele: os triângulos do Trine e o primeiro Wings of Destruction dependem de para onde ele está virado. Light of Judgment II é um raidwide."
            : "Faça stack logo ao sul do Kefka: Light of Judgment II é um raidwide, e o Kefka continua virado para o norte para o Trine.",
        LightOfJudgment);

    public static StratCue WingsDodge(UmadP2TrinesState state) => StratCue.ForAll(
        "Wings of Destruction (meia sala)",
        $"O Kefka acende a asa {(state.WingsRight ? "direita" : "esquerda")}: ela acerta a metade {(state.WingsRight ? "direita" : "esquerda")} dele, o {(state.WingsRight ? "leste" : "oeste")}, então fique 2y dentro da metade "
        + $"{(state.WingsRight ? "oeste" : "leste")} ao lado dele. Isso fica dentro do triângulo central, que cai por último, então fique ali até o primeiro grupo de triângulos explodir.",
        Wings);

    public static StratCue Spots(UmadP2TrinesState state) => new(
        "Trine",
        role => role switch
        {
            PartyRole.MainTank => $"Pegue o buster de perto do Wings of Destruction: uns 9y para o {(state.CentrePointsEast ? "oeste" : "sudoeste")}, o mais perto do Kefka que os cantos do triângulo central deixarem (mire na borda reta dele), "
                                  + "e longe de qualquer triângulo que ainda vai cair. Ele acerta o jogador mais perto, então ninguém mais pode ficar mais perto.",
            PartyRole.OffTank => $"Pegue o buster de longe do Wings of Destruction: a 18,5y em {Where(state.TankTriangle)}, o primeiro triângulo do primeiro grupo no sentido anti-horário a partir do noroeste. "
                                 + "Ele acerta o jogador mais longe, então ninguém mais pode ficar mais longe.",
            _ => $"Vá para {Where(state.PartyTriangle)}, o primeiro triângulo do primeiro grupo no sentido horário a partir do norte, depois que ele explodir: dentro dele, bem afastado mas não na parede. "
                 + $"Os círculos dos busters estão nos tanks, então fique a mais de 7y de {Label(PartyRole.MainTank)} e {Label(PartyRole.OffTank)}.",
        },
        Trine);

    public static readonly StratCue BusterSpots = new(
        "Wings of Destruction (tank busters)",
        role => role switch
        {
            PartyRole.MainTank => "Continue sendo o jogador mais perto do Kefka: um buster de 7y cai no jogador mais perto e deixa uma vulnerabilidade, então o off-tank tem que ficar bem longe de você.",
            PartyRole.OffTank => "Continue sendo o jogador mais longe do Kefka: um buster de 7y cai no jogador mais longe e deixa uma vulnerabilidade, então fique longe do main tank.",
            _ => "Fique no seu triângulo: os busters caem no jogador mais perto e no mais longe, e esses têm que ser os dois tanks.",
        },
        Busters);

    public static readonly StratCue UltimateEmbrace = new(
        "Ultimate Embrace II",
        role => role switch
        {
            PartyRole.MainTank => "Ultimate Embrace II é um tank buster compartilhado de 5y em você: fique parado para o off-tank se juntar. Sozinho, precisa de invuln.",
            PartyRole.OffTank => $"Junte-se a {Label(PartyRole.MainTank)} para o Ultimate Embrace II, um tank buster de 5y que vocês dois dividem.",
            _ => "Fique a mais de 5y dos tanks: Ultimate Embrace II é o buster compartilhado deles, letal para qualquer outro.",
        },
        Embrace);
}
