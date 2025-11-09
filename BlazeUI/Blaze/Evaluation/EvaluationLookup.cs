using System;
using System.Collections.Generic;
using BlazeUI.Blaze.Utils;

namespace BlazeUI.Blaze.Evaluation;
using Magic_Lookup;
using static Utils.BitboardUtils;
using static EvalData;

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

    public class SliceGroup(RookEval rook)
    {
        public readonly RookEval Rook = rook;
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
                RookEval.GenerateNew(combination, Slice.First));
        });
        
        Batch.ForEach(SecondSliceCombinations, combination =>
        {
            SecondSliceLookup[combination >> 16] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Second));
        });
        
        Batch.ForEach(ThirdSliceCombinations, combination =>
        {
            ThirdSliceLookup[combination >> 32] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Third));
        });
        
        Batch.ForEach(FourthSliceCombinations, combination =>
        {
            FourthSliceLookup[combination >> 48] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Fourth));
        });
    }

    public static class Lookup
    {
        public static void RookEvalLookupWhite(ulong rooks, ulong pawns, ref PestoEval.Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
            MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns, ref eval);
        }

        public static void RookEvalLookupBlack(ulong rooks, ulong pawns, ref PestoEval.Eval eval)
        {
            MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
            MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns, ref eval);
        }
    }
}