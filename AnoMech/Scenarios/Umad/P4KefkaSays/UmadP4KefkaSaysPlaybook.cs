using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using GazeLayout = AnoMech.Scenarios.Umad.P4KefkaSays.UmadP4KefkaSaysAi.GazeLayout;

namespace AnoMech.Scenarios.Umad.P4KefkaSays;

// What UmadP4KefkaSaysAi's moves are for, per role, for the death recap. Every text works from
// the run's truth, as the bots do, never from the orbs.
internal static class UmadP4KefkaSaysPlaybook
{
    private const string Strat = "kefkabin";

    private static readonly string[] MysterySources =
        ["NAUR §4.2 (0:51:46)", "NAUR §4.5 (0:54:08)", "NAUR §4.7 (0:54:54)"];

    private static string Compass(Vector3 offset) => DeathRecap.Compass(Vector3.Zero, offset);

    // ---- Mystery Magic: Blizzard III cones and Thrumming Thunder III lines ----

    private static string RealCones(MysteryCast m) => m.BlizzardOffset == 0 ? "sudeste e noroeste" : "nordeste e sudoeste";

    private static string SafeCones(MysteryCast m) => m.BlizzardOffset == 0 ? "nordeste e sudoeste" : "noroeste e sudeste";

    private static string LineRun(MysteryCast m) => m.LightningOrientation > 0f ? "de nordeste a sudoeste" : "de noroeste a sudeste";

    // The 10y lane between the centre and the nearest real line: lines sit 5y and 15y either
    // side of the centre, and LightningOffset picks which two are real.
    private static string SafeLane(MysteryCast m)
    {
        var o = m.LightningOrientation > 0f ? 1f : -1f;
        var across = o > 0f ? new Vector3(1f, 0f, 1f) : new Vector3(1f, 0f, -1f);
        return Compass(across * (m.LightningOffset == 0 ? o : -o));
    }

    private static string MysteryText(MysteryCast m)
        => $"Os cones de gelo que acertam de verdade apontam para {RealCones(m)}, e as linhas de raio verdadeiras ({LineRun(m)}) deixam segura a faixa logo ao {SafeLane(m)} do meio.";

    public static readonly StratCue Gather = StratCue.ForAll(
        "Kefka Says",
        "Junte-se no meio: os raidwides do Chaos e do Neo Exdeath precisam de cura e mitigação em todo mundo, e todo Mystery Magic é desviado a partir daqui.",
        $"NAUR §4.1 (0:51:26) · {Strat}");

    public static StratCue MysteryMagic(UmadP4KefkaSaysState state, int cast) => new(
        $"Mystery Magic {cast + 1}",
        _ => $"{MysteryText(state.Mystery[cast])} Todo mundo faz stack num ponto só, alguns yalms fora do meio, no espaço livre dos dois.",
        $"{MysterySources[cast]} · {Strat}");

    // ---- Flood of Naught ----

    private static string Colour(bool white) => white ? "White (NAUR: roxo)" : "Black (NAUR: azul)";

    public static StratCue FloodOfNaught(UmadP4KefkaSaysState state) => new(
        "Flood of Naught",
        role =>
        {
            var i = Array.IndexOf(state.Wave3.List, role);
            if (i < 0) return "Cada jogador pega um dos dois Antilights: fique a 8y da linha central, onde o Edge of Death mata.";
            var beyondDeath = i % 2 == 0;
            var white = state.Wounds[i];
            var needWhite = beyondDeath == white;
            var leftIsNeeded = (state.Antilights[0].Antilight == Antilight.White) == needWhite;
            var side = Compass(state.NeoExdeathDirection.Apply(new Vector3(leftIsNeeded ? -1f : 1f, 0f, 0f)));
            return $"Você tem {(beyondDeath ? "Beyond Death" : "Allagan Field")} e um Wound {Colour(white)}. "
                   + (beyondDeath ? "Beyond Death pega o raio da própria cor, cujo golpe letal o limpa; " : "Allagan Field pega a outra cor, então você não toma golpe letal; ")
                   + $"o Antilight {(needWhite ? "White" : "Black")} está no lado {side}. Fique a 8y da linha central, onde o Edge of Death mata.";
        },
        $"NAUR §4.8 (0:55:55) · {Strat}");

