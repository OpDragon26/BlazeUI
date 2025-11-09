using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using static EvalData;
using Utils;
using static PestoEval;

public static class GenericEval
{
    public static TEval GenerateEval<TEval, TEvalTest>(uint piece, ulong bitboard, Slice slice) where TEval : Evaluation<TEvalTest>, new() where TEvalTest : EvalTest, new()
    {
        int startRank = 6 - (int)slice * 2;
        
        TEval eval = new TEval();

        for (int file = 0; file < 8; file++)
        for (int rank = startRank; rank < startRank + 2; rank++)
        {
            if ((BitboardUtils.GetSquare(file, rank) & bitboard) != 0)
            {
                eval.MgWhite += GetWhiteMgEval(piece, file, rank) + MgValue[piece];
                eval.EgWhite += GetWhiteEgEval(piece, file, rank) + MgValue[piece];
                eval.MgBlack += GetBlackMgEval(piece, file, rank) + MgValue[piece];
                eval.EgBlack += GetBlackEgEval(piece, file, rank) + MgValue[piece];
                
                eval.WhiteTest.Add(new TEvalTest{file = file , rank = rank});
                eval.BlackTest.Add(new TEvalTest{file = file , rank = rank});
            }
        }
        
        return eval;
    }
    
    public abstract class Evaluation<TEvalTest> where TEvalTest : EvalTest
    {
        public int MgWhite;
        public int MgBlack;
        public int EgWhite;
        public int EgBlack;

        public List<TEvalTest> WhiteTest = new();
        public List<TEvalTest> BlackTest = new();

        public virtual void EvaluateWhite(ulong bitboard, ref Eval eval)
        {
            eval.MgWhite += MgWhite;
            eval.EgWhite += EgWhite;
            TestWhite(bitboard, ref eval);
        }

        public virtual void EvaluateBlack(ulong bitboard, ref Eval eval)
        {
            eval.MgBlack += MgBlack;
            eval.EgBlack += EgBlack;
            TestBlack(bitboard, ref eval);
        }
        
        public virtual void EvaluateWhite(ulong white, ulong black, ref Eval eval)
        {
            eval.MgWhite += MgWhite;
            eval.EgWhite += EgWhite;
            TestWhite(white, black, ref eval);
        }

        public virtual void EvaluateBlack(ulong white, ulong black, ref Eval eval)
        {
            eval.MgBlack += MgBlack;
            eval.EgBlack += EgBlack;
            TestBlack(white, black, ref eval);
        }

        protected void TestWhite(ulong bitboard, ref Eval eval)
        {
            foreach (EvalTest t in WhiteTest)
                if (t.Test(bitboard))
                {
                    eval.MgWhite += t.MgBonus;
                    eval.EgWhite += t.EgBonus;
                }
        }

        protected void TestBlack(ulong bitboard, ref Eval eval)
        {
            foreach (TEvalTest t in BlackTest)
                if (t.Test(bitboard))
                {
                    eval.MgBlack += t.MgBonus;
                    eval.EgBlack += t.EgBonus;
                }
        }
        
        protected void TestWhite(ulong white, ulong black, ref Eval eval)
        {
            foreach (TEvalTest t in WhiteTest)
                if (t.Test(white, black))
                {
                    eval.MgWhite += t.MgBonus;
                    eval.EgWhite += t.EgBonus;
                }
        }

        protected void TestBlack(ulong white, ulong black, ref Eval eval)
        {
            foreach (TEvalTest t in BlackTest)
                if (t.Test(white, black))
                {
                    eval.MgBlack += t.MgBonus;
                    eval.EgBlack += t.EgBonus;
                }
        }
    }

    public abstract class EvalTest(int file, int rank)
    {
        public int MgBonus;
        public int EgBonus;
        public int file = file;
        public int rank = rank;
        
        public virtual bool Test(ulong bitboard) => false;
        public virtual bool Test(ulong white, ulong black) => false;
    }
}