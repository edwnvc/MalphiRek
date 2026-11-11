using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

// What UmadP3BowelsOfAgonyAi's moves are for, per role, for the death recap.
internal static class UmadP3BowelsOfAgonyPlaybook
{
    private const string Decisive = "NAUR §3.1 (0:32:15)";
    private const string Bowels = "NAUR §3.2 (0:32:54)";
    private const string ShortResolve = "NAUR §3.3 (0:34:17)";
    private const string Thunder = "NAUR §3.4 (0:35:00)";
    private const string Implosion1 = "NAUR §3.5 (0:35:25)";
    private const string Sg3k = "Icy Veins P3 · raidplan hbgw9v52x9syetck";

    private static readonly string[] SpotWords = ["sul", "sudeste", "leste", "nordeste", "norte", "noroeste", "oeste", "sudoeste"];

    private static string Spot(int spot) => SpotWords[((spot % 8) + 8) % 8];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string CrystalName(Element element) => element == Element.Water ? "água" : "fogo";

    private static string DebuffName(Element element) => element == Element.Water ? "Dynamic Fluid" : "Entropy";

    private static string CrystalShot(Element element) => element == Element.Water ? "Tsunami" : "Inferno";

    private static bool IsRanged(PartyRole role) => role is PartyRole.PhysRangedDps or PartyRole.CasterDps;

    private static string WindFacing(UmadP3BowelsOfAgonyState state, PartyRole role, string source)
        => state.Winds[role] == Wind.Headwind ? $"você tem Headwind, então fique de costas para {source}" : $"você tem Tailwind, então fique de frente para {source}";

    public static readonly StratCue DecisiveBattleSplit = new(
        "The Decisive Battle",
        role => role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB
            ? "Fique perto do Chaos no lado oeste: os quatro jogadores mais perto dele pegam Epic Hero, que te prende a ele, e o resto pega Fated Hero para o Exdeath."
            : "Fique perto do Exdeath no lado leste: os quatro jogadores mais perto do Chaos pegam Epic Hero, e vocês quatro pegam Fated Hero, que te prende ao Exdeath.",
        Decisive);

    public static readonly StratCue BossesToTheMiddle = new(
        "Bowels of Agony",
        role => role switch
        {
            PartyRole.MainTank => "Puxe o Chaos para o meio: fique a leste dele para ele andar até o centro. Bowels of Agony é um raidwide que distribui os debuffs.",
            PartyRole.OffTank => "Puxe o Exdeath para o meio: fique a oeste dele para ele andar até o centro. Bowels of Agony é um raidwide que distribui os debuffs.",
            _ => "Bata no seu boss no meio. Bowels of Agony é um raidwide que dá Headwind ou Tailwind para todo mundo e um elemento para dois pares.",
        },
        Bowels);

    public static StratCue Lb3Setup(UmadP3BowelsOfAgonyState state) => new(
        "Bowels of Agony: cristais (LB3)",
        role =>
        {
            var wind = Spot(state.WindSpot);
            if (role == PartyRole.MainTank) return $"Leve o Chaos até o cristal de vento do {wind}: fique nessa parede para ele ficar a uns 10y, virado para ela.";
            if (role == PartyRole.OffTank) return "Mantenha o Exdeath no meio: ele solta o Thunder III ali em seguida, e você o deixa e vai para a parede quando o cast começar.";
            if (IsRanged(role))
                return $"Vá para o cristal de {CrystalName(state.ShortElement)} do {Spot(state.ShortSpot)}, o curto: o {CrystalShot(state.ShortElement)} dele atira nos dois jogadores mais perto, e esses têm que ser vocês dois, a 12y um do outro.";
            return $"Faça stack na parede atrás do Chaos, perto do cristal de vento do {wind}, fora do Thunder III do Exdeath. O LB3 mantém todos os debuffs de vento para o Vacuum Wave do Limit Cut, então fique longe dos cristais.";
        },
        Bowels);

    public static StratCue Lb3ThunderAoe(UmadP3BowelsOfAgonyState state) => new(
        "Thunder III (AoE grande)",
        role => role == PartyRole.OffTank
            ? $"O Exdeath está castando o Thunder III, uma AoE que mata qualquer um a menos de uns 15y dele: deixe-o no meio e entre no stack na parede {Spot(state.WindSpot)}."
            : "O Exdeath está castando o Thunder III, uma AoE que mata qualquer um a menos de uns 15y dele: fique lá fora, na parede.",
        ShortResolve);

