using System;

namespace BlazeUI.Blaze.Search;
using static Board_Representation.ZobristHash;
using Move_Generation;

public static class History
{
    private const int HistoryMax = 150;
    
    private static readonly int[,,,,] HistoryTable = new int[2,8,8,8,8];

    public static int Get(Move move, int side)
    {
        return HistoryTable[side, move.Source.file, move.Source.rank, move.Destination.file, move.Destination.rank];
    }

    public static void Set(Move move, int side, int bonus)
    {
        int clamped = Math.Clamp(bonus, -HistoryMax, HistoryMax);
        HistoryTable[side, move.Source.file, move.Source.rank, move.Destination.file, move.Destination.rank]
            += clamped - Get(move, side) * Math.Abs(clamped) / HistoryMax;
    }
}


public static class Counter
{
    private static readonly CounterEntry[,,,] CounterTable = new CounterEntry[8,8,8,8];

    private struct CounterEntry(Move move, int bonus)
    {
        public readonly Move move = move;
        public readonly int bonus = bonus;
    }

    public static int Get(Move? previous, Move counter)
    {
        if (previous is null)
            return 0;
        CounterEntry result = CounterTable[previous.Source.file, previous.Source.rank, previous.Destination.file, previous.Destination.rank];
        return counter.Equals(result.move) ? result.bonus : 0;
    }

    public static void Set(Move? previous, Move counter, int bonus)
    {
        if (previous is null) 
            return;
        CounterTable[previous.Source.file, previous.Source.rank, previous.Destination.file, previous.Destination.rank] = new CounterEntry(counter, bonus);
    }
}

public static class RefutationTable
{
    private static HashEntry[] Table = [];
    private static ulong Size;
    private static bool init;

    public static void Init(int size)
    {
        init = true;
        Size = (ulong)size;
        Table = new HashEntry[size];
    }

    public static bool TryGet(Zobrist zobrist, out HashEntry result)
    {
        if (!init)
            throw new InvalidOperationException("Table not initialized");
        result = Table[zobrist.key % Size];
        return result.filled && result.zobrist.Equals(zobrist);
    }

    public static void Set(Zobrist zobrist, Move move, byte bonus)
    {
        if (!init)
            throw new InvalidOperationException("Table not initialized");
        Table[zobrist.key % Size] = new HashEntry(zobrist, move, bonus);
    }
    
    public readonly struct HashEntry(Zobrist zobrist, Move move, byte bonus)
    {
        public readonly bool filled = true;
        public readonly Zobrist zobrist = zobrist;
        public readonly Move move = move;
        public readonly byte bonus = bonus;
    }
}