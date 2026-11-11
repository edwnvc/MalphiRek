using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P5Celestriad;

// What UmadP5CelestriadAi's moves are for, per role, for the death recap.
internal static class UmadP5CelestriadPlaybook
{
    private const string Celestriad = "NAUR §5.5 (1:06:58) · strat by RoarkGit";
    private const string Choice = "NAUR §5.6 (1:08:13) · strat by RoarkGit";

    private static string Name(CelestriadElement element)
        => element == CelestriadElement.Fire ? "fogo" : element == CelestriadElement.Lightning ? "raio" : "gelo";

    // The other role sharing this role's debuff (or lack of one): its tower partner every set.
    private static string WithPartner(UmadP5CelestriadState state, PartyRole role)
    {
        foreach (var (other, element) in state.PlayerDebuffElement)
            if (other != role && element == state.PlayerDebuffElement[role])
                return $" com {SettingsGrid.RoleLabel(other)}";
        return "";
    }

    private static string TowerText(UmadP5CelestriadState state, int set, PartyRole role)
    {
        var target = Name(state.ElementForSet(role, set));
        var partner = WithPartner(state, role);
        if (state.PlayerDebuffElement[role] is not { } own)
            return $"Você não tem vulnerabilidade, então completa o {target}, o elemento com duas torres acesas neste grupo: "
                   + $"pegue a torre do sentido horário (à direita, olhando para fora){partner}; a dupla com debuff pega a outra.";

        var rotate = set switch
        {
            0 => $"Você tem vulnerabilidade a {Name(own)}, então uma torre de {Name(own)} te mata: vá um elemento no sentido horário, para {target}.",
            1 => $"Gire mais um elemento no sentido horário, para {target}: a torre que você acabou de pegar te deixou vulnerável ao elemento dela.",
            _ => $"Gire mais um elemento no sentido horário, de volta para {target}: sua primeira vulnerabilidade a {Name(own)} acaba logo antes deste grupo resolver.",
        };
        var tower = state.ElementForSet(role, set) == state.DoubleElement[set]
            ? $"Ele tem duas torres acesas: pegue a do sentido anti-horário (à esquerda, olhando para fora){partner}; os jogadores livres pegam a outra."
            : $"Pegue a torre acesa dele{partner}, cada um a 1y do centro dela.";
        return $"{rotate} {tower}";
    }

    public static StratCue Towers(UmadP5CelestriadState state, int set) => new(
        $"Celestriad: torres do grupo {set + 1}",
        role => TowerText(state, set, role)
                + (state.AeroVariant[set] is null ? "" : " Espere ali até o Catastrophic Choice mostrar qual metade da torre é segura."),
        Celestriad);

    public static StratCue CatastrophicChoiceHalf(UmadP5CelestriadState state, int set) => new(
        $"Celestriad grupo {set + 1}: Catastrophic Choice",
        role =>
        {
            var target = Name(state.ElementForSet(role, set));
            return state.AeroVariant[set] == CatastrophicChoice.Aero
                ? $"A escolha do Kefka é vento (verde): um donut, então vá para a metade de dentro da sua torre de {target}, na direção dele."
                : $"A escolha do Kefka é terra (marrom): uma AoE em volta dele, então vá para a metade de fora da sua torre de {target}.";
        },
        Choice);
}