    // ---- Stack, spread and Acceleration Bomb ----

    // Round 0 resolves at ~71s, round 1 at ~96s; each player's one bomb goes off in one of them.
    private static (int Round, bool Real)? Bomb(UmadP4KefkaSaysState state, PartyRole role)
    {
        var w1 = Array.IndexOf(state.Wave1.List, role) % 4;
        var w2 = Array.IndexOf(state.Wave2.List, role) % 4;
        return w2 == 0 ? (1, state.Wave2True)
             : w2 == 1 ? (0, state.Wave2True)
             : w1 == 0 ? (0, state.Wave1True)
             : w1 == 1 ? (1, state.Wave1True)
             : null;
    }

    private static string BombText(UmadP4KefkaSaysState state, int round, PartyRole role)
        => Bomb(state, role) is { } bomb && bomb.Round == round
            ? bomb.Real
                ? " Seu Acceleration Bomb é verdadeiro e explode agora: pare de se mexer e de usar ações antes que ele exploda."
                : " Seu Acceleration Bomb é falso e explode agora: continue se mexendo enquanto ele explode."
            : "";

    private static string ElementSpot(UmadP4KefkaSaysState state, int round, PartyRole role)
    {
        var j = Array.IndexOf(state.ElemRoles[round].List, role);
        var real = state.ElemTrue[round];
        var support = j is >= 0 and < 4;
        var (marker, spreads) = (j % 4) switch
        {
            2 => ("Forked Lightning", real),
            3 => ("Compressed Water", !real),
            _ => ((string?)null, false),
        };
        var stack = support ? "norte" : "sul";
        var spread = support ? "oeste" : "leste";
        var mine = marker is null
            ? $"Você não tem stack nem spread nesta rodada, então faz stack no {stack}."
            : $"Seu {marker} veio de um Grand Cross {(real ? "verdadeiro" : "falso")}, então é {(spreads ? "spread" : "stack")}: você vai para o {(spreads ? spread : stack)}.";
        return $"Supports fazem stack no norte e DPS no sul; o spread de Support vai para o oeste, o de DPS para o leste. {mine}";
    }

    public static StratCue Elements(UmadP4KefkaSaysState state, int round) => new(
        round == 0 ? "Stack, spread e Acceleration Bomb curtos" : "Blizzard III Blowout + stack, spread e Acceleration Bomb longos",
        role => ElementSpot(state, round, role)
                + (round == 1 ? $" Os cones de gelo verdadeiros apontam para {RealCones(state.Mystery[3])}, então cada posição sai 1y do seu cardeal para as fatias seguras de {SafeCones(state.Mystery[3])}." : "")
                + BombText(state, round, role),
        round == 0 ? $"NAUR §4.10 (0:57:05) · {Strat}" : $"NAUR §4.13 (0:58:34) · §4.10 (0:57:05) · {Strat}");

    public static readonly StratCue AccelerationBombFake = StratCue.ForAll(
        "Acceleration Bomb (falso)",
        "Seu Acceleration Bomb veio de um Grand Cross falso, então ficar parado quando ele explode te mata: continue se mexendo durante ele.",
        $"NAUR §4.10 (0:57:05) · {Strat}");

    // ---- Cursed Shriek ----

    public static StratCue Gaze(UmadP4KefkaSaysState state, GazeLayout layout, int round, bool facing) => new(
        $"Cursed Shriek {(round == 0 ? "curto" : "longo")}{(facing ? ": direção" : "")}",
        role =>
        {
            var wave = round == 0 ? state.Wave1 : state.Wave2;
            var real = round == 0 ? state.Wave1True : state.Wave2True;
            var shrieker = role == wave[0] || role == wave[4];
            var spot = GazeSpot(state, layout, round, shrieker, role.IsDps());
            var rule = real ? "O Shriek é verdadeiro: todos os outros olham para longe dos jogadores com Shriek." : "O Shriek é falso: todos os outros olham para os jogadores com Shriek.";
            var turn = facing
                ? real ? " Dê um passinho para fora agora: andar te vira para longe do meio, onde eles estão." : " Dê um passinho para dentro agora: andar te vira para o meio, onde eles estão."
                : "";
            return $"{spot} {rule}{turn}";
        },
        $"{(round == 0 ? "NAUR §4.11 (0:57:40)" : "NAUR §4.14 (0:58:56)")} · {Strat}{(layout == GazeLayout.SupportsNorthDpsSouth ? ", gazes: supports N / DPS S" : "")}");

