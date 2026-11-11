using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

// What UmadP1GravenImage2Ai's moves are for, per role, for the death recap.
internal static class UmadP1GravenImage2Playbook
{
    private const string FirstTethers = "NAUR §1.7 (0:05:24)";
    private const string SecondTethers = "NAUR §1.9 (0:06:50)";
    private const string Trap = "NAUR §1.10 (0:07:09)";

    public static readonly StratCue Uptime = UmadP1Playbook.Uptime("NAUR §1.6 (0:05:02)");
    public static readonly StratCue LightOfJudgment = UmadP1Playbook.LightOfJudgment("NAUR §1.11 (0:09:15)");

    public static StratCue FirstStack(UmadP1GravenImage2State state) => new(
        "Graven Image 2: primeiras poças",
        _ => "Todo mundo faz stack no waymark A para as quatro poças de Gravitas caírem uma em cima da outra; cada Gravitas tem que acertar os 8, senão o grupo morre. "
             + $"{IceText(state.Ice)} Por isso o stack fica no lado {(state.Ice.HitsNorthEast ? "oeste" : "leste")} do A.",
        $"{FirstTethers} · wtfdig kefka_p1/8_graven2_first_puddle");

    public static readonly StratCue SecondStack = new(
        "Graven Image 2: segundas poças",
        _ => "Todo mundo faz stack no waymark C para o segundo grupo de poças cair junto no sul; cada Gravitas tem que acertar os 8, senão o grupo morre.",
        $"{SecondTethers} · wtfdig kefka_p1/12_graven2_second_puddle");

    public static StratCue Spreads(UmadP1GravenImage2State state, int set) => new(
        $"Spreads do Vitrophyre (grupo {set + 1})",
        role => UmadP1Roles.IsDps(role) == state.PurpleDps[set] ? PurpleText(set) : YellowText(role, set),
        set == 0 ? $"{FirstTethers} · wtfdig kefka_p1/9_graven2_spreads" : $"{SecondTethers} · wtfdig kefka_p1/13_graven2_second_spreads");

    private static string PurpleText(int set)
        => $"Seu tether é com a estátua central (roxa), então sua poça caiu junto com o stack: atravesse o boss para o {(set == 0 ? "sul" : "norte")}, longe dos spreads, e não volte para dentro de uma poça.";

    private static string YellowText(PartyRole role, int set)
    {
        var fromNorth = set == 0;
        var east = UmadP1Roles.IsGroupOne(role) == fromNorth;
        var where = UmadP1Roles.IsTankOrMelee(role)
            ? $"para o alcance máximo de melee no {(east ? "leste" : "oeste")}"
            : $"mais para fora, para o {(fromNorth ? east ? "nordeste" : "noroeste" : east ? "sudeste" : "sudoeste")}";
        return $"Seu tether é com a estátua da direita (amarela): um spread de 5y que empurra qualquer outro e detona qualquer poça que tocar. "
               + $"O grupo {(UmadP1Roles.IsGroupOne(role) ? 1 : 2)} se abre para a {(UmadP1Roles.IsGroupOne(role) ? "esquerda" : "direita")} de frente para o boss; como {Label(role)} você vai {where}, a mais de 10y das poças.";
    }

    public static readonly StratCue Buster = new(
        "Revolting Ruin III",
        role => role == PartyRole.MainTank
            ? "Você segura o Kefka: fique ao norte dele para os dois cones do Revolting Ruin III apontarem para longe do grupo."
            : "Junte-se ao sul do boss, fora dos cones do tank.",
        "NAUR §1.8 (0:06:29) · wtfdig kefka_p1/10_graven2_tankbuster");

    private static string CleaveText(UmadP1GravenImage2State state, int cleave)
        => state.CleaveWest[cleave]
            ? "O orbe roxo (esquerda) brilha, então a metade oeste leva o cleave: vá para a metade leste."
            : "O orbe amarelo (direita) brilha, então a metade leste leva o cleave: vá para a metade oeste.";

    public static StratCue FirstCleave(UmadP1GravenImage2State state) => new(
        "Cleave de meia sala 1",
        role => CleaveText(state, 0) + (role == PartyRole.MainTank ? " Fique ao norte do boss para o buster." : " Fique ao sul do boss, fora dos cones do tank."),
        "NAUR §1.8 (0:06:29) · wtfdig kefka_p1/11_graven2_halfroom_cleave");

    public static StratCue SecondCleave(UmadP1GravenImage2State state) => new(
        "Cleave de meia sala 2",
        role => CleaveText(state, 1) + $" Os {Group(role)} esperam ao {(UmadP1Roles.IsDps(role) ? "sul" : "norte")} do boss, prontos para o confete.",
        $"{SecondTethers} · wtfdig kefka_p1/14_graven2_second_halfroom_cleave");

    public static StratCue Confetti(UmadP1GravenImage2State state) => new(
        "Double-trouble Trap 2 (confete)",
        role => role == state.TrapSupport || role == state.TrapDps
            ? $"Você está com o confete: fique no alcance máximo de melee ao {(UmadP1Roles.IsDps(role) ? "sul" : "norte")}, ao lado das poças do seu lado, para o knockback jogar seu grupo nas poças do outro lado. Depois volte para as poças atrás de você."
            : $"Faça stack com os {Group(role)} dentro do hitbox do boss, ao {(UmadP1Roles.IsDps(role) ? "sul" : "norte")}: o knockback do confete te leva até as poças do outro lado para estourá-las.",
        $"{Trap} · wtfdig kefka_p1/15_graven2_second_confetti_positions");

    public static StratCue Soak(UmadP1GravenImage2State state) => new(
        "Soak das poças do Gravity III",
        role =>
        {
            var holder = role == state.TrapSupport || role == state.TrapDps;
            var north = !UmadP1Roles.IsDps(role) == holder;
            return $"Quatro jogadores por grupo de poças, e cada poça tem que ser estourada por 4 (o quebra-cabeça das poças do P1). "
                   + (holder ? $"Como quem segura o confete, volte andando para as poças do {(north ? "norte" : "sul")} atrás de você."
                             : $"O knockback te levou para as poças do {(north ? "norte" : "sul")}: fique até elas estourarem.");
        },
        $"{Trap} · wtfdig kefka_p1/16_puddle_soaks");
}
