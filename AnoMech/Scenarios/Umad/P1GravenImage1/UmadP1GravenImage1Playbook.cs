using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

// What UmadP1GravenImage1Ai's moves are for, per role, for the death recap.
internal static class UmadP1GravenImage1Playbook
{
    private const string Gi1 = "NAUR §1.2 (0:00:48)";
    private const string Beams = "NAUR §1.3 (0:02:31) · wtfdig kefka_p1/5_graven1_laser_positions";

    public static readonly StratCue Uptime = UmadP1Playbook.Uptime("NAUR §1.1 (0:00:28)");
    public static readonly StratCue LightOfJudgment = UmadP1Playbook.LightOfJudgment("NAUR §1.6 (0:05:02)");

    private static string FireText(UmadP1GravenImage1State state)
    {
        var shown = state.Fire.ShowsStackIcon ? "stack" : "spread";
        var actual = state.Fire.IsStack ? "stack" : "spread";
        return state.Fire.IsLie
            ? $"O orbe de fogo é falso, então o marcador de {shown} significa {actual}."
            : $"O orbe de fogo é verdadeiro: é {actual}.";
    }

    private static string SpreadSpot(PartyRole role) => ((int)role % 4) switch
    {
        0 => "na borda norte ou sul do quadrante seguro, junto do hitbox do boss",
        1 => "na borda leste ou oeste do quadrante seguro, junto do hitbox do boss",
        2 => "mais para dentro, entre a parede e o hitbox",
        _ => "na parede, na linha leste-oeste",
    };

    private static string QuadrantSpotText(UmadP1GravenImage1State state, PartyRole role)
    {
        var quadrant = Corner(state.Ice1.SafeQuadrant(west: !UmadP1Roles.IsDps(role)));
        var spot = state.Fire.IsStack
            ? $"faça stack com os {Group(role)} no hitbox do boss, no quadrante {quadrant}"
            : $"como {Label(role)}, faça spread {SpreadSpot(role)} ({quadrant})";
        return $"{IceText(state.Ice1)} {FireText(state)} Os {Group(role)} ficam com o quadrante seguro do {Side(role)}: {spot}.";
    }

    public static StratCue KnockbackTether(UmadP1GravenImage1State state) => new(
        "Graven Image 1: tether de knockback",
        role => "Você está com tether: o Pulse Wave te joga 13y em linha reta para longe da estátua do norte, "
                + $"então fique onde ele te faz cair no seu lugar. {QuadrantSpotText(state, role)}",
        $"{Gi1} · wtfdig kefka_p1/2_graven1");

    public static StratCue MysteryMagic1(UmadP1GravenImage1State state) => new(
        "Mystery Magic 1: fogo + gelo",
        role => QuadrantSpotText(state, role),
        $"{Gi1} · wtfdig kefka_p1/{(state.Fire.IsStack ? "3_graven1_stack_positions" : "4_graven1_spread_positions")}");

    public static readonly StratCue WaveCannonLineup = new(
        "Fila do Wave Cannon",
        role => $"Faça a fila de oeste para leste: H2, H1, OT, MT | M1, M2, R1, R2, para cada raio da estátua acertar só o seu alvo. Você é {Label(role)}.",
        Beams);

    public static StratCue Towers(UmadP1GravenImage1State state) => new(
        "Torres do Wave Cannon",
        role => state.WaveTargets.Contains(role)
            ? "Um raio te acertou e deixou Magic Vulnerability Up: pegar uma torre agora te mata, então saia de todas as torres."
            : $"Nenhum raio te acertou, então você pega uma torre dos {Group(role)} no lado {Side(role)}: indo de oeste para leste, o jogador não atingido mais de fora pega a torre mais de fora.",
        Beams);

    public static StratCue Confetti(UmadP1GravenImage1State state) => new(
        "Double-trouble Trap 1 (confete)",
        role => role == state.TrapSupport || role == state.TrapDps
            ? $"Você está com o confete: fique atrás do seu grupo, no alcance máximo de melee, no {Side(role)}, para o knockback dele jogar o grupo através do boss."
            : $"Faça stack com os {Group(role)} no hitbox do boss, no {Side(role)}: o confete precisa de exatamente 4, e o knockback dele te leva para o outro lado.",
        "NAUR §1.4 (0:03:09) · wtfdig kefka_p1/6_graven1_confetti_positions");

    public static StratCue MysteryMagic2(UmadP1GravenImage1State state) => new(
        "Mystery Magic 2: raio + gelo",
        _ => $"Logo depois do knockback do confete, vá para o ponto mais próximo livre do gelo e do raio. {IceText(state.Ice2)} "
             + (state.Thunder.IsLie
                 ? "O orbe de raio é falso: as linhas que você vê são seguras e as não marcadas acertam."
                 : "O orbe de raio é verdadeiro: desvie das linhas que você vê."),
        "NAUR §1.5 (0:03:26) · wtfdig kefka_p1/7_graven1_lightning_ice_resolution");
}
