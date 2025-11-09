using System;
using BlazeUI.Blaze.Utils;

namespace BlazeUI.Blaze.Evaluation;
using Magic_Lookup;
using static Utils.BitboardUtils;
using static EvalData;

public static class EvaluationLookup
{
    public static ulong[] FirstSliceCombinations = [];
    public static ulong[] SecondSliceCombinations = [];
    public static ulong[] ThirdSliceCombinations = [];
    public static ulong[] FourthSliceCombinations = [];

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
        FirstSliceCombinations = Combinations(GetSlice(Slice.First), 9).ToArray();
        SecondSliceCombinations = Combinations(GetSlice(Slice.Second), 9).ToArray();
        ThirdSliceCombinations = Combinations(GetSlice(Slice.Third), 9).ToArray();
        FourthSliceCombinations = Combinations(GetSlice(Slice.Fourth), 9).ToArray();

        FirstSliceLookup = new SliceGroup[0xFF81];
        SecondSliceLookup = new SliceGroup[0xFF81];
        ThirdSliceLookup = new SliceGroup[0xFF81];
        FourthSliceLookup = new SliceGroup[0xFF81];

        foreach (ulong combination in FirstSliceCombinations)
            FirstSliceLookup[combination] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.First));

        foreach (ulong combination in SecondSliceCombinations)
            SecondSliceLookup[combination >> 16] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Second));
        
        foreach (ulong combination in ThirdSliceCombinations)
            ThirdSliceLookup[combination >> 32] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Third));
        
        foreach (ulong combination in FourthSliceCombinations)
            FourthSliceLookup[combination >> 48] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Fourth));
        
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
    }
}