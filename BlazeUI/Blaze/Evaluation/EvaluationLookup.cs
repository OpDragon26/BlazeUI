using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using Utils;
using Magic_Lookup;
using static Utils.BitboardUtils;
using static EvalData;
using static PestoEval;

public static class EvaluationLookup
{
    private static List<ulong> FirstSliceCombinations = [];
    private static List<ulong> SecondSliceCombinations = [];
    private static List<ulong> ThirdSliceCombinations = [];
    private static List<ulong> FourthSliceCombinations = [];

    public static SliceGroup[] FirstSliceLookup = [];
    public static SliceGroup[] SecondSliceLookup = [];
    public static SliceGroup[] ThirdSliceLookup = [];
    public static SliceGroup[] FourthSliceLookup = [];

    public static PawnEval[][] PawnEvalLookup = [];
    
    public class SliceGroup(RookEval rook, BishopEval bishop, KnightEval knight, QueenEval queen)
    {
        public readonly RookEval Rook = rook;
        public readonly BishopEval Bishop = bishop;
        public readonly KnightEval Knight = knight;
        public readonly QueenEval Queen = queen;
    }
    
    public static void Init()
    {
        FirstSliceCombinations = Combinations(GetSlice(Slice.First), 9);
        SecondSliceCombinations = Combinations(GetSlice(Slice.Second), 9);
        ThirdSliceCombinations = Combinations(GetSlice(Slice.Third), 9);
        FourthSliceCombinations = Combinations(GetSlice(Slice.Fourth), 9);

        FirstSliceLookup = new SliceGroup[0xFF81];
        SecondSliceLookup = new SliceGroup[0xFF81];
        ThirdSliceLookup = new SliceGroup[0xFF81];
        FourthSliceLookup = new SliceGroup[0xFF81];

        Batch.ForEach(FirstSliceCombinations, combination =>
        {
            FirstSliceLookup[combination] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.First),
                BishopEval.GenerateNew(combination, Slice.First),
                KnightEval.GenerateNew(combination, Slice.First),
                QueenEval.GenerateNew(combination, Slice.First));
        });
        
        Batch.ForEach(SecondSliceCombinations, combination =>
        {
            SecondSliceLookup[combination >> 16] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Second),
                BishopEval.GenerateNew(combination, Slice.Second),
                KnightEval.GenerateNew(combination, Slice.Second),
                QueenEval.GenerateNew(combination, Slice.Second));
        });
        
        Batch.ForEach(ThirdSliceCombinations, combination =>
        {
            ThirdSliceLookup[combination >> 32] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Third),
                BishopEval.GenerateNew(combination, Slice.Third),
                KnightEval.GenerateNew(combination, Slice.Third),
                QueenEval.GenerateNew(combination, Slice.Third));
        });
        
        Batch.ForEach(FourthSliceCombinations, combination =>
        {
            FourthSliceLookup[combination >> 48] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Fourth),
                BishopEval.GenerateNew(combination, Slice.Fourth),
                KnightEval.GenerateNew(combination, Slice.Fourth),
                QueenEval.GenerateNew(combination, Slice.Fourth));
        });
    }

    public static class Lookup
    {
        public static void RookEvalLookupWhite(ulong rooks, ulong pawns, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
        }

        public static void RookEvalLookupBlack(ulong rooks, ulong pawns, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
        }

        public static void BishopEvalLookupWhite(ulong bishops, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(bishops).Bishop.EvaluateWhite(ref eval);
            MagicLookup.SecondSliceEvalLookup(bishops).Bishop.EvaluateWhite(ref eval);
            MagicLookup.ThirdSliceEvalLookup(bishops).Bishop.EvaluateWhite(ref eval);
            MagicLookup.FourthSliceEvalLookup(bishops).Bishop.EvaluateWhite(ref eval);
        }

        public static void BishopEvalLookupBlack(ulong bishops, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(bishops).Bishop.EvaluateBlack(ref eval);
            MagicLookup.SecondSliceEvalLookup(bishops).Bishop.EvaluateBlack(ref eval);
            MagicLookup.ThirdSliceEvalLookup(bishops).Bishop.EvaluateBlack(ref eval);
            MagicLookup.FourthSliceEvalLookup(bishops).Bishop.EvaluateBlack(ref eval);
        }

        public static void KnightEvalLookupWhite(ulong knights, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(knights).Knight.EvaluateWhite(ref eval);
            MagicLookup.SecondSliceEvalLookup(knights).Knight.EvaluateWhite(ref eval);
            MagicLookup.ThirdSliceEvalLookup(knights).Knight.EvaluateWhite(ref eval);
            MagicLookup.FourthSliceEvalLookup(knights).Knight.EvaluateWhite(ref eval);
        }
        
        public static void KnightEvalLookupBlack(ulong knights, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(knights).Knight.EvaluateBlack(ref eval);
            MagicLookup.SecondSliceEvalLookup(knights).Knight.EvaluateBlack(ref eval);
            MagicLookup.ThirdSliceEvalLookup(knights).Knight.EvaluateBlack(ref eval);
            MagicLookup.FourthSliceEvalLookup(knights).Knight.EvaluateBlack(ref eval);
        }
        
        public static void QueenEvalLookupWhite(ulong queens, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(queens).Queen.EvaluateWhite(ref eval);
            MagicLookup.SecondSliceEvalLookup(queens).Queen.EvaluateWhite(ref eval);
            MagicLookup.ThirdSliceEvalLookup(queens).Queen.EvaluateWhite(ref eval);
            MagicLookup.FourthSliceEvalLookup(queens).Queen.EvaluateWhite(ref eval);
        }
        
        public static void QueenEvalLookupBlack(ulong queens, ref Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(queens).Queen.EvaluateBlack(ref eval);
            MagicLookup.SecondSliceEvalLookup(queens).Queen.EvaluateBlack(ref eval);
            MagicLookup.ThirdSliceEvalLookup(queens).Queen.EvaluateBlack(ref eval);
            MagicLookup.FourthSliceEvalLookup(queens).Queen.EvaluateBlack(ref eval);
        }
    }
}