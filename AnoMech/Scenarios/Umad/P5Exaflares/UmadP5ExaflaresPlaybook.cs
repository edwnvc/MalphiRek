using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P5Exaflares;

// What UmadP5ExaflaresAi's moves are for, per role, for the death recap.
internal static class UmadP5ExaflaresPlaybook
{
    private const string Apocalypse = "NAUR §5.8 (1:10:18) · strat by Wydox";
    private const string Entropy = "NAUR §5.9 (1:10:56) · strat by Wydox";

    private static bool IsInner(PartyRole role) => role.IsTank() || role is PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;

    private static bool FromNorthwest(int wave) => wave % 2 == 0;

    // The pair is centred on Kefka only for lines 2 and 5; 1+4 and 3+6 leave an open lane beyond them.
    private static bool PairOffCentre(UmadP5ExaflaresState state, int wave)
    {
        var order = FromNorthwest(wave) ? state.LeftOrder : state.RightOrder;
        var first = wave / 2 * 2;
        return order[first] + order[first + 1] != 7;
    }

    public static readonly StratCue FanOut = StratCue.ForAll(
        "Stray Apocalypse: espalhar no início",
        "Antes da primeira onda, saia do bolo inicial e vá para o anel de melee em volta do Kefka, na sua própria direção, "
        + "para ninguém dividir o mesmo lugar quando começar o primeiro desvio. O NAUR não define posições iniciais.",
        Apocalypse);

    public static StratCue Wave(UmadP5ExaflaresState state, int wave) => new(
        $"Stray Apocalypse: onda {wave + 1} de 6",
        role =>
        {
            var lane = IsInner(role)
                ? "para o espaço entre as duas linhas dela, o mais perto do Kefka que o espaço deixar, para manter uptime"
                : PairOffCentre(state, wave)
                    ? "para a faixa segura mais próxima: o espaço entre as duas linhas ou, como elas estão fora do centro, a faixa livre depois delas (o NAUR sempre usa o espaço entre as linhas)"
                    : "para o ponto mais próximo no espaço entre as duas linhas, afastado dos outros";
            var hold = wave == 0 ? "" : " Nunca ande para frente ou para trás: isso cruza o fogo da onda anterior, que ainda está rolando.";
            return $"A onda {wave + 1} vem do {(FromNorthwest(wave) ? "noroeste" : "nordeste")}: fique de frente para ela e ande só para a esquerda ou para a direita, {lane}.{hold}";
        },
        Apocalypse);

    public static readonly StratCue Spread = new(
        "Stray Entropy: espalhar",
        role => "Stray Entropy coloca uma AoE de 5y em todo mundo, e duas sobrepostas matam. "
                + (IsInner(role)
                    ? "Tanks e melees se espalham em volta do Kefka, dentro do alcance máximo de melee mas fora do centro dele, o mais longe uns dos outros que der."
                    : "Healers e ranged vão para cerca de 13y do Kefka, cada um na sua direção, e se afastam uns 10y entre si, deixando o anel do boss para os melees."),
        Entropy);
}
