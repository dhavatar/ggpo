namespace VectorWar
{
    enum VectorWarInputs : int
    {
        Thrust = (1 << 0),
        Brake = (1 << 1),
        RotateLeft = (1 << 2),
        RotateRight = (1 << 3),
        Fire = (1 << 4),
        Bomb = (1 << 5),
    }
}