    public static StratCue Lb3Resolve(UmadP3BowelsOfAgonyState state, Element element) => new(
        element == state.ShortElement ? $"{CrystalName(element)} curto (LB3)" : $"{CrystalName(element)} longo (LB3)",
        role =>
        {
            var crystal = $"cristal de {CrystalName(element)} do {Spot(state.SpotOf(element))}";
            var holds = state.ElementOf(role) == element;
            if (IsRanged(role))
            {
                var spot = element == state.ShortElement
                    ? $"Fique perto do {crystal}, a 12y do outro ranged"
                    : $"Fique na linha que vai das costas do Chaos até o {crystal}, um de vocês perto e o outro longe, a mais de 10y um do outro";
                var own = holds ? $" Seu {DebuffName(element)} também acaba aqui, longe de todos." : "";
                return $"{spot}: o {CrystalShot(element)} dele atira nos dois jogadores mais perto, e esses têm que ser vocês dois.{own}";
            }
            if (element == Element.Water)
                return holds
                    ? "Seu Dynamic Fluid acaba: o donut dele não acerta ninguém a menos de 4y de você, então fique no stack apertado na parede."
                    : "Fique no stack apertado na parede: os donuts do Dynamic Fluid não acertam ninguém a menos de 4y de quem tem o debuff.";
            var side = UmadP3BowelsOfAgonyState.IsSupport(role) ? "esquerda" : "direita";
            return holds
                ? $"Seu Entropy acaba, um círculo de 5y em você: saia para a {side} do seu grupo ao longo da parede, a mais de 5y de todos."
                : $"Supports fazem stack à esquerda e DPS à direita, de frente para o Chaos, então você está no grupo da {side}; quem tem Entropy sai passando por você.";
        },
        element == state.ShortElement ? ShortResolve : "NAUR §3.5 (0:35:48)");

    public static readonly StratCue Lb3Regroup = new(
        "Thunder III: fique longe",
        role => IsRanged(role)
            ? "Fique perto do cristal, a mais de 10y do Exdeath: o buster Thunder III dele acerta quem estiver mais perto."
            : "Volte para um stack só na parede, a mais de 10y do Exdeath: o buster Thunder III dele acerta quem estiver mais perto.",
        Thunder);

    public static StratCue Lb3PreImplosion(UmadP3BowelsOfAgonyState state) => new(
        "Implosion: preparação",
        role =>
        {
            if (role == PartyRole.MainTank) return $"Volte para a parede {Spot(state.WindSpot)}, para o Chaos ficar virado para ela quando castar o Implosion.";
            if (role == PartyRole.OffTank) return $"Leve o Exdeath até o cristal de vento do {Spot(state.WindSpot)}, no lugar do Chaos a uns 10y, e deixe-o ali durante o Implosion: o Limit Cut começa com os dois bosses nesse canto.";
            if (IsRanged(role)) return $"Atravesse até o cristal de {CrystalName(state.LongElement)} longo ({Spot(state.LongSpot)}): você desvia do Implosion ali e depois pega os tiros dele.";
            return "Fique na parede perto do Chaos: você desvia do Implosion perto dele.";
        },
        Implosion1);

    public static StratCue Implosion(UmadP3BowelsOfAgonyState state, UmadP3BowelsOfAgonyAi.Strat strat, bool firstHit) => new(
        firstHit ? "Implosion (primeiro cleave)" : "Implosion (segundo cleave)",
        role =>
        {
            var sidesHit = firstHit == state.Latitudinal;
            var name = state.Latitudinal ? "Latitudinal" : "Longitudinal";
            var hit = sidesHit ? "os lados dele" : "a frente e as costas dele";
            var safe = sidesHit ? "na frente ou atrás dele" : "do lado dele";
            var rule = $"{name} Implosion: este cleave acerta {hit} (dois cones de 90°), então fique {safe}, logo fora da diagonal";
            if (strat == UmadP3BowelsOfAgonyAi.Strat.Lb3)
                return IsRanged(role)
                    ? $"{rule}, no lado do cristal de {CrystalName(state.LongElement)} longo, onde você pega os tiros dele em seguida."
                    : $"{rule}, perto do Chaos: supports à esquerda dele, DPS à direita (olhando para ele a partir da parede), os lados em que vocês pegam o {CrystalName(state.LongElement)} longo.";
            return state.ElementOf(role) != null
                ? $"{rule}: quem tem elemento desvia na direção do cristal de {CrystalName(state.LongElement)} longo ({Spot(state.LongSpot)})."
                : $"{rule}: quem não tem elemento desvia na direção do cristal de vento ({Spot(state.WindSpot)}).";
        },
        strat == UmadP3BowelsOfAgonyAi.Strat.Lb3 ? Implosion1 : Sg3k);

