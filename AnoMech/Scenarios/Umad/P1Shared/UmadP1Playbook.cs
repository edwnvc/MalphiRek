using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Death-recap explanations shared by the P1 strats, after the NAUR guide (sections and video
// times) and the wtfdig images it checks against.
internal static class UmadP1Playbook
{
    public static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static string Side(PartyRole role) => UmadP1Roles.IsDps(role) ? "leste" : "oeste";

    public static string Group(PartyRole role) => UmadP1Roles.IsDps(role) ? "DPS" : "Supports";

    // A quadrant's X/Z signs as a compass corner (north is -Z).
    public static string Corner(Vector2 signs) => (signs.X > 0f, signs.Y > 0f) switch
    {
        (true, true) => "sudeste",
        (true, false) => "nordeste",
        (false, true) => "sudoeste",
        (false, false) => "noroeste",
    };

    public static string IceText(IceRoll ice)
    {
        var hit = ice.HitsNorthEast ? "nordeste e sudoeste" : "noroeste e sudeste";
        return ice.IsLie
            ? $"O orbe de gelo é falso, então os cones que você vê são os seguros; os cones de verdade acertam {hit}."
            : $"O orbe de gelo é verdadeiro: os cones acertam {hit}.";
    }

    public static StratCue Uptime(string section) => new(
        "Uptime",
        role => role == PartyRole.MainTank
            ? "Você segura o Kefka: fique ao norte dele para os cones e o Hyperdrive apontarem para longe do grupo."
            : $"Espere atrás do boss, {Group(role)} no lado sul-{Side(role)}, prontos para ir para o {Side(role)} na próxima mecânica.",
        $"{section} · wtfdig kefka_p1/1_tankbuster");

    public static StratCue LightOfJudgment(string section) => new(
        "Light of Judgment + Hyperdrive",
        role => role == PartyRole.MainTank
            ? "Você segura o Kefka: o Hyperdrive te acerta três vezes com uma AoE de 5y, então fique ao norte dele, longe de todos."
            : "Fique atrás do boss, bem longe do tank: a AoE de 5y do Hyperdrive mata qualquer outro que ela pegar.",
        section);
}
