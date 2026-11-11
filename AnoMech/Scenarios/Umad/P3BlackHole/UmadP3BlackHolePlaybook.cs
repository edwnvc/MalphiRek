using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Umad.UmadConstants;
using TetherOrder = AnoMech.Scenarios.Umad.P3BlackHole.UmadP3BlackHoleAi.TetherOrder;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// What UmadP3BlackHoleAi's moves are for, per role, for the death recap. The Thunder III texts
// are shared with Limit Cut, which runs the same buster plan.
internal static class UmadP3BlackHolePlaybook
{
    private const string Rotation = "NAUR §3.16 (0:44:00)";
    private const string Stomps = "NAUR §3.21 (0:48:55) · old bh (n/s stomps)";
    public const string ThunderRuleSource = "NAUR §3.4 (0:35:00)";
    public const string Thunder1 = "NAUR §3.14 (0:42:40) · §3.4 (0:35:00)";
    public const string Thunder2 = "NAUR §3.17 (0:45:08) · §3.4 (0:35:00)";
    private const string ModifiedDeck = "P3 Modified DSA (Double Tethers) deck";

    private static readonly string[] WaveSources =
        ["NAUR §3.14 (0:41:49)", "NAUR §3.16 (0:44:00)", "NAUR §3.18 (0:45:55)", "NAUR §3.20 (0:48:19)"];

    private static readonly string[] SlapSources =
        ["NAUR §3.13 (0:41:20)", "NAUR §3.15 (0:42:46)", "NAUR §3.19 (0:46:27)"];

    // By D>S>A seat: the set whose laser is a seat's first. Each seat then takes the next two.
    private static readonly int[] FirstSet = [2, 5, 8, 3, 1, 4, 7, 6];

    // By D>S>A seat, Modified DSA's lasers: one player takes both of set 2, and one both of set 9.
    private static readonly string[] ModifiedSets =
        ["os grupos 1, 3 e 4", "os grupos 5, 6 e 7", "o grupo 8, depois os dois do grupo 9", "os grupos 3, 4 e 5",
         "os dois do grupo 2, depois o grupo 3", "os grupos 4, 5 e 6", "os grupos 7, 8 e 10", "os grupos 6, 7 e 8"];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string Compass(Vector3 offset) => DeathRecap.Compass(Vector3.Zero, offset);

    private static string Compass(Direction direction) => Compass(direction.Apply(new Vector3(0f, 0f, -1f)));

    // A direction with Kefka as relative north (frame -Z), on the real compass.
    private static string Relative(Direction kefka, float x, float z) => Compass(kefka.Apply(new Vector3(x, 0f, z)));

    private static string Strat(TetherOrder order) => order switch
    {
        TetherOrder.SupportDpsAccretion => "Black Hole: S>D>A",
        TetherOrder.ModifiedDsa => "Modified DSA (double tethers, n/s stomps)",
        _ => "old bh (DSA, single tethers, n/s stomps)",
    };

    // Modified DSA only changes the first and last waves; the middle two follow NAUR.
    private static string WaveSource(TetherOrder order, int wave)
        => order == TetherOrder.ModifiedDsa && wave is 0 or 3 ? ModifiedDeck : WaveSources[wave];

    // ---- Earthquake and the Black Hole tethers ----

    public static readonly StratCue Earthquake = new(
        "Earthquake",
        role => role switch
        {
            PartyRole.MainTank => "Segure o Chaos a 6y a leste do meio enquanto o OT segura o Exdeath logo a oeste, para o grupo fazer stack entre os dois.",
            PartyRole.OffTank => "Segure o Exdeath a 4y a oeste do meio enquanto o MT segura o Chaos logo a leste, para o grupo fazer stack entre os dois.",
            _ => "Faça stack no meio: o Earthquake deixa todo mundo com 1 HP e Primordial Crust, e os healers precisam de vocês juntos para curar todos.",
        },
        "NAUR §3.11 (0:40:04)");

    public static readonly StratCue AccretionHeals = StratCue.ForAll(
        "Cura do Accretion",
        "Todo mundo, tanks também, faz stack no meio: os dois jogadores com Accretion são curados um de cada vez, e cada cleanse solta um raidwide pequeno.",
        "NAUR §3.12 (0:41:00)");

    private static int Seat(UmadP3BlackHoleState state, PartyRole role) => Array.IndexOf(state.Roles.List, role);