    public static StratCue Sg3kSetup(UmadP3BowelsOfAgonyState state) => new(
        "Bowels of Agony: cristais (SG3K)",
        role => role switch
        {
            PartyRole.MainTank => $"Leve o Chaos para entre o cristal de {CrystalName(state.ShortElement)} curto ({Spot(state.ShortSpot)}) e o cristal de vento ({Spot(state.WindSpot)}).",
            PartyRole.OffTank => $"Leve o Exdeath até a parede no cristal de {CrystalName(state.LongElement)} longo ({Spot(state.LongSpot)}): assim o Thunder III dele não alcança os outros.",
            _ => Sg3kResolve(state, state.ShortElement).Why(role),
        },
        Sg3k);

    public static StratCue Sg3kResolve(UmadP3BowelsOfAgonyState state, Element element) => new(
        element == state.ShortElement ? $"{CrystalName(element)} curto (SG3K)" : $"{CrystalName(element)} longo (SG3K)",
        role =>
        {
            var crystal = $"cristal de {CrystalName(element)} do {Spot(state.SpotOf(element))}";
            var resolvers = state.HoldersOf(element);
            var other = element == Element.Water ? Element.Fire : Element.Water;
            var cleanse = state.HoldersOf(other);
            if (role == resolvers[0])
                return $"Seu {DebuffName(element)} acaba: fique na parede atrás do {crystal}. Você e {Label(resolvers[1])} são os dois mais perto dele, então o {CrystalShot(element)} atira em vocês dois.";
            if (role == resolvers[1])
                return $"Seu {DebuffName(element)} acaba: fique a 7y para fora, na direção do {crystal}. O {CrystalShot(element)} em você empurra {Label(cleanse[0])} e {Label(cleanse[1])}, o que tira os ventos deles.";
            if (state.ElementOf(role) == other)
            {
                var where = element == Element.Water ? "dentro do círculo de 5y" : "no donut, a 4-10y dele";
                return $"Fique logo atrás de {Label(resolvers[1])}, {where}: o {CrystalShot(element)} do {crystal} te empurra e tira seu vento; {WindFacing(state, role, Label(resolvers[1]))}.";
            }
            return "Fique perto do cristal de vento com seu par: cada vento que um cristal tira vira um Cyclone, um stack de dois jogadores em quem estiver mais perto do cristal de vento.";
        },
        Sg3k);

    public static StratCue Sg3kCleanseFacing(UmadP3BowelsOfAgonyState state, Element resolving) => new(
        "Knockback do cristal",
        role =>
        {
            var other = resolving == Element.Water ? Element.Fire : Element.Water;
            var centre = Label(state.HoldersOf(resolving)[1]);
            return state.ElementOf(role) == other
                ? $"O {CrystalShot(resolving)} em {centre} te empurra: {WindFacing(state, role, centre)}, e o empurrão só tira seu vento. Virado para o lado errado, você é jogado para fora da plataforma."
                : $"O {CrystalShot(resolving)} em {centre} empurra os jogadores com {DebuffName(other)} para tirar os ventos deles; fique onde está.";
        },
        Sg3k);

    public static StratCue Sg3kPreImplosion(UmadP3BowelsOfAgonyState state) => new(
        "Implosion: preparação",
        role => role switch
        {
            PartyRole.MainTank => "Puxe o Chaos para o meio e fique ao norte dele, para ele ficar virado para o norte: assim os cones do Implosion se dividem nos intercardeais.",
            PartyRole.OffTank => $"Leve o Exdeath na direção do cristal de vento ({Spot(state.WindSpot)}), onde o Limit Cut precisa dele.",
            _ => state.ElementOf(role) != null
                ? $"Vá para o cristal de {CrystalName(state.LongElement)} longo ({Spot(state.LongSpot)}): quem tem elemento desvia do Implosion na direção dele."
                : $"Vá para o cristal de vento ({Spot(state.WindSpot)}): quem não tem elemento desvia do Implosion na direção dele.",
        },
        Sg3k);

    public static StratCue ThunderTank(UmadP3BowelsOfAgonyState state) => UmadP3BlackHolePlaybook.ThunderTank(state.ThunderPlan, Thunder);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting) => UmadP3BlackHolePlaybook.ThunderClearance(holder, waiting, Thunder);

    public static StratCue ThunderSwap(UmadP3BowelsOfAgonyState state) => UmadP3BlackHolePlaybook.ThunderSwap(state.ThunderPlan, Thunder);
}
