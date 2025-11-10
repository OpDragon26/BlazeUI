using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using Utils;
using Magic_Lookup;
using static Utils.BitboardUtils;
using static EvalData;
using static PestoEval;

public static class EvaluationLookup
{
    public static SliceGroup[] FirstSliceLookup = [];
    public static SliceGroup[] SecondSliceLookup = [];
    public static SliceGroup[] ThirdSliceLookup = [];
    public static SliceGroup[] FourthSliceLookup = [];

    //MagicNumbers.GenerateMagicNumberParallel(Combinations(PushSectionToIndex(Section.LeftEdge, Masks.Section), 8).Distinct().ToArray(), 44);
    private static readonly (ulong magicNumber, int push, int highest) PawnEvalNumber = (5058929296669118257, 44, 1048560);
    private static readonly PawnEval[,] PawnEvalLookup = new PawnEval[6, PawnEvalNumber.highest + 1];
    
    public class SliceGroup(RookEval rook, BishopEval bishop, KnightEval knight, QueenEval queen)
    {
        public readonly RookEval Rook = rook;
        public readonly BishopEval Bishop = bishop;
        public readonly KnightEval Knight = knight;
        public readonly QueenEval Queen = queen;
    }
    
    public static void Init()
    {
        FirstSliceLookup = new SliceGroup[0xFF81];
        SecondSliceLookup = new SliceGroup[0xFF81];
        ThirdSliceLookup = new SliceGroup[0xFF81];
        FourthSliceLookup = new SliceGroup[0xFF81];

        Batch.ForEach(Combinations(GetSlice(Slice.First), 9), combination =>
        {
            FirstSliceLookup[combination] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.First),
                BishopEval.GenerateNew(combination, Slice.First),
                KnightEval.GenerateNew(combination, Slice.First),
                QueenEval.GenerateNew(combination, Slice.First));
        });
        
        Batch.ForEach(Combinations(GetSlice(Slice.Second), 9), combination =>
        {
            SecondSliceLookup[combination >> 16] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Second),
                BishopEval.GenerateNew(combination, Slice.Second),
                KnightEval.GenerateNew(combination, Slice.Second),
                QueenEval.GenerateNew(combination, Slice.Second));
        });
        
        Batch.ForEach(Combinations(GetSlice(Slice.Third), 9), combination =>
        {
            ThirdSliceLookup[combination >> 32] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Third),
                BishopEval.GenerateNew(combination, Slice.Third),
                KnightEval.GenerateNew(combination, Slice.Third),
                QueenEval.GenerateNew(combination, Slice.Third));
        });
        
        Batch.ForEach(Combinations(GetSlice(Slice.Fourth), 9), combination =>
        {
            FourthSliceLookup[combination >> 48] = new SliceGroup(
                RookEval.GenerateNew(combination, Slice.Fourth),
                BishopEval.GenerateNew(combination, Slice.Fourth),
                KnightEval.GenerateNew(combination, Slice.Fourth),
                QueenEval.GenerateNew(combination, Slice.Fourth));
        });
        
        // find a magic number 
        for (int i = 0; i < 6; i++)
        {
            Section section = (Section)i;

            List<ulong> combinations = Combinations(GetSection(section), 8);
            Batch.ForEach(combinations, combination =>
            {
                ulong magicIndex = (PushSectionToIndex(section, combination) * PawnEvalNumber.magicNumber) >> PawnEvalNumber.push;
                PawnEvalLookup[(int)section, magicIndex] = PawnEval.GenerateNew(combination, section);
            });
        }
    }

    public static class Lookup
    {
        public static EvalResult RookEvalLookupWhite(ulong rooks, ulong pawns)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns));
            result.Add(MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns));
            result.Add(MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns));
            result.Add(MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateWhite(pawns));

            return result;
        }

        public static EvalResult RookEvalLookupBlack(ulong rooks, ulong pawns)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns));
            result.Add(MagicLookup.SecondSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns));
            result.Add(MagicLookup.ThirdSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns));
            result.Add(MagicLookup.FourthSliceEvalLookup(rooks).Rook.EvaluateBlack(pawns));
            
            return result;
        }

        public static EvalResult BishopEvalLookupWhite(ulong bishops)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(bishops).Bishop.White);
            result.Add(MagicLookup.SecondSliceEvalLookup(bishops).Bishop.White);
            result.Add(MagicLookup.ThirdSliceEvalLookup(bishops).Bishop.White);
            result.Add(MagicLookup.FourthSliceEvalLookup(bishops).Bishop.White);
            
            return result;
        }

        public static EvalResult BishopEvalLookupBlack(ulong bishops)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(bishops).Bishop.Black);
            result.Add(MagicLookup.SecondSliceEvalLookup(bishops).Bishop.Black);
            result.Add(MagicLookup.ThirdSliceEvalLookup(bishops).Bishop.Black);
            result.Add(MagicLookup.FourthSliceEvalLookup(bishops).Bishop.Black);

            return result;
        }

        public static EvalResult KnightEvalLookupWhite(ulong knights)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(knights).Knight.White);
            result.Add(MagicLookup.SecondSliceEvalLookup(knights).Knight.White);
            result.Add(MagicLookup.ThirdSliceEvalLookup(knights).Knight.White);
            result.Add(MagicLookup.FourthSliceEvalLookup(knights).Knight.White);
            
            return result;
        }
        
        public static EvalResult KnightEvalLookupBlack(ulong knights)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(knights).Knight.Black);
            result.Add(MagicLookup.SecondSliceEvalLookup(knights).Knight.Black);
            result.Add(MagicLookup.ThirdSliceEvalLookup(knights).Knight.Black);
            result.Add(MagicLookup.FourthSliceEvalLookup(knights).Knight.Black);
            
            return result;
        }
        
        public static EvalResult QueenEvalLookupWhite(ulong queens)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(queens).Queen.White);
            result.Add(MagicLookup.SecondSliceEvalLookup(queens).Queen.White);
            result.Add(MagicLookup.ThirdSliceEvalLookup(queens).Queen.White);
            result.Add(MagicLookup.FourthSliceEvalLookup(queens).Queen.White);
            
            return result;
        }
        
        public static EvalResult QueenEvalLookupBlack(ulong queens)
        {
            EvalResult result = new();
            
            result.Add(MagicLookup.FirstSliceEvalLookup(queens).Queen.Black);
            result.Add(MagicLookup.SecondSliceEvalLookup(queens).Queen.Black);
            result.Add(MagicLookup.ThirdSliceEvalLookup(queens).Queen.Black);
            result.Add(MagicLookup.FourthSliceEvalLookup(queens).Queen.Black);
            
            return result;
        }

        private static PawnEval SinglePawnEvalLookup(ulong pawns, Section section)
        {
            //Console.WriteLine(section);
            //CLIUtils.PrintBitboard(pawns & GetSection(section), 0);
            return PawnEvalLookup[(int)section, (PushSectionToIndex(section, pawns & GetSection(section)) * PawnEvalNumber.magicNumber) >> PawnEvalNumber.push];
        }

        public static EvalResult PawnEvalLookupWhite(ulong whitePawns, ulong blackPawns)
        {
            EvalResult result = new();
            
            result.Add(SinglePawnEvalLookup(whitePawns, Section.AB).EvaluateWhite(blackPawns));
            result.Add(SinglePawnEvalLookup(whitePawns, Section.C).EvaluateWhite(blackPawns));
            result.Add(SinglePawnEvalLookup(whitePawns, Section.D).EvaluateWhite(blackPawns));
            result.Add(SinglePawnEvalLookup(whitePawns, Section.E).EvaluateWhite(blackPawns));
            result.Add(SinglePawnEvalLookup(whitePawns, Section.F).EvaluateWhite(blackPawns));
            result.Add(SinglePawnEvalLookup(whitePawns, Section.GH).EvaluateWhite(blackPawns));
            
            return result;
        }
        
        public static EvalResult PawnEvalLookupBlack(ulong blackPawns, ulong whitePawns)
        {
            EvalResult result = new();
            
            result.Add(SinglePawnEvalLookup(blackPawns, Section.AB).EvaluateWhite(whitePawns));
            result.Add(SinglePawnEvalLookup(blackPawns, Section.C).EvaluateWhite(whitePawns));
            result.Add(SinglePawnEvalLookup(blackPawns, Section.D).EvaluateWhite(whitePawns));
            result.Add(SinglePawnEvalLookup(blackPawns, Section.E).EvaluateWhite(whitePawns));
            result.Add(SinglePawnEvalLookup(blackPawns, Section.F).EvaluateWhite(whitePawns));
            result.Add(SinglePawnEvalLookup(blackPawns, Section.GH).EvaluateWhite(whitePawns));
            
            return result;
        }
    }
}