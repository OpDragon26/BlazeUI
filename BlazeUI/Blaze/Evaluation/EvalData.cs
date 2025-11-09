using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;

public static class EvalData
{
    public enum Slice
    {
        First, Second, Third, Fourth
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

    public abstract class EvalTest
    {
        public int MgBonus;
        public int EgBonus;
        public virtual bool Test(ulong bitboard) => false;
        public virtual bool Test(ulong white, ulong black) => false;
    }
    
    public struct Eval
    {
        public int MgWhite;
        public int MgBlack;
        public int EgWhite;
        public int EgBlack;
        public int GamePhase;

        public int Calculate()
        {
            int mgScore = MgWhite - MgBlack;
            int egScore = EgWhite - EgBlack;
            if (GamePhase > 24)
                GamePhase = 24;
            int egPhase = 24 - GamePhase;

            return (mgScore * GamePhase + egScore * egPhase) / 24;
        }
    }
}