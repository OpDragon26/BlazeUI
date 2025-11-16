namespace BlazeUI.Blaze.Search;
using static Board_Representation.ZobristHash;
using Move_Generation;

public static class TranspositionTable
{
    private static ulong Size;
    private static TTEntry[] Table = [];

    public static void Init(int size)
    {
        Size = (ulong)size;
        Table = new TTEntry[size];
    }

    public static void Clear()
    {
        Table = new TTEntry[Size];
    }

    public static int Retrieve(Zobrist key, int depth, int alpha, int beta, int side)
    {
        TTEntry found = Table[key.key % Size];

        if (found.Key.Equals(key) && 
            found.Depth >= depth && 
            found.Flag != Flag.None)
        {
            if (found.Flag == Flag.Exact)
                return found.Value;
            if (side == 1 && found.Flag == Flag.Alpha && found.Value <= alpha)
                return alpha;
            if (side == 0 && found.Flag == Flag.Beta && found.Value >= beta)
                return beta;
        }
        
        return 0;
    }

    public static void Record(Zobrist key, int depth, int val, Flag flag, Move? bestMove = null)
    {
        Table[key.key % Size] = new(key, depth, flag, val, bestMove);
    }
}

public struct TTEntry(Zobrist key, int depth, Flag flag, int value, Move? bestMove)
{
    public readonly Zobrist Key = key;
    public readonly int Depth = depth;
    public readonly Flag Flag = flag;
    public readonly int Value = value;
    public  Move? BestMove = bestMove;

    public TTEntry() : this(new(), 0, Flag.None, 0, null) { }
}

public enum Flag
{
    None,
    Exact,
    Alpha,
    Beta,
}