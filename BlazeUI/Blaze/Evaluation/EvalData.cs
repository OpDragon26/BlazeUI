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
        public override EvalResult EvaluateWhite(ulong pawns)
        {
            return base.EvaluateWhite(pawns);
        }
        
        public override EvalResult EvaluateBlack(ulong pawns)
        {
            return base.EvaluateBlack(pawns);
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
        //public readonly int file = file;
        //public readonly int rank = rank;
        
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
        
        public EvalResult EvaluateWhite(ulong blackPawns)
        {
            EvalResult result = White;
            
            // test for passed pawns
            foreach (PassedTest t in Tests)
            {
                result.MgScore += t.MiddleGameWhite(blackPawns);
                result.EgScore += t.EndGameWhite(blackPawns);
            }
            
            return result;
        }

        public EvalResult EvaluateBlack(ulong whitePawns)
        {
            EvalResult result = Black;
            
            // test for passed pawns
            foreach (PassedTest t in Tests)
            {
                result.MgScore += t.MiddleGameBlack(whitePawns);
                result.EgScore += t.EndGameBlack(whitePawns);
            }
            
            return result;
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
                    eval.White.MgScore -= (pawnCount - 1) * MgDoublePawnPenalty;
                    eval.White.EgScore -= (pawnCount - 1) * EgDoublePawnPenalty;
                    
                    eval.Black.MgScore -= (pawnCount - 1) * MgDoublePawnPenalty;
                    eval.Black.EgScore -= (pawnCount - 1) * EgDoublePawnPenalty;
                
                    if (IsPawnIsolated(pawns, file))
                    {
                        eval.White.MgScore -= pawnCount * MgIsolatedPawnPenalty;
                        eval.White.EgScore -= pawnCount * EgIsolatedPawnPenalty;
                        
                        eval.Black.MgScore -= pawnCount * MgIsolatedPawnPenalty;
                        eval.Black.EgScore -= pawnCount * EgIsolatedPawnPenalty;
                    }
                }
                
                
                for (int rank = 1; rank < 7; rank++)
                {
                    if ((pawns & GetSquare(file, rank)) != 0)
                    {
                        eval.White.MgScore += GetWhiteMgEval(WhitePawn, file, rank) + MiddleGameVal[WhitePawn];
                        eval.White.EgScore += GetWhiteEgEval(WhitePawn, file, rank) + EndGameVal[WhitePawn];
                        eval.Black.MgScore += GetBlackMgEval(WhitePawn, file, rank) + MiddleGameVal[WhitePawn];
                        eval.Black.EgScore += GetBlackEgEval(WhitePawn, file, rank) + EndGameVal[WhitePawn];
                        
                        eval.Tests.Add(new(file, rank));
                    }
                }
            }
            
            return eval;
        }
    }
}