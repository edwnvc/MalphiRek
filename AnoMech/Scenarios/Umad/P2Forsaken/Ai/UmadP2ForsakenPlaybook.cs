using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P2Forsaken.Ai;

// What each Forsaken strat's moves are for, per role, for the death recap. The strats share one
// move timeline but differ in spots and flex rules, so each gets its own instance. Left and right
// are NAUR's: facing Kefka from the towers, which puts the helpers' +X tower on the left.
public sealed class UmadP2ForsakenPlaybook
{
    private enum OddSpots { Na, Eu }

    private enum FirstSetStacks { RoleOrder, SupportLeft, ConeRoleLeft }

    private enum Flex { Partner, South }

    private const string KroxyRinonPlan = "raidplan.io/plan/UATE__aDcw1-bgVv";
    private const string SouthAdjustPlan = "raidplan.io/plan/uq7zdjvuu7uuw8fj";

    public static readonly UmadP2ForsakenPlaybook KroxyRinon =
        new("Kroxy-Rinon", KroxyRinonPlan, OddSpots.Na, FirstSetStacks.RoleOrder, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook SouthFlex =
        new("South Flex", SouthAdjustPlan, OddSpots.Na, FirstSetStacks.RoleOrder, Flex.South);

    public static readonly UmadP2ForsakenPlaybook LpduBuddies =
        new("LPDU Buddies", "raidplan.io/plan/142oXOZpPc_jh3dd", OddSpots.Eu, FirstSetStacks.SupportLeft, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook P3ZBuddyMeow =
        new("p3Z Buddy Meow", "raidplan.io/plan/lZWqxfxvyhF9sp3Z", OddSpots.Eu, FirstSetStacks.ConeRoleLeft, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook ZP6SouthAdjust =
        new("zP6 South adjust", "raidplan.io/plan/rtc1FcuZFMuyBzP6", OddSpots.Eu, FirstSetStacks.RoleOrder, Flex.South);

    private readonly string name;
    private readonly string plan;
    private readonly OddSpots odd;
    private readonly FirstSetStacks firstSetStacks;
    private readonly Flex flex;

    private UmadP2ForsakenPlaybook(string name, string plan, OddSpots odd, FirstSetStacks firstSetStacks, Flex flex)
    {
        this.name = name;
        this.plan = plan;
        this.odd = odd;
        this.firstSetStacks = firstSetStacks;
        this.flex = flex;
    }

    public StratCue Lineup => new(
        "Forsaken (raidwide)",
        role => $"Espere perto do Kefka com os {(UmadP1Roles.IsDps(role) ? "DPS no lado leste dele" : "Supports no lado oeste dele")} enquanto o raidwide do Forsaken sai; "
                + "use mitigação e escudos nele, depois leia o seu marcador e o do seu par.",
        $"NAUR §2.2 (0:15:46) · {plan}");

    // `soakers` is the helper's live list for the group soaking set index+1; the Why runs right
    // after the move is built, when its order still ranks same-marker players as the move did.
    public StratCue Towers(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers) => new(
        index % 2 == 0
            ? $"Torres do Forsaken, grupo {index + 1} (layout ímpar)"
            : $"Torres do Forsaken, grupo {index + 1} (layout par) + {EndName(EndOf(state, index))}",
        role => soakers.Contains(role) ? SoakerWhy(state, index, soakers, role) : OutsideWhy(state, index, role),
        TowerSource(index));

    public StratCue EndBait(UmadP2ForsakenState state, int occurrence)
    {
        var future = state.EndAttacks[occurrence] != EndAttack.PastsEnd;
        if (occurrence == 3)
            return new($"{EndName(state.EndAttacks[occurrence])}: últimos clones",
                _ => future
                    ? "É Future's End: os últimos clones viraram para o grupo entre as últimas torres e fazem cleave na metade da frente, então quando o cast começar todo mundo atravessa para o outro lado, atrás deles."
                    : "É Past's End: os últimos clones fazem cleave na metade de trás, então todo mundo fica junto entre as últimas torres, na frente deles.",
                $"NAUR §2.10 (0:24:17) · {plan}");
        var nextSet = occurrence * 2 + 3;
        return new($"{EndName(state.EndAttacks[occurrence])}: All Things Ending",
            _ => future
                ? $"É Future's End: todo mundo se junta no alcance máximo de melee no lado oposto às torres do grupo {nextSet}, porque o Kefka e os clones viram para o grupo e fazem cleave na metade da frente. Quando o cast começar, corra para as torres atrás deles."
                : $"É Past's End: todo mundo se junta no alcance máximo de melee entre as torres do grupo {nextSet}, porque o Kefka e os clones viram para o grupo e fazem cleave na metade de trás, o que deixa as torres seguras quando o cast começar.",
            $"NAUR §2.6 (0:20:34) · wtfdig kefka_p2/9_future_past_baits · {plan}");
    }

    public StratCue LastClonesGather(UmadP2ForsakenState state) => new(
        $"{EndName(state.EndAttacks[3])}: últimos clones",
        _ => "Todo mundo se junta no alcance máximo de melee entre as últimas torres para os últimos clones virarem para o grupo. "
             + (state.EndAttacks[3] == EndAttack.PastsEnd
                 ? "É Past's End, então você fica aqui, na frente deles, enquanto eles fazem cleave atrás."
                 : "É Future's End, então quando o cast começar você atravessa para o outro lado, atrás deles."),
        $"NAUR §2.10 (0:24:17) · {plan}");

    // The tower side the helper gives `role` this set (true = left), or null outside the soakers.
    internal bool? SoaksLeft(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
        => SoakSpot(state, index, soakers, role)?.Left;

    private (uint Marker, bool Left)? SoakSpot(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
    {
        if (!soakers.Contains(role)) return null;
        var marker = Marker(state, role);
        if (index % 2 == 0)
        {
            if (marker == LockonId.ForsakenStack)
            {
                var (left, right) = Stacks(state, index, soakers);
                return role == left ? (marker, true) : role == right ? (marker, false) : null;
            }
            if (marker == LockonId.ForsakenCone && role == Nth(state, soakers, marker, 0)) return (marker, true);
            if (marker == LockonId.ForsakenChariot && role == Nth(state, soakers, marker, 0)) return (marker, false);
            return null;
        }
        if (marker is not (LockonId.ForsakenCone or LockonId.ForsakenChariot)) return null;
        if (role == Nth(state, soakers, marker, 0)) return (marker, true);
        if (role == Nth(state, soakers, marker, 1)) return (marker, false);
        return null;
    }

    private (PartyRole? Left, PartyRole? Right) Stacks(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers)
    {
        var first = Nth(state, soakers, LockonId.ForsakenStack, 0);
        var second = Nth(state, soakers, LockonId.ForsakenStack, 1);
        if (index != 0 || first is not { } f) return (first, second);
        var keep = firstSetStacks switch
        {
            FirstSetStacks.SupportLeft => !UmadP1Roles.IsDps(f),
            FirstSetStacks.ConeRoleLeft => RoleMarker(state, f) == LockonId.ForsakenCone,
            _ => true,
        };
        return keep ? (first, second) : (second, first);
    }

    private static PartyRole? Nth(UmadP2ForsakenState state, IReadOnlyList<PartyRole> soakers, uint marker, int n)
        => soakers.Where(r => Marker(state, r) == marker).Skip(n).Select(r => (PartyRole?)r).FirstOrDefault();

    private static uint Marker(UmadP2ForsakenState state, PartyRole role)
        => state.Lockons.TryGetValue(role, out var marker) ? marker : 0;

    // The cone-or-spread marker the stack holder's role drew on set 1.
    private static uint RoleMarker(UmadP2ForsakenState state, PartyRole stackRole)
    {
        foreach (var (role, marker) in state.Lockons)
            if (UmadP1Roles.IsDps(role) == UmadP1Roles.IsDps(stackRole) && marker != LockonId.ForsakenStack)
                return marker;
        return 0;
    }

    private string SoakerWhy(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
    {
        if (SoakSpot(state, index, soakers, role) is not { } soak)
            return $"Seu grupo pega o grupo de torres {index + 1}: vá para o lugar do seu marcador numa torre, dois por torre, senão a torre mata o grupo.";
        var spot = index % 2 == 0 ? OddSoakSpot(soak.Marker, soak.Left) : EvenSoakSpot(EndOf(state, index), soak.Marker, soak.Left);
        return $"{spot} {SideReason(state, index, soakers, role, soak.Marker)}";
    }

    private string OddSoakSpot(uint marker, bool left) => (odd, marker, left) switch
    {
        (OddSpots.Na, LockonId.ForsakenStack, true) =>
            "Com stack, pegue a borda de dentro da torre da esquerda, no anel do hitbox do Kefka: quem pega o cone e o tank de fora completam os 3 que o stack precisa.",
        (OddSpots.Na, LockonId.ForsakenStack, false) =>
            "Com stack, pegue a parte norte da torre da direita, um pouco puxada para o meio: os dois DPS de fora completam os 3 que o stack precisa.",
        (OddSpots.Na, LockonId.ForsakenCone, _) =>
            "Com o cone, pegue a parte sul da torre da esquerda, um pouco para dentro da borda: você é o terceiro jogador do stack esquerdo, e seu cone sai no jogador mais perto, o healer de fora depois da borda, longe de todos.",
        (OddSpots.Na, _, _) =>
            "Com o spread, pegue a ponta sul da torre da direita, a mais de 5y do stack direito, para o seu círculo não acertar mais ninguém.",
        (_, LockonId.ForsakenStack, true) =>
            "Com stack, pegue a parte nordeste da torre da esquerda, na direção do Kefka: quem pega o cone e o tank de fora completam os 3 que o stack precisa.",
        (_, LockonId.ForsakenStack, false) =>
            "Com stack, pegue o lado sudoeste da torre da direita, na direção da outra torre, onde os dois DPS de fora completam 3; no NAUR esse stack fica na parte norte da torre.",
        (_, LockonId.ForsakenCone, _) =>
            "Com o cone, pegue a parte sudeste da torre da esquerda: você é o terceiro jogador do stack esquerdo, e seu cone sai no healer de fora logo depois da borda sul, longe de todos.",
        _ =>
            "Com o spread, pegue a parte nordeste da torre da direita, a mais de 5y do stack direito; no NAUR o spread fica na ponta sul da torre.",
    };

    private string EvenSoakSpot(EndAttack end, uint marker, bool left)
    {
        var side = left ? "esquerda" : "direita";
        var baiter = left ? "healer" : "ranged";
        var compass = left ? "oeste" : "leste";
        if (marker == LockonId.ForsakenChariot)
            return $"Com spread, pegue a borda sul da torre da {side}, o mais para trás possível, para o seu círculo não acertar quem pega o cone nem os baits.";
        return $"Com cone, pegue a borda norte da torre da {side}, onde ela cruza o anel interno do hitbox do Kefka: como um dos quatro mais perto dele você faz o bait de um golpe do {EndName(end)}, e seu cone sai no {baiter} de fora, no waymark {compass} relativo.";
    }

    private string SideReason(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role, uint marker)
    {
        var oddSet = index % 2 == 0;
        if (oddSet && marker != LockonId.ForsakenStack)
            return "Os grupos ímpares sempre põem o cone à esquerda e o spread à direita.";
        if (oddSet && index == 0 && firstSetStacks == FirstSetStacks.ConeRoleLeft)
            return $"Esta strat ({name}) põe à esquerda, no grupo 1, o stack da função que tem os cones; o NAUR sempre põe o stack de Support à esquerda.";
        if (flex == Flex.South && index is not (0 or 3))
            return $"Esta strat ({name}) te mantém na sua última torre, a não ser que seu par de torre tenha o mesmo marcador, e aí o que estiver mais ao sul troca; o NAUR reorganiza por função a cada grupo.";
        if (oddSet)
        {
            var sameRole = soakers.Any(r => r != role && Marker(state, r) == LockonId.ForsakenStack && UmadP1Roles.IsDps(r) == UmadP1Roles.IsDps(role));
            return !sameRole
                ? "O stack de Support vai para a esquerda e o de DPS para a direita."
                : UmadP1Roles.IsDps(role)
                    ? "Os dois stacks estão em DPS: o ranged fica no lado dos DPS, a direita, e o melee troca para a esquerda."
                    : "Os dois stacks estão em Supports: o healer fica no lado dos Supports, a esquerda, e o tank troca para a direita.";
        }
        var partner = Partner(role);
        if (!UmadP1Roles.IsTankOrMelee(role))
            return $"Supports vão para a esquerda e DPS para a direita; como {Kind(role)} você fica no seu lado, e seu par {Label(partner)} troca se os marcadores forem iguais.";
        return Marker(state, partner) == marker
            ? $"Supports vão para a esquerda e DPS para a direita, mas seu marcador é igual ao do seu par {Label(partner)}, então você troca."
            : $"Supports vão para a esquerda e DPS para a direita; seu marcador é diferente do do seu par {Label(partner)}, então você fica no seu lado.";
    }

    private string OutsideWhy(UmadP2ForsakenState state, int index, PartyRole role)
    {
        var soakingGroup = index is < 3 or 7 ? "A" : "B";
        var spot = index % 2 == 0 ? OddOutsideSpot(role) : EvenOutsideSpot(EndOf(state, index), role);
        return $"O Grupo {soakingGroup} pega o grupo de torres {index + 1}, então você fica fora das torres. {spot}";
    }

    private string OddOutsideSpot(PartyRole role) => (Kind(role), odd) switch
    {
        ("healer", _) =>
            "Fique logo depois da borda sul da torre da esquerda: você é o mais perto de quem pega o cone, então o cone sai em você, longe do grupo; fique a mais de 5y do stack esquerdo.",
        ("tank", OddSpots.Na) =>
            "Fique logo fora da borda nordeste da torre da esquerda, na direção do Kefka, como terceiro jogador do stack esquerdo.",
        ("tank", _) =>
            "Fique entre o Kefka e a torre da esquerda, logo fora dela, como terceiro jogador do stack esquerdo.",
        (_, OddSpots.Na) =>
            "Fique com o outro DPS de fora logo depois da borda noroeste da torre da direita: vocês dois completam os 3 do stack direito.",
        _ =>
            "Fique com o outro DPS de fora logo depois do lado sudoeste da torre da direita, entre as torres, para completar os 3 do stack direito; no NAUR você fica na borda noroeste dela.",
    };

    private string EvenOutsideSpot(EndAttack end, PartyRole role)
    {
        switch (Kind(role))
        {
            case "healer":
            case "ranged":
                var left = Kind(role) == "healer";
                var compass = left ? "oeste" : "leste";
                return $"Fique no waymark {compass} relativo (canto de fora de um numerado, borda de dentro de um com letra): você é o mais perto de quem pega o cone da torre da {(left ? "esquerda" : "direita")}, então o cone sai em você, longe dos baits do {EndName(end)}.";
            default:
                var tank = Kind(role) == "tank";
                return $"Fique no {(tank ? "noroeste" : "nordeste")} relativo, no hitbox do Kefka: com o {(tank ? "melee" : "tank")} e os dois que pegam cone, você é um dos quatro mais perto dele, então faz o bait de um golpe do {EndName(end)}.";
        }
    }

    private string TowerSource(int index)
    {
        var section = index switch
        {
            0 => "NAUR §2.4 (0:17:08)",
            1 => "NAUR §2.5 (0:18:59)",
            2 => "NAUR §2.7 (0:21:10)",
            3 => "NAUR §2.8 (0:22:53)",
            7 => "NAUR §2.10 (0:24:17)",
            _ => "NAUR §2.9 (0:23:43)",
        };
        var image = (index % 2 == 0, odd) switch
        {
            (true, OddSpots.Na) => " · wtfdig kefka_p2/3_odd_towers",
            (false, _) => " · wtfdig kefka_p2/6_even_towers_diamond_box_markers",
            _ => "",
        };
        return $"{section}{image} · {plan}";
    }

    private static EndAttack EndOf(UmadP2ForsakenState state, int index) => state.EndAttacks[(index - 1) / 2];

    private static string EndName(EndAttack end) => end == EndAttack.PastsEnd ? "Past's End" : "Future's End";

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string Kind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => "tank",
        PartyRole.RegenHealer or PartyRole.ShieldHealer => "healer",
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => "melee",
        _ => "ranged",
    };

    private static PartyRole Partner(PartyRole role)
        => (PartyRole)(UmadP1Roles.IsDps(role) ? ((int)role - 2) % 4 + 4 : ((int)role + 2) % 4);
}