    // S>D>A trades the support and DPS seats of each line; the Accretion seats keep theirs.
    private static int DsaSeat(TetherOrder order, int seat)
        => order != TetherOrder.SupportDpsAccretion ? seat : seat switch
        {
            <= 2 => seat + 4,
            >= 4 and <= 6 => seat - 4,
            _ => seat,
        };

    private static string Turn(UmadP3BlackHoleState state, TetherOrder order, PartyRole role)
    {
        var seat = Seat(state, role);
        if (seat < 0) return "";
        var line = UmadP3BlackHoleState.SlotLine[seat] switch { 1 => "First", 2 => "Second", _ => "Third" };
        var kind = seat is 3 or 7 ? "Accretion" : seat < 3 ? "Support" : "DPS";
        var first = FirstSet[DsaSeat(order, seat)];
        var sets = order == TetherOrder.ModifiedDsa ? ModifiedSets[seat] : $"os grupos {first}, {first + 1} e {first + 2}";
        return $"Você é {line} in Line ({kind}): seus lasers são {sets}; "
               + "os dois primeiros acumulam Unbecoming e Meanest Existence, o terceiro limpa seu Primordial Crust.";
    }

    private static string OrderText(TetherOrder order) => order switch
    {
        TetherOrder.SupportDpsAccretion => "os Supports sem Accretion pegam o primeiro tether, os DPS sem Accretion o segundo, e os Accretion o terceiro (o NAUR põe os DPS primeiro)",
        TetherOrder.ModifiedDsa => "os DPS sem Accretion pegam o primeiro tether, os Supports sem Accretion o segundo, e os Accretion o terceiro, "
                                   + "exceto que o Support First in Line pega o tether sozinho do grupo 1, o DPS First in Line os dois do grupo 2, "
                                   + "o Support Third in Line os dois do grupo 9 e o DPS Third in Line o do grupo 10",
        _ => "os DPS sem Accretion pegam o primeiro tether, os Supports sem Accretion o segundo, e os Accretion o terceiro",
    };

    public static StratCue BlackHoleWave(UmadP3BlackHoleState state, TetherOrder order, int wave) => new(
        $"Black Hole: onda {wave + 1}",
        role => "Espere no meio, longe dos black holes: só os jogadores da vez saem, e eles puxam os lasers ao longo da parede. "
                + Turn(state, order, role),
        $"{WaveSource(order, wave)} · {Strat(order)}");

    public static StratCue Grab(UmadP3BlackHoleState state, TetherOrder order, int tetherIndex) => new(
        "Black Hole: pegue seu tether",
        role =>
        {
            var which = state.ScenarioObjects.Tethers.Count == 1 ? "único" : tetherIndex switch { 0 => "primeiro", 1 => "segundo", _ => "terceiro" };
            return $"Contando no sentido horário a partir do Kefka ({Compass(state.ScenarioObjects.TetherSortFrom)}), {OrderText(order)}: "
                   + $"entre na linha do {which} tether para pegá-lo. {Turn(state, order, role)}";
        },
        $"{Rotation} · {Strat(order)}");

    // Where Movement.Intercept first aims: the nearest point on the tether, `margin` short of
    // either end.
    public static Vector2 TetherGrabSpot(SimTether tether, SimCharacter player, float margin)
    {
        var at = new Vector2(player.Position.X, player.Position.Z);
        var hole = tether.A is { } a ? new Vector2(a.Position.X, a.Position.Z) : at;
        var holder = tether.B is { } b ? new Vector2(b.Position.X, b.Position.Z) : at;
        var line = holder - hole;
        var length = line.Length();
        if (length < 1e-6f) return hole;
        var inset = margin / length;
        var (min, max) = 2f * inset >= 1f ? (0.5f, 0.5f) : (inset, 1f - inset);
        return hole + line * Math.Clamp(Vector2.Dot(at - hole, line) / (length * length), min, max);
    }

    public static StratCue GrabBoth(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: pegue os dois tethers",
        role => "Este par de tethers é só seu: você já entrou na linha de um tether, agora entre na do outro, "
                + $"para os dois black holes ficarem presos em você. {Turn(state, order, role)}",
        $"{ModifiedDeck} · {Strat(order)}");

