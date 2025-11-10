using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using static EvalData;
using Utils;
using static PestoEval;

public static class GenericEval
{
    public static TEval GenerateEval<TEval, TEvalTest>(uint piece, ulong bitboard, Slice slice, int tMgBonus = 0, int tEgBonus = 0) 
        where TEval : Evaluation<TEvalTest>, new() where TEvalTest : EvalTest, new()
    {
        TEval eval = new TEval();

        int startRank = 6 - (int)slice * 2;
        
        for (int file = 0; file < 8; file++)
        for (int rank = startRank; rank < startRank + 2; rank++)
        {
            if ((BitboardUtils.GetSquare(file, rank) & bitboard) != 0)
            {
                eval.White.Increment += PhaseIncrement[piece];
                eval.Black.Increment += PhaseIncrement[piece];
                
                eval.White.MgScore += GetWhiteMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.White.EgScore += GetWhiteEgEval(piece, file, rank) + EndGameVal[piece];
                eval.Black.MgScore += GetBlackMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.Black.EgScore += GetBlackEgEval(piece, file, rank) + EndGameVal[piece];
                

                eval.WhiteTest.Add(new TEvalTest{file = file , rank = rank , MgBonus = tMgBonus, EgBonus = tEgBonus});
                eval.BlackTest.Add(new TEvalTest{file = file , rank = rank , MgBonus = tMgBonus, EgBonus = tEgBonus});
                
            }
        }
        
        return eval;
    }
    
    public static TEval GenerateEval<TEval>(uint piece, ulong bitboard, Slice slice) 
        where TEval : Evaluation, new()
    {
        TEval eval = new TEval();

        int startRank = 6 - (int)slice * 2;
        
        for (int file = 0; file < 8; file++)
        for (int rank = startRank; rank < startRank + 2; rank++)
        {
            if ((BitboardUtils.GetSquare(file, rank) & bitboard) != 0)
            {
                eval.White.Increment += PhaseIncrement[piece];
                eval.Black.Increment += PhaseIncrement[piece];
                
                eval.White.MgScore += GetWhiteMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.White.EgScore += GetWhiteEgEval(piece, file, rank) + EndGameVal[piece];
                eval.Black.MgScore += GetBlackMgEval(piece, file, rank) + MiddleGameVal[piece];
                eval.Black.EgScore += GetBlackEgEval(piece, file, rank) + EndGameVal[piece];
            }
        }
        
        return eval;
    }

    public abstract class Evaluation
    {
        public EvalResult White;
        public EvalResult Black;
    }
    
    public abstract class Evaluation<TEvalTest> : Evaluation where TEvalTest : EvalTest
    {
        public readonly List<TEvalTest> WhiteTest = new();
        public readonly List<TEvalTest> BlackTest = new();
        
        public virtual EvalResult EvaluateWhite(ulong bitboard)
        {
            EvalResult result = White;
            
            foreach (TEvalTest t in WhiteTest)
                if (t.Test(bitboard))
                {
                    result.MgScore += t.MgBonus;
                    result.EgScore += t.EgBonus;
                }
            
            return result;
        }

        public virtual EvalResult EvaluateBlack(ulong bitboard)
        {
            EvalResult result = Black;
            
            foreach (TEvalTest t in BlackTest)
                if (t.Test(bitboard))
                {
                    result.MgScore += t.MgBonus;
                    result.EgScore += t.EgBonus;
                }
            
            return result;
        }
    }

    public abstract class EvalTest(int file, int rank)
    {
        public int MgBonus;
        public int EgBonus;
        public int file = file;
        public int rank = rank;
        
        public virtual bool Test(ulong bitboard) => false;
    }
}