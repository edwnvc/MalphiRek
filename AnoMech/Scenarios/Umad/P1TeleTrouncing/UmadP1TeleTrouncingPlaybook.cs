using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1TeleTrouncing;

// What UmadP1TeleTrouncingAi's moves are for, per role, for the death recap.
internal static class UmadP1TeleTrouncingPlaybook
{
    private const string Arrows = "NAUR §1.12 (0:09:32) · wtfdig kefka_p1/17_graven3_arrow_placement";

    private static string Name(TelePortentDirection direction) => direction switch
    {
        TelePortentDirection.Up => "para cima",
        TelePortentDirection.Down => "para baixo",
        TelePortentDirection.Left => "para a esquerda",
        _ => "para a direita",
    };

    // The box side an arrow's teleporters line: each runs clockwise along it.
    private static string BoxSide(TelePortentDirection direction) => direction switch
    {
        TelePortentDirection.Right => "norte",
        TelePortentDirection.Down => "leste",
        TelePortentDirection.Left => "sul",
        _ => "oeste",
    };

    public static StratCue Arrow(UmadP1TeleTrouncingState state, bool first) => new(
        first ? "Tele-trouncing: flecha curta" : "Tele-trouncing: flecha longa",
        role =>
        {
            var (shortArrow, longArrow) = state.ExpiryDirections(role);
            var box = "Todos os teleportes ficam numa caixa no sentido horário (um quadrado de 12y), cada um no lado ao longo do qual a flecha dele corre. ";
            if (shortArrow == longArrow)
                return box + $"Suas duas flechas apontam {Name(shortArrow)}, então as duas vão no lado {BoxSide(shortArrow)}: "
                       + (first ? "a curta logo no sentido anti-horário do cardeal" : "a longa no cardeal") + ".";
            return box + $"Suas flechas são diferentes (primeiro {Name(shortArrow)}, depois {Name(longArrow)}), então você fica no canto onde os lados "
                   + $"{BoxSide(shortArrow)} e {BoxSide(longArrow)} se encontram, um teleporte de cada lado dele, "
                   + (first ? "a flecha curta primeiro." : "agora a longa.");
        },
        Arrows);

    public static StratCue Confetti(UmadP1TeleTrouncingState state) => new(
        "Double-trouble Trap 3 (confete)",
        role => (role == state.ConfettiStackSupport || role == state.ConfettiStackDps
                    ? "Você está com o confete: fique atrás do seu grupo, "
                    : "Faça stack com a sua função no anel do boss, ")
                + $"{(UmadP1Roles.IsDps(role) ? "DPS no sudeste" : "Supports no noroeste")}, para o knockback jogar o grupo através do boss e não dentro de um teleporte.",
        "NAUR §1.13 (0:11:12) · wtfdig kefka_p1/18_graven3_third_confetti");

    private static string TetherSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => "norte, logo depois do hitbox do boss",
        PartyRole.PhysRangedDps => "norte, fora da caixa de teleportes",
        PartyRole.OffTank => "oeste, logo depois do hitbox do boss",
        PartyRole.CasterDps => "oeste, fora da caixa de teleportes",
        PartyRole.MeleeDpsA => "sul, logo depois do hitbox do boss",
        PartyRole.RegenHealer => "sul, fora da caixa de teleportes",
        PartyRole.MeleeDpsB => "leste, logo depois do hitbox do boss",
        _ => "leste, fora da caixa de teleportes",
    };

    public static readonly StratCue Tethers = new(
        "Graven Image 3: tethers de Sleep / Confused",
        role => $"Seja qual for seu tether, vá para o seu lugar fixo: {TetherSpot(role)}. Cada cardeal junta um Support com um DPS, "
                + "então o que está Confused anda até o outro passando pelos teleportes e os usa; o que está dormindo só fica parado.",
        "NAUR §1.14 (0:11:30) · wtfdig kefka_p1/19_graven3_fixed_positions");

    private static string SpreadSpot(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA => "no waymark 1",
        PartyRole.MainTank => "no waymark 2",
        PartyRole.OffTank => "no waymark 3",
        PartyRole.MeleeDpsB => "no waymark 4",
        PartyRole.PhysRangedDps => "na parede norte",
        PartyRole.CasterDps => "na parede oeste",
        PartyRole.RegenHealer => "na parede sul",
        _ => "na parede leste",
    };

    public static StratCue MysteryMagic3(UmadP1TeleTrouncingState state) => new(
        "Mystery Magic 3: fogo + raio + gaze",
        role =>
        {
            var fire = state.FireIsLie
                ? $"O orbe de fogo é falso, então o marcador de {(state.FireShowsStackIcon ? "stack" : "spread")} significa {(state.FireIsStack ? "stack" : "spread")}. "
                : $"É {(state.FireIsStack ? "stack" : "spread")} de verdade. ";
            var spot = state.FireIsStack
                ? $"os {Group(role)} fazem stack no waymark {(UmadP1Roles.IsDps(role) ? "1 (noroeste)" : "3 (sudeste)")}"
                : $"você faz spread {SpreadSpot(role)}";
            var lightning = state.ThunderIsLie ? "das linhas não marcadas (o orbe de raio é falso)" : "das linhas marcadas";
            var gaze = state.GazeInverted ? "O gaze é falso (\"?\"): olhe para a estátua." : "O gaze é verdadeiro: fique de costas para a estátua.";
            return $"{fire}Como {Label(role)}, {spot}, desviando {lightning}. {gaze}";
        },
        "NAUR §1.15 (0:12:09) · wtfdig kefka_p1/20-22 (gaze static, spread, stack)");
}