    public static StratCue HoldBoth(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: segure os dois tethers",
        role => "Segure os dois tethers a uns 12y para fora, no intercardeal entre os dois black holes deles, desviando de qualquer black hole pequeno ali, "
                + $"para os dois lasers passarem da borda, longe do stack no meio. {Turn(state, order, role)}",
        $"{ModifiedDeck} · {Strat(order)}");

    public static readonly StratCue ExdeathToOpenCardinal = new(
        "Exdeath para o cardeal livre",
        role => role == PartyRole.OffTank
            ? "Leve o Exdeath para o cardeal logo no sentido horário do black hole do primeiro tether, o que não tem black hole nesta onda, ficando a uns 14y para fora para ele ficar a uns 10y, "
              + "e tome o Thunder III ali, atrás dos black holes do segundo par."
            : "Fique no meio: o off-tank segura o Exdeath no cardeal livre, logo no sentido horário do black hole do primeiro tether, para o Thunder III.",
        Thunder1);

    public static readonly StratCue ExdeathToFirstTether = new(
        "Exdeath na direção do primeiro tether",
        role => role == PartyRole.OffTank
            ? "Leve o Exdeath para o intercardeal logo no sentido horário do black hole do primeiro tether, ficando a uns 14y para fora para ele ficar a uns 11y, "
              + "e tome o Thunder III ali, do outro lado da arena em relação aos lasers do segundo par."
            : "Fique no meio: o off-tank segura o Exdeath no intercardeal logo no sentido horário do black hole do primeiro tether para o Thunder III, "
              + "do outro lado da arena em relação aos lasers do segundo par.",
        ModifiedDeck);

    public static StratCue Pull(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: puxe seu tether",
        role => "Estique seu tether 60° no sentido horário do black hole dele e segure a uns 10y para fora, perto do alcance máximo de melee do Chaos "
                + $"em vez de na parede, para o laser passar longe do stack no meio. {Turn(state, order, role)}",
        $"{Rotation} · {Strat(order)}");

    public static StratCue ReturnToMiddle(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: volta para o meio",
        role => $"Sua vez com este tether acabou: volte para o stack no meio, fora do caminho de todos os lasers. {Turn(state, order, role)}",
        $"{Rotation} · {Strat(order)}");

    public static StratCue LookUponSplit(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole grupo 10 + body slam do Kefka",
        role =>
        {
            var holder = state.Roles[order is TetherOrder.SupportDpsAccretion or TetherOrder.ModifiedDsa ? 6 : 2];
            var line = $"O body slam do Kefka corta uma linha de 16y pelo meio, vindo da parede {Compass(state.KefkaPosition[4])}";
            return role == holder
                ? $"{line}. Você segura o último tether: leve-o até a parede, 45° para o lado do black hole dele que sai da linha, para o laser final apontar para a parede, longe de todos."
                : $"{line}: vá para a parede oposta a {Label(holder)}, que segura o último tether, longe da linha e do laser final.";
        },
        $"{(order == TetherOrder.ModifiedDsa ? ModifiedDeck : "NAUR §3.20 (0:48:19)")} · {Strat(order)}");

    // ---- Kefka, Chaos and Exdeath around the tethers ----

    public static StratCue Slap(UmadP3BlackHoleState state, int slapIndex, int kefkaIndex) => new(
        $"Tapa do Kefka {slapIndex + 1}",
        role =>
        {
            var kefka = state.KefkaPosition[kefkaIndex];
            var where = $"O Kefka ({Compass(kefka)}) é o norte relativo.";
            if (state.SlapAttacks[slapIndex] == ActionId.SlapHappy_Left)
                return $"{where} Ele bate na metade {Relative(kefka, -1f, 0f)}, depois solta um stack de grupo que precisa dos oito: "
                       + $"todo mundo faz stack junto no lado {Relative(kefka, 1f, 0f)}.";
            var (group, rel, z) = role.IsTank() ? ("os tanks", "noroeste", -1f) : role.IsDps() ? ("os DPS", "sudoeste", 1f) : ("os healers", "oeste", 0f);
            return $"{where} Ele bate na metade {Relative(kefka, 1f, 0f)}, depois solta três cones em um tank, um healer e um DPS, "
                   + $"cada um dividido pela sua função: {group} ficam no {rel} relativo, a {Relative(kefka, -1f, z)}.";
        },
        SlapSources[slapIndex]);