    private static string GazeSpot(UmadP4KefkaSaysState state, GazeLayout layout, int round, bool shrieker, bool dps)
    {
        var thunder = state.Mystery[3];
        var lane = $"As linhas verdadeiras do Thrumming Thunder deixam segura a faixa logo ao {SafeLane(thunder)} do meio";
        if (round == 0 && layout == GazeLayout.SupportsNorthDpsSouth)
        {
            var north = thunder.LightningOrientation > 0f ? "nordeste" : "noroeste";
            var south = thunder.LightningOrientation > 0f ? "sudoeste" : "sudeste";
            return shrieker
                ? $"Você está com o Cursed Shriek. {lane}: fique a 2,2y do meio ao longo da borda dela, para o {(dps ? south : north)}."
                : $"{lane}: façam fila ao longo da borda dela, Supports para o {north}, DPS para o {south}, a 7,5-10y para fora, longe dos dois jogadores com Shriek.";
        }
        if (round == 0)
            return shrieker
                ? $"Você está com o Cursed Shriek. {lane}: fique logo fora do meio, dentro dela, a 1y do outro jogador com Shriek."
                : $"{lane}: façam fila a 5y do meio ao longo dela, em volta dos dois jogadores com Shriek.";
        if (layout == GazeLayout.SupportsNorthDpsSouth)
            return shrieker
                ? $"Você está com o Cursed Shriek: fique a 2y ao {(dps ? "sul" : "norte")} do meio, do lado oposto ao outro jogador com Shriek."
                : $"Supports esperam a 7,5-9y ao norte do meio, DPS ao sul, longe dos dois jogadores com Shriek a 2y de cada lado dele.";
        return shrieker
            ? $"Você está com o Cursed Shriek: fique a 2y ao {(dps ? "sul" : "norte")} do meio, do lado oposto ao outro jogador com Shriek."
            : "Façam um anel a 6y do meio em volta dos dois jogadores com Shriek.";
    }

    // ---- Stray Flames and Stray Spray ----

    public static readonly StratCue FireBait = StratCue.ForAll(
        "Bait do Entropy",
        "O Entropy solta uma AoE de 6y onde cada um estiver quando ele acabar: todo mundo faz o bait junto no meio para as AoEs se sobreporem, depois reage ao formato dela.",
        $"NAUR §4.12 (0:58:13) · {Strat}");

    public static StratCue StrayFlames(UmadP4KefkaSaysState state) => StratCue.ForAll(
        "Stray Flames",
        state.InfernoMystery.SolutionIsChariot
            ? "O Inferno era verdadeiro, então a AoE do Entropy é um círculo em volta de você: corra direto para o seu lugar de stack ou spread da próxima rodada."
            : "O Inferno era falso, então a AoE do Entropy é um donut: fique no meio, dentro do buraco de 6y dele.",
        $"NAUR §4.12 (0:58:13) · {Strat}");

    public static readonly StratCue WaterBait = StratCue.ForAll(
        "Bait do Dynamic Fluid",
        "O Dynamic Fluid solta uma AoE de 6y onde cada um estiver quando ele acabar: todo mundo faz o bait junto no meio, depois reage ao formato dela e ao Mana Release.",
        $"NAUR §4.15 (0:59:04) · {Strat}");

    public static StratCue StraySprayAndManaRelease(UmadP4KefkaSaysState state) => StratCue.ForAll(
        "Stray Spray + Mana Release",
        (state.TsunamiMystery.SolutionIsChariot
            ? "O Tsunami era falso, então a AoE do Dynamic Fluid é um círculo em volta de você: saia para além de 6y. "
            : "O Tsunami era verdadeiro, então a AoE do Dynamic Fluid é um donut: fique a menos de 6y do meio. ")
        + $"O Mana Release repete o gelo e o raio do Kefka ao mesmo tempo. {MysteryText(state.Mystery[4])} "
        + "Todo mundo faz stack no único ponto livre dos três.",
        $"NAUR §4.15 (0:59:04) · {Strat}");
}
