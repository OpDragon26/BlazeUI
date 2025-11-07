using System;
using System.Collections.Generic;
using System.Linq;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;

namespace BlazeUI.Blaze.Utils;

public static class PerftUtils
{
    public static MismatchedMove[] CompareResults(Move[] pseudolegal, Move[] legal)
    {
        List<MismatchedMove> mismatchedList = new();

        foreach (Move move in pseudolegal)
            if (!legal.Contains(move)) // move present in pseudolegal but not in legal -> missing move
                mismatchedList.Add(new MismatchedMove(move, Mismatch.Missing));
        foreach (Move move in legal)
            if (!pseudolegal.Contains(move)) // move present in legal but not pseudolegal -> extra move
                mismatchedList.Add(new MismatchedMove(move, Mismatch.Extra));

        return mismatchedList.ToArray();
    }
    
    public static void PrintMismatch(MismatchedMove[] moves, Board board)
    {
        Console.WriteLine("Board:");
        CLIUtils.PrintBoard(board);
        Console.WriteLine();

        foreach (MismatchedMove move in moves)
            Console.WriteLine(move);
    }
    
    public static void AnalyzeBoard(Board board)
    {
        Move[] pseudolegal = PseudoLegalMoveGen.FilterChecks(PseudoLegalMoveGen.SearchBoard(board), board);
        Move[] legal = MoveGenerator.SearchBoard(board, false).ToArray();

        if (pseudolegal.Length != legal.Length)
            PrintMismatch(CompareResults(pseudolegal,  legal), board);
        else
        {
            Console.WriteLine("Correct moves found:");
            foreach (Move move in legal)
                Console.WriteLine(move.Notate(board));
        }
    }
    
    public readonly struct MismatchedMove(Move move, Mismatch mismatch)
    {
        public override string ToString()
        {
            return $"{mismatch.ToString()} move: {move.GetUCI()}";
        }
    }

    public enum Mismatch
    {
        Extra,
        Missing
    }
}