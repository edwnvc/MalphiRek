using AnoMech.Core.Game.Ai;

namespace AnoMech.Scenarios.Umad.P5Flood;

// What UmadP5FloodAi's moves are for, for the death recap. The whole party moves as one stack.
internal static class UmadP5FloodPlaybook
{
    private const string Flood = "NAUR §5.3 (1:03:06)";

    private const string Stack = "Fique no stack: cada golpe também solta Chaotic Flood, um stack de 6y em alguém aleatório que não é tank.";

    // Quadrant indices as UmadP5FloodAi numbers them.
    public static string Cardinal(int quadrant) => quadrant switch
    {
        0 => "norte",
        1 => "leste",
        2 => "sul",
        _ => "oeste",
    };

    public static StratCue Start(int quadrant) => StratCue.ForAll(
        "Flood: posição inicial",
        $"Os dois primeiros pares de linhas deixam livre o lado {Cardinal(quadrant)} do Kefka, então o grupo inteiro faz stack perto dele desse lado. {Stack}",
        Flood);

    public static StratCue Rotate(UmadP5FloodState state, int quadrant, int tick) => StratCue.ForAll(
        $"Flood: rotação para o golpe {tick + 1} de 4",
        $"As linhas giram no sentido {(state.RotationClockwise ? "horário" : "anti-horário")}, então logo depois de cada golpe o grupo anda 90° junto com elas, "
        + $"aqui para o lado {Cardinal(quadrant)} do Kefka. {Stack}",
        Flood);
}
