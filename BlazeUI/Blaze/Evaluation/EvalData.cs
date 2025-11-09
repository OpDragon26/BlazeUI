namespace BlazeUI.Blaze.Evaluation;
using static Utils.BitboardUtils;
using static GenericEval;
using static PestoEval;
using static Weights;
using static Board_Representation.Pieces;

public static class EvalData
{
    public enum Slice
    {
        First, Second, Third, Fourth
    }
    
    public class RookTest() : EvalTest(8, 8)
    {
        public override bool Test(ulong pawns)
        {
            return (pawns & GetFile(file)) == 0;
        }
    }

    public class RookEval : Evaluation<RookTest>
    {
        public override void EvaluateWhite(ulong pawns, ref Eval eval)
        {
            base.EvaluateWhite(pawns, ref eval);
        }
        
        public override void EvaluateBlack(ulong pawns, ref Eval eval)
        {
            base.EvaluateBlack(pawns, ref eval);
        }

        public static RookEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<RookEval, RookTest>(WhiteRook, bitboard, slice, true, tMgBonus: OpenFileAdvantage);
        }
    }
    
    public class BishopTest() : EvalTest(8, 8) {}
    public class BishopEval : Evaluation<BishopTest>
    {
        public static BishopEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<BishopEval, BishopTest>(WhiteBishop, bitboard, slice, false);
        }
    }
    
    public class KnightTest() : EvalTest(8, 8) {}
    public class KnightEval : Evaluation<KnightTest>
    {
        public static KnightEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<KnightEval, KnightTest>(WhiteKnight, bitboard, slice, false);
        }
    }
    
    public class QueenTest() : EvalTest(8, 8) {}

    public class QueenEval : Evaluation<QueenTest>
    {
        public static QueenEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<QueenEval, QueenTest>(WhiteQueen, bitboard, slice, false);
        }
    }
}