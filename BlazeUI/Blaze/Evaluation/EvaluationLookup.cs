using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using Board_Representation;
using Magic_Lookup;
using Utils;
using static EvaluationData;
public static class EvaluationLookup
{
    public static class Lookup
    {
        public static void WhitePawnLookup(ulong whitePawns, ulong blackPawns, ref EvalState evalState)
        {
            MagicLookup.PawnEvaluationLookupRight(whitePawns).FinalWhite(blackPawns, ref evalState);
            MagicLookup.PawnEvaluationLookupCenter(whitePawns).FinalWhite(blackPawns, ref evalState);
            MagicLookup.PawnEvaluationLookupLeft(whitePawns).FinalWhite(blackPawns, ref evalState);
        }
        
        public static void BlackPawnLookup(ulong blackPawns, ulong whitePawns, ref EvalState evalState)
        {
            MagicLookup.PawnEvaluationLookupRight(blackPawns).FinalBlack(whitePawns, ref evalState);
            MagicLookup.PawnEvaluationLookupCenter(blackPawns).FinalBlack(whitePawns, ref evalState);
            MagicLookup.PawnEvaluationLookupLeft(blackPawns).FinalBlack(whitePawns, ref evalState);
        }

        public static void WhiteRookLookup(ulong rooks, ulong blockers, ulong pawns, ref EvalState evalState)
        {
            MagicLookup.FirstRookEvalLookup(rooks).FinalWhite(pawns, blockers, ref evalState);
            MagicLookup.SecondRookEvalLookup(rooks).FinalWhite(pawns, blockers, ref evalState);
            MagicLookup.ThirdRookEvalLookup(rooks).FinalWhite(pawns, blockers, ref evalState);
            MagicLookup.FourthRookEvalLookup(rooks).FinalWhite(pawns, blockers, ref evalState);
        }

        public static void BlackRookLookup(ulong rooks, ulong blockers, ulong pawns, ref EvalState evalState)
        {
            MagicLookup.FirstRookEvalLookup(rooks).FinalBlack(pawns, blockers, ref evalState);
            MagicLookup.SecondRookEvalLookup(rooks).FinalBlack(pawns, blockers, ref evalState);
            MagicLookup.ThirdRookEvalLookup(rooks).FinalBlack(pawns, blockers, ref evalState);
            MagicLookup.FourthRookEvalLookup(rooks).FinalBlack(pawns, blockers, ref evalState);
        }

        public static void WhiteKnightLookup(ulong knights, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstKnightEvalLookup(knights).FinalWhite(blockers, ref evalState);
            MagicLookup.SecondKnightEvalLookup(knights).FinalWhite(blockers, ref evalState);
            MagicLookup.ThirdKnightEvalLookup(knights).FinalWhite(blockers, ref evalState);
            MagicLookup.FourthKnightEvalLookup(knights).FinalWhite(blockers, ref evalState);
        }
        
        public static void BlackKnightLookup(ulong knights, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstKnightEvalLookup(knights).FinalBlack(blockers, ref evalState);
            MagicLookup.SecondKnightEvalLookup(knights).FinalBlack(blockers, ref evalState);
            MagicLookup.ThirdKnightEvalLookup(knights).FinalBlack(blockers, ref evalState);
            MagicLookup.FourthKnightEvalLookup(knights).FinalBlack(blockers, ref evalState);
        }
        
        public static void WhiteBishopLookup(ulong Bishops, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstBishopEvalLookup(Bishops).FinalWhite(blockers, ref evalState);
            MagicLookup.SecondBishopEvalLookup(Bishops).FinalWhite(blockers, ref evalState);
            MagicLookup.ThirdBishopEvalLookup(Bishops).FinalWhite(blockers, ref evalState);
            MagicLookup.FourthBishopEvalLookup(Bishops).FinalWhite(blockers, ref evalState);
        }
        
        public static void BlackBishopLookup(ulong Bishops, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstBishopEvalLookup(Bishops).FinalBlack(blockers, ref evalState);
            MagicLookup.SecondBishopEvalLookup(Bishops).FinalBlack(blockers, ref evalState);
            MagicLookup.ThirdBishopEvalLookup(Bishops).FinalBlack(blockers, ref evalState);
            MagicLookup.FourthBishopEvalLookup(Bishops).FinalBlack(blockers, ref evalState);
        }
        
        public static void WhiteQueenLookup(ulong Queens, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstQueenEvalLookup(Queens).FinalWhite(blockers, ref evalState);
            MagicLookup.SecondQueenEvalLookup(Queens).FinalWhite(blockers, ref evalState);
            MagicLookup.ThirdQueenEvalLookup(Queens).FinalWhite(blockers, ref evalState);
            MagicLookup.FourthQueenEvalLookup(Queens).FinalWhite(blockers, ref evalState);
        }
        
        public static void BlackQueenLookup(ulong Queens, ulong blockers, ref EvalState evalState)
        {
            MagicLookup.FirstQueenEvalLookup(Queens).FinalBlack(blockers, ref evalState);
            MagicLookup.SecondQueenEvalLookup(Queens).FinalBlack(blockers, ref evalState);
            MagicLookup.ThirdQueenEvalLookup(Queens).FinalBlack(blockers, ref evalState);
            MagicLookup.FourthQueenEvalLookup(Queens).FinalBlack(blockers, ref evalState);
        }
    }
    
    public struct EvalState
    {
        public int MgScore;
        public int EgScore;
        public int Phase;

        public int TaperedEval()
        {
            int mgPhase = Phase >= 24 ? 24 : Phase;
            int egPhase = 24 - mgPhase;
        
            return (MgScore * mgPhase + EgScore * egPhase) / 24;
        }
    }
    
    public static int EvaluateKnightMobility(int file, int rank)
    {
        ulong controlled = Bitboards.KnightMasks[file, rank];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier 
                     + ulong.PopCount(controlled & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier) * 3;
    }

    public static int EvaluateRookMobility(int file, int rank, int index)
    {
        ulong controlled = Bitboards.SmallRookBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
            + ulong.PopCount(controlled & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
    }

    public static int EvaluateBishopMobility(int file, int rank, int index)
    {
        ulong controlled = Bitboards.SmallBishopBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
            + ulong.PopCount(controlled & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
    }
}