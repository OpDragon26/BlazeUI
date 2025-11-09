using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using static EvalData;
using Utils;
using static PestoEval;

public static class GenericEval
{
    public static TEval GenerateEval<TEval, TEvalTest>(uint piece, ulong bitboard, Slice slice, int tMgBonus = 0, int tEgBonus = 0) where TEval : Evaluation<TEvalTest>, new() where TEvalTest : EvalTest, new()
    {
        TEval eval = new TEval();

        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
        {
            if ((BitboardUtils.GetSquare(file, rank) & bitboard) != 0)
            {
                eval.PhaseIncrement += PhaseIncrement[piece];
                
                eval.MgWhite += GetWhiteMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.EgWhite += GetWhiteEgEval(piece, file, rank) + EndGameVal[piece];
                eval.MgBlack += GetBlackMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.EgBlack += GetBlackEgEval(piece, file, rank) + EndGameVal[piece];
                
                eval.WhiteTest.Add(new TEvalTest{file = file , rank = rank , MgBonus = tMgBonus, EgBonus = tEgBonus});
                eval.BlackTest.Add(new TEvalTest{file = file , rank = rank , MgBonus = tMgBonus, EgBonus = tEgBonus});
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
        public int PhaseIncrement;

        public readonly List<TEvalTest> WhiteTest = new();
        public readonly List<TEvalTest> BlackTest = new();

        public virtual void EvaluateWhite(ulong bitboard, ref Eval eval)
        {
            eval.GamePhase += PhaseIncrement;
            eval.MiddleGameWhite += MgWhite;
            eval.EndGameWhite += EgWhite;
            TestWhite(bitboard, ref eval);
        }

        public virtual void EvaluateBlack(ulong bitboard, ref Eval eval)
        {
            eval.GamePhase += PhaseIncrement;
            eval.MiddleGameBlack += MgBlack;
            eval.EndGameBlack += EgBlack;
            TestBlack(bitboard, ref eval);
        }
        
        public virtual void EvaluateWhite(ulong white, ulong black, ref Eval eval)
        {
            eval.GamePhase += PhaseIncrement;
            eval.MiddleGameWhite += MgWhite;
            eval.EndGameWhite += EgWhite;
            TestWhite(white, black, ref eval);
        }

        public virtual void EvaluateBlack(ulong white, ulong black, ref Eval eval)
        {
            eval.GamePhase += PhaseIncrement;
            eval.MiddleGameBlack += MgBlack;
            eval.EndGameBlack += EgBlack;
            TestBlack(white, black, ref eval);
        }

        private void TestWhite(ulong bitboard, ref Eval eval)
        {
            foreach (TEvalTest t in WhiteTest)
                if (t.Test(bitboard))
                {
                    eval.MiddleGameWhite += t.MgBonus;
                    eval.EndGameWhite += t.EgBonus;
                }
            
        }

        private void TestBlack(ulong bitboard, ref Eval eval)
        {
            foreach (TEvalTest t in BlackTest)
                if (t.Test(bitboard))
                {
                    eval.MiddleGameBlack += t.MgBonus;
                    eval.EndGameBlack += t.EgBonus;
                }
        }
        
        private void TestWhite(ulong white, ulong black, ref Eval eval)
        {
            foreach (TEvalTest t in WhiteTest)
                if (t.Test(white, black))
                {
                    eval.MiddleGameWhite += t.MgBonus;
                    eval.EndGameWhite += t.EgBonus;
                }
        }

        private void TestBlack(ulong white, ulong black, ref Eval eval)
        {
            foreach (TEvalTest t in BlackTest)
                if (t.Test(white, black))
                {
                    eval.MiddleGameBlack += t.MgBonus;
                    eval.EndGameBlack += t.EgBonus;
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