    public static readonly StratCue Edict = StratCue.ForAll(
        "Damning Edict",
        "O Chaos faz cleave na metade da sala à frente dele: fique 3y atrás dele, o mais perto do meio que der, para o desvio do tapa depois ser um passo curto.",
        "NAUR §3.15 (0:42:46)");

    public static StratCue EdictAndBodySlam(UmadP3BlackHoleState state) => new(
        "Damning Edict 2 + body slam do Kefka",
        _ => $"O Damning Edict e o body slam do Kefka (uma linha de 16y pelo meio, vindo da parede {Compass(state.KefkaPosition[2])}) "
             + "saem com 1,4s de diferença, perto demais para desviar de um e depois do outro: fique num ponto só, atrás do Chaos e fora da linha.",
        "NAUR §3.17 (0:44:54)");

    public static StratCue ImplosionAnchor(UmadP3BlackHoleState state) => new(
        "Implosion: posicione o Chaos",
        role =>
        {
            var anchor = Compass(state.KefkaPosition[3].Rotate(1));
            return role == PartyRole.MainTank
                ? $"Deixe o Chaos a 5y para o {anchor}, 45° no sentido horário do Kefka, para os cones do Implosion travarem num ângulo conhecido."
                : $"O MT deixa o Chaos a 5y para o {anchor}, 45° no sentido horário do Kefka, para os cones do Implosion travarem num ângulo conhecido.";
        },
        "NAUR §3.19 (0:46:27)");

    public static StratCue Implosion(UmadP3BlackHoleState state, int shockwave) => new(
        shockwave == 0 ? "Implosion: primeiros cones" : "Implosion: segundos cones",
        _ =>
        {
            var longitudinal = state.ImplosionAttack == ActionId.LongitudinalImplosion;
            var safe = Relative(state.KefkaPosition[3], state.SlapAttacks[2] == ActionId.SlapHappy_Left ? 1f : -1f, 0f);
            return $"O {(longitudinal ? "Longitudinal" : "Latitudinal")} Implosion acerta {(longitudinal ? "a frente e as costas do Chaos, depois os lados" : "os lados do Chaos, depois a frente e as costas")}. "
                   + $"A strat fica na diagonal do Chaos, o espaço que os dois pares não acertam, inclinando ~10° para longe dos cones que vão sair, no lado {safe} que o próximo tapa do Kefka deixa seguro. "
                   + $"O NAUR, em vez disso, desvia primeiro para {(longitudinal ? "os lados" : "a frente ou as costas")} dele, perto dele.";
        },
        "NAUR §3.19 (0:46:27)");

    // ---- Thunder III (also Limit Cut's) ----

    private const string ThunderRule = "O Thunder III do Exdeath acerta duas vezes quem estiver mais perto dele, com 3s de intervalo";

    public static string ThunderPlan(ThunderIIIAssignment plan) => plan switch
    {
        ThunderIIIAssignment.MtInvulnsBoth => "o MT fica nele e usa invuln nos dois golpes",
        ThunderIIIAssignment.OtInvulnsBoth => "o OT fica nele e usa invuln nos dois golpes",
        ThunderIIIAssignment.ShareMtFirst => "o MT toma o primeiro golpe e o OT o segundo, porque o Lightning Resistance Down de um golpe torna o segundo letal",
        _ => "o OT toma o primeiro golpe e o MT o segundo, porque o Lightning Resistance Down de um golpe torna o segundo letal",
    };

