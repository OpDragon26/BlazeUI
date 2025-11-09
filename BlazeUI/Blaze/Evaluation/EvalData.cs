using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using static Utils.BitboardUtils;
using static Utils.EvalUtils;
using static GenericEval;
using static PestoEval;
using static Weights;
using static Board_Representation.Pieces;

public static class EvalData
{
    // used to divide the 
    public enum Slice
    {
        First, Second, Third, Fourth
    }

    public enum Section
    {
        AB, C, D, E, F, GH
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
            return GenerateEval<RookEval, RookTest>(WhiteRook, bitboard, slice, tMgBonus: OpenFileAdvantage);
        }
    }
    
    public class BishopEval : Evaluation
    {
        public static BishopEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<BishopEval>(WhiteBishop, bitboard, slice);
        }
    }
    
    public class KnightEval : Evaluation
    {
        public static KnightEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<KnightEval>(WhiteKnight, bitboard, slice);
        }
    }
    
    public class QueenEval : Evaluation
    {
        public static QueenEval GenerateNew(ulong bitboard, Slice slice)
        {
            return GenerateEval<QueenEval>(WhiteQueen, bitboard, slice);
        }
    }

    private class PassedTest(int file, int rank)
    {
        public int MiddleGameWhite(ulong blackPawns)
        {
            return IsPawnPassedWhite(blackPawns, file, rank) ? MiddleGamePassedBonus[rank] : 0;
        }

        public int MiddleGameBlack(ulong whitePawns)
        {
            return IsPawnPassedBlack(whitePawns, file, rank) ? MiddleGamePassedBonus[7 - rank] : 0;
        }
        
        public int EndGameWhite(ulong blackPawns)
        {
            return IsPawnPassedWhite(blackPawns, file, rank) ? EndGamePassedBonus[rank] : 0;
        }

        public int EndGameBlack(ulong whitePawns)
        {
            return IsPawnPassedBlack(whitePawns, file, rank) ? EndGamePassedBonus[7 - rank] : 0;
        }
    }
    
    public class PawnEval : Evaluation
    {
        private readonly List<PassedTest> Tests = new();
        
        public void EvaluateWhite(ulong blackPawns, ref Eval eval)
        {
            eval.MiddleGameWhite += MgWhite;
            eval.EndGameWhite += MgWhite;
            
            // test for passed pawns
            foreach (PassedTest t in Tests)
            {
                eval.MiddleGameWhite += t.MiddleGameWhite(blackPawns);
                eval.EndGameWhite += t.EndGameWhite(blackPawns);
            }
        }

        public void EvaluateBlack(ulong whitePawns, ref Eval eval)
        {
            eval.MiddleGameBlack += MgBlack;
            eval.EndGameBlack += EgBlack;
            
            // test for passed pawns
            foreach (PassedTest t in Tests)
            {
                eval.MiddleGameBlack += t.MiddleGameBlack(whitePawns);
                eval.EndGameBlack += t.EndGameBlack(whitePawns);
            }
        }

        public static PawnEval GenerateNew(ulong pawns, Section section)
        {
            PawnEval eval = new();
            
            (int startFile, int endFile) r = FindRelevantFiles(section);

            for (int file = r.startFile; file < r.endFile; file++)
            {
                if ((pawns & GetFile(file)) == 0)
                    continue;

                int pawnCount = CountPawns(pawns, file);

                if (pawnCount != 0)
                {
                    eval.MgWhite -= (pawnCount * pawnCount - 1) * MgDoublePawnPenalty;
                    eval.EgWhite -= (pawnCount * pawnCount - 1) * EgDoublePawnPenalty;
                    
                    eval.MgBlack -= (pawnCount * pawnCount - 1) * MgDoublePawnPenalty;
                    eval.EgBlack -= (pawnCount * pawnCount - 1) * EgDoublePawnPenalty;
                    
                    if (IsPawnIsolated(pawns, file))
                    {
                        eval.MgWhite -= pawnCount * MgIsolatedPawnPenalty;
                        eval.EgWhite -= pawnCount * EgIsolatedPawnPenalty;
                        
                        eval.MgBlack -= pawnCount * MgIsolatedPawnPenalty;
                        eval.EgBlack -= pawnCount * EgIsolatedPawnPenalty;
                    }
                }
                
                for (int rank = 1; rank < 7; rank++)
                {
                    eval.MgWhite += GetWhiteMgEval(WhitePawn, file, rank) + MiddleGameVal[WhitePawn];
                    eval.EgWhite += GetWhiteEgEval(WhitePawn, file, rank) + EndGameVal[WhitePawn];
                    eval.MgBlack += GetBlackMgEval(WhitePawn, file, rank) + MiddleGameVal[WhitePawn];
                    eval.EgBlack += GetBlackEgEval(WhitePawn, file, rank) + EndGameVal[WhitePawn];
                    
                    eval.Tests.Add(new(file, rank));
                }
            }
            
            return eval;
        }
    }
}