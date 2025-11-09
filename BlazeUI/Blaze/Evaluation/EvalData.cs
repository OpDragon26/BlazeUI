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
    
    public class RookTest : EvalTest
    {
        public RookTest() : base(8, 8) {}
        
        public RookTest(int file, int rank) : base(file, rank)
        {
            MgBonus = Weights.OpenFileAdvantage;
        }
        
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

        public static RookEval GenerateNew(uint piece, ulong bitboard, Slice slice)
        {
            return GenerateEval<RookEval, RookTest>(piece, bitboard, slice);
        }
    }
}