    public static StratCue ThunderTank(ThunderIIIAssignment plan, string source) => new(
        "Thunder III (tank buster)",
        role => role == ThunderIIIPlanning.Roles(plan).First
            ? $"{ThunderRule}, e {ThunderPlan(plan)}: vá para cima do Exdeath para ser o mais perto."
            : $"{ThunderRule}, e {ThunderPlan(plan)}: fique a mais de 10y dele.",
        source);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting, string source = ThunderRuleSource) => new(
        "Thunder III: fique longe",
        role => role == waiting
            ? $"{ThunderRule}: espere a 10y ao lado de {Label(holder)} para o primeiro golpe não te escolher, depois troque para cima do Exdeath para o segundo."
            : $"{ThunderRule}, e {Label(holder)} está nele: afaste-se para 10y do Exdeath para ele não te escolher.",
        source);

    public static StratCue ThunderSwap(ThunderIIIAssignment plan, string source) => new(
        "Thunder III: troca de tanks",
        role =>
        {
            if (ThunderIIIPlanning.Roles(plan) is not (var first, { } second))
                return $"{ThunderRule}, e {ThunderPlan(plan)}: fique a mais de 10y dele.";
            if (role == first)
                return $"Você tomou o primeiro golpe do Thunder III e está com Lightning Resistance Down, então um segundo te mataria: troque de lugar com {Label(second)}, que vai para cima do Exdeath.";
            if (role == second)
                return $"Vá para cima do Exdeath agora, trocando de lugar com {Label(first)}: você toma o segundo golpe do Thunder III, que mataria {Label(first)} por causa do Lightning Resistance Down.";
            return $"{ThunderRule}: os tanks trocam de lugar entre os golpes, então fique a mais de 10y dele.";
        },
        source);

    public static StratCue ThunderStack(ThunderIIIAssignment plan, string source) => StratCue.ForAll(
        "Thunder III: stack",
        $"Faça stack no meio: {ThunderRule}, e {ThunderPlan(plan)}. Depois, todos os outros se afastam para 10y dele.",
        source);

    // ---- Blizzard III and Stomp a Mole ----

    private static bool GroupOne(PartyRole role) => (int)role % 2 == 0;

    public static StratCue StompPreposition(UmadP3BlackHoleState state) => new(
        "Blizzard III + Stomp a Mole: início",
        role => $"O último teleporte do Kefka ({Compass(state.KefkaPosition[4])}) agora é o norte relativo. "
                + (role.IsTank()
                    ? $"Primeiro leve o {(role == PartyRole.MainTank ? "Chaos" : "Exdeath")} de volta para o meio, depois deixe-o ali e espere a 9y ao norte relativo dele com os supports, "
                      + "para as primeiras poças do Blizzard III caírem longe dele."
                    : $"{(role.IsDps() ? "Os DPS esperam a 9y ao sul relativo" : "Os Supports esperam a 9y ao norte relativo")} do meio, para as primeiras poças do Blizzard III caírem longe dele."),
        Stomps);

    public static StratCue StompCorners(UmadP3BlackHoleState state) => new(
        "Blizzard III: segundas poças",
        role =>
        {
            var west = GroupOne(role);
            var north = !role.IsDps();
            var world = Relative(state.KefkaPosition[4], west ? -1f : 1f, north ? -1f : 1f);
            return $"Saia das primeiras poças para o canto {(north ? west ? "noroeste" : "nordeste" : west ? "sudoeste" : "sudeste")} relativo ({world}) para soltar a segunda ali: "
                   + "Supports no norte, DPS no sul, Grupo 1 no oeste, Grupo 2 no leste, dois por canto.";
        },
        Stomps);

    private static string Tower(UmadP3BlackHoleState state, PartyRole role)
    {
        var west = GroupOne(role);
        return $"O Grupo {(west ? 1 : 2)} pega a torre {(west ? "oeste" : "leste")} relativa ({Relative(state.KefkaPosition[4], west ? -1f : 1f, 0f)}), dois por torre; uma torre sem ninguém mata o grupo.";
    }

    public static StratCue StompStackAndTowers(UmadP3BlackHoleState state) => new(
        "Knock Down 1 + torres do Stomp a Mole",
        role =>
        {
            var marked = state.StackTargets[0];
            var stackers = marked.IsDps() ? "DPS" : "Supports";
            return role.IsDps() == marked.IsDps()
                ? $"O primeiro stack está em {Label(marked)}, então os {stackers} entram no meio e pegam ele juntos: ele precisa de quatro."
                : $"O primeiro stack está em {Label(marked)}, então os {stackers} pegam ele no meio e a sua função pega as torres do Kefka. {Tower(state, role)}";
        },
        Stomps);

    public static StratCue StompSwap(UmadP3BlackHoleState state, bool west) => new(
        west ? "Knock Down 2 + torres: troca do oeste" : "Knock Down 2 + torres: troca do leste",
        role =>
        {
            const string timing = "Os pares do oeste trocam primeiro, porque a torre oeste explode 1,3s antes da leste.";
            return role.IsDps() == state.StackTargets[0].IsDps()
                ? $"As funções trocam: você pegou o primeiro stack, então agora pega uma torre. {Tower(state, role)} {timing}"
                : $"As funções trocam: volte para o meio para o segundo stack, em {Label(state.StackTargets[1])}. {timing}";
        },
        Stomps);
}
