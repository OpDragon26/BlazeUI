using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI.Blaze.Evaluation;
using static Utils.BitboardUtils;
using static GenericEval;
using static PestoEval;

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
            return GenerateEval<RookEval, RookTest>(Pieces.WhiteRook, bitboard, slice, tMgBonus: Weights.OpenFileAdvantage);
        }
    }
}