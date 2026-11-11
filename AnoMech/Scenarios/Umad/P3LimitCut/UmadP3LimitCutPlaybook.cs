using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Scenarios.Umad.P3LimitCut;

// What UmadP3LimitCutAi's moves are for, per role, for the death recap.
internal static class UmadP3LimitCutPlaybook
{
    private const string Umbra = "NAUR §3.6 (0:36:46)";
    private const string Vacuum = "NAUR §3.8 (0:37:10)";
    private const string Thunder = "NAUR §3.9 (0:38:27) · §3.4 (0:35:00)";

    // Spot index order: 0 = S, then on toward E.
    private static readonly string[] SpotWords = ["sul", "sudeste", "leste", "nordeste", "norte", "noroeste", "oeste", "sudoeste"];

    private static string Spot(int spot) => SpotWords[((spot % 8) + 8) % 8];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static StratCue Stack(UmadP3LimitCutState state) => new(
        "Umbra Smash: stack perto dos bosses",
        role =>
        {
            var corner = Spot(state.BossSpot);
            return role switch
            {
                PartyRole.MainTank => $"Segure o Chaos a uns 10y para fora, no intercardeal {corner}, com o Exdeath logo atrás dele e o grupo em stack logo para dentro: o Umbra Smash vai em quem estiver mais longe do Chaos, e esse tem que ser só o bait.",
                PartyRole.OffTank => $"Segure o Exdeath logo atrás do Chaos, no intercardeal {corner}, com o grupo em stack logo para dentro: o Umbra Smash vai em quem estiver mais longe do Chaos, e esse tem que ser só o bait.",
                _ when role == state.BaitRole => $"Por enquanto, faça stack com o grupo a 5,5y para fora, na direção dos bosses, no intercardeal {corner}: em seguida você atravessa a arena para fazer o bait do Umbra Smash.",
                _ => $"Faça stack a 5,5y para fora, na direção dos bosses, no intercardeal {corner}: o Umbra Smash vai em quem estiver mais longe do Chaos, e esse tem que ser só {Label(state.BaitRole)}, o bait.",
            };
        },
        Umbra);

    public static StratCue UmbraBait(UmadP3LimitCutState state) => new(
        "Umbra Smash: bait",
        role => role == state.BaitRole
            ? $"Você faz o bait do Umbra Smash: ele vai em quem estiver mais longe do Chaos quando o cast começa e mata qualquer um a menos de 20y de onde cai, então fique na parede {Spot(state.BossSpot + 4)}, do outro lado da arena em relação ao Chaos e ao stack."
            : $"{Label(state.BaitRole)} faz o bait do Umbra Smash na parede {Spot(state.BossSpot + 4)}; fique no stack perto dos bosses, a mais de 20y de onde ele cai.",
        Umbra);

    public static StratCue BaitBack(UmadP3LimitCutState state) => new(
        "Umbra Smash: volta para o stack",
        role => role == state.BaitRole
            ? "O Umbra Smash travou onde você estava quando o cast começou, e o Chaos cai ali: corra de volta para o stack agora, a mais de 20y do impacto."
            : $"{Label(state.BaitRole)} volta do bait do Umbra Smash; fique no stack, a mais de 20y de onde ele cai.",
        Umbra);

    public static readonly StratCue TanksIntoStack = new(
        "Umbra Smash: tanks entram no stack",
        role => role.IsTank()
            ? "O Umbra Smash já travou no bait, então largue os bosses e entre no stack: o grupo inteiro toma junto o knockback do Vacuum Wave do Exdeath."
            : "O Umbra Smash já travou no bait; os tanks entram no seu stack, para o grupo inteiro tomar junto o knockback do Vacuum Wave do Exdeath.",
        Umbra);

    public static StratCue VacuumWave(UmadP3LimitCutState state) => new(
        "Vacuum Wave",
        role => state.Winds[role] == Wind.Headwind
            ? "Você tem Headwind: fique de costas para o Exdeath, para o Vacuum Wave acertar suas costas, limpar o debuff e só te empurrar 10y. Mais de 45° de erro te joga 40y para fora da arena; o LB3 de tank cobre o dano."
            : "Você tem Tailwind: fique de frente para o Exdeath, para o Vacuum Wave acertar sua frente, limpar o debuff e só te empurrar 10y. Mais de 45° de erro te joga 40y para fora da arena; o LB3 de tank cobre o dano.",
        Vacuum);

    public static readonly StratCue Cyclones = StratCue.ForAll(
        "Cyclones",
        "Cada vento levado para o Vacuum Wave solta um Cyclone, um stack de dois jogadores cuja vulnerabilidade cai no próximo. Todo mundo se junta logo depois do meio, longe do Exdeath, para dividir todos eles debaixo do LB3 de tank.",
        Vacuum);

    public static StratCue Charges(UmadP3LimitCutState state) => new(
        "Limit Cut: investidas do Ultima Blaster",
        role =>
        {
            var number = state.NumberOf(role);
            if (number == 0) return "Cada clone investe direto no seu número: fique na parede em frente ao clone com o seu número.";
            var from = state.ChargeSpot(number - 1);
            var fill = state.Clockwise ? "anti-horário" : "horário";
            var other = number > 4 ? number - 4 : number + 4;
            return $"Você é o número {number}: o clone {number} investe em você vindo do ponto {Spot(from)}, mortal a não ser que você esteja longe. "
                   + $"Fique na parede em frente a ele, meio passo no sentido {fill} do ponto {Spot(from + 4)}, porque o clone {other} investe vindo exatamente desse ponto. "
                   + $"O número 1 começa em frente ao primeiro clone ({Spot(state.StartSpot)}) e os números seguem no sentido {fill}, contra o giro dos clones.";
        },
        "NAUR §3.9 (0:37:50)");

    public static StratCue ClearForThunder(UmadP3LimitCutState state) => new(
        "Thunder III: saia de perto do Exdeath",
        role =>
        {
            var (first, second) = ThunderIIIPlanning.Roles(state.ThunderPlan);
            var plan = $"O Thunder III do Exdeath acerta duas vezes quem estiver mais perto dele, com 3s de intervalo, e {UmadP3BlackHolePlaybook.ThunderPlan(state.ThunderPlan)}";
            if (role == first) return $"{plan}: vá para cima dele.";
            if (role == second) return $"{plan}: espere no intercardeal {Spot(state.BossSpot)}, longe do primeiro golpe, até a sua vez de trocar.";
            if (role.IsTank()) return $"{plan}: não é com você, então espere no intercardeal {Spot(state.BossSpot)}, a mais de 10y dele.";
            return $"{plan}: juntem-se a 4y do meio, longe do canto {Spot(state.BossSpot)}, a mais de 10y dele.";
        },
        Thunder);

    public static StratCue ClearForLastThunder(UmadP3LimitCutState state) => new(
        "Thunder III: saia de perto do Exdeath",
        role =>
        {
            var (first, second) = ThunderIIIPlanning.Roles(state.LastThunderPlan);
            var plan = $"O último Thunder III do Exdeath acerta duas vezes quem estiver mais perto dele, com 3s de intervalo, e {UmadP3BlackHolePlaybook.ThunderPlan(state.LastThunderPlan)}";
            if (role == first) return $"{plan}: vá para cima dele.";
            if (role == second) return $"{plan}: espere a 10y ao lado dele, longe do primeiro golpe, até a sua vez de trocar.";
            if (role.IsTank()) return $"{plan}: não é com você, então espere a 10y ao lado dele.";
            return $"{plan}: juntem-se a 4y do meio, do lado oposto a ele, para o golpe no tank não te alcançar.";
        },
        Thunder);

    public static StratCue ThunderTank(ThunderIIIAssignment plan) => UmadP3BlackHolePlaybook.ThunderTank(plan, Thunder);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting) => UmadP3BlackHolePlaybook.ThunderClearance(holder, waiting, Thunder);

    public static StratCue ThunderSwap(ThunderIIIAssignment plan) => UmadP3BlackHolePlaybook.ThunderSwap(plan, Thunder);

    public static StratCue DecisiveBattleSplit => new(
        "The Decisive Battle (segundo)",
        role => role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB
            ? "Vá para o Chaos antes de resolver: MT, H1 e os dois melees pegam Epic Hero, que te prende a ele."
            : "Vá para o Exdeath antes de resolver: OT, H2 e os dois ranged pegam Fated Hero, que te prende a ele.",
        "NAUR §3.10 (0:39:41)");

    public static StratCue Uptime => new(
        "The Decisive Battle (segundo)",
        role => (role switch
                {
                    PartyRole.MainTank => "Segure o Chaos onde ele está",
                    PartyRole.OffTank => "Segure o Exdeath onde o Thunder III o deixou",
                    PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => "Fique perto do Chaos onde ele está",
                    _ => "Fique perto do Exdeath onde o Thunder III o deixou",
                })
                + ": deixe os bosses onde estão, o Black Hole começa puxando eles para o meio. O Decisive Battle prende MT, H1 e os dois melees ao Chaos e o resto ao Exdeath.",
        "NAUR §3.10 (0:39:41)");
}
