using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze;

public static class Evaluation
{
    public static class Lookup
    {
        public static int PawnRegular(int side, ulong friendly, ulong enemy)
        {
            return MagicLookup.PawnEvaluationLookupRight(friendly).GetFinal(enemy, side) +
                   MagicLookup.PawnEvaluationLookupCenter(friendly).GetFinal(enemy, side) +
                   MagicLookup.PawnEvaluationLookupLeft(friendly).GetFinal(enemy, side);

        }

        public static int RookRegular(int side, ulong rooks, ulong blockers, ulong friendly, ulong enemy)
        {
            return MagicLookup.FirstRookEvalLookup(rooks).GetResult(enemy, friendly, blockers, side) +
                   MagicLookup.SecondRookEvalLookup(rooks).GetResult(enemy, friendly, blockers, side) +
                   MagicLookup.ThirdRookEvalLookup(rooks).GetResult(enemy, friendly, blockers, side) +
                   MagicLookup.FourthRookEvalLookup(rooks).GetResult(enemy, friendly, blockers, side);

        }

        public static int QueenRegular(int side, ulong queens, ulong blockers)
        {
            return MagicLookup.FirstQueenEvalLookup(queens).GetFinal(blockers, side) +
                   MagicLookup.SecondQueenEvalLookup(queens).GetFinal(blockers, side) +
                   MagicLookup.ThirdQueenEvalLookup(queens).GetFinal(blockers, side) +
                   MagicLookup.FourthQueenEvalLookup(queens).GetFinal(blockers, side);
        }

        public static int KnightRegular(int side, ulong knights, ulong blockers)
        {
            return MagicLookup.FirstKnightEvalLookup(knights).GetFinal(blockers, side) +
                   MagicLookup.SecondKnightEvalLookup(knights).GetFinal(blockers, side) +
                   MagicLookup.ThirdKnightEvalLookup(knights).GetFinal(blockers, side) +
                   MagicLookup.FourthKnightEvalLookup(knights).GetFinal(blockers, side);
        }

        public static int BishopRegular(int side, ulong bishops, ulong blockers)
        {
            return MagicLookup.FirstBishopEvalLookup(bishops).GetFinal(blockers, side) +
                   MagicLookup.SecondBishopEvalLookup(bishops).GetFinal(blockers, side) +
                   MagicLookup.ThirdBishopEvalLookup(bishops).GetFinal(blockers, side) +
                   MagicLookup.FourthBishopEvalLookup(bishops).GetFinal(blockers, side);
        }
        
        
        public static int PawnEndgame(int side, ulong friendly, ulong enemy)
        {
            return MagicLookup.PawnEvaluationLookupRight(friendly).GetFinalEndgame(enemy, side) +
                   MagicLookup.PawnEvaluationLookupCenter(friendly).GetFinalEndgame(enemy, side) +
                   MagicLookup.PawnEvaluationLookupLeft(friendly).GetFinalEndgame(enemy, side);

        }

        public static int RookEndgame(int side, ulong rooks, ulong blockers)
        {
            return MagicLookup.FirstRookEvalLookup(rooks).GetFinalEndgame(blockers, side) +
                   MagicLookup.SecondRookEvalLookup(rooks).GetFinalEndgame(blockers, side) +
                   MagicLookup.ThirdRookEvalLookup(rooks).GetFinalEndgame(blockers, side) +
                   MagicLookup.FourthRookEvalLookup(rooks).GetFinalEndgame(blockers, side);

        }

        public static int QueenEndgame(int side, ulong queens, ulong blockers)
        {
            return MagicLookup.FirstQueenEvalLookup(queens).GetFinalEndgame(blockers, side) +
                   MagicLookup.SecondQueenEvalLookup(queens).GetFinalEndgame(blockers, side) +
                   MagicLookup.ThirdQueenEvalLookup(queens).GetFinalEndgame(blockers, side) +
                   MagicLookup.FourthQueenEvalLookup(queens).GetFinalEndgame(blockers, side);
        }

        public static int KnightEndgame(int side, ulong knights, ulong blockers)
        {
            return MagicLookup.FirstKnightEvalLookup(knights).GetFinalEndgame(blockers, side) +
                   MagicLookup.SecondKnightEvalLookup(knights).GetFinalEndgame(blockers, side) +
                   MagicLookup.ThirdKnightEvalLookup(knights).GetFinalEndgame(blockers, side) +
                   MagicLookup.FourthKnightEvalLookup(knights).GetFinalEndgame(blockers, side);
        }

        public static int BishopEndgame(int side, ulong bishops, ulong blockers)
        {
            return MagicLookup.FirstBishopEvalLookup(bishops).GetFinalEndgame(blockers, side) +
                   MagicLookup.SecondBishopEvalLookup(bishops).GetFinalEndgame(blockers, side) +
                   MagicLookup.ThirdBishopEvalLookup(bishops).GetFinalEndgame(blockers, side) +
                   MagicLookup.FourthBishopEvalLookup(bishops).GetFinalEndgame(blockers, side);
        }
    }
    
    public enum Section
    {
        Right, Left, Center
    }
    
    public static PawnEvaluation GeneratePawnEval(ulong pawnCombination, Section boardSide)
    {
        PawnEvaluation eval = new();
        List<PassedBonus> wPassedBonuses = new();
        List<PassedBonus> bPassedBonuses = new();
        List<PassedBonus> wPassedBonusesEndgame = new();
        List<PassedBonus> bPassedBonusesEndgame = new();

        ulong relevantPawns = pawnCombination & boardSide switch
        {
            Section.Right => Bitboards.RightPawnMask,
            Section.Left => Bitboards.LeftPawnMask,
            Section.Center => Bitboards.CenterPawnMask,
            _ => throw new Exception("no")
        };
        int startAtFile = boardSide switch
        {
            Section.Left => 0,
            Section.Center => 3,
            Section.Right => 5,
            _ => throw new Exception("no")
        };
        int endAtFile = boardSide switch {
            Section.Left => 3,
            Section.Center => 5,
            Section.Right => 8,
            _ => throw new Exception("no")
        };

        for (int file = startAtFile; file < endAtFile; file++)
        {
            if ((BitboardUtils.GetFile(file) & relevantPawns) == 0)
                continue;
            
            for (int rank = 1; rank < 7; rank++)
            {
                if ((BitboardUtils.GetSquare(file, rank) & relevantPawns) != 0)
                {
                    // material and weight at the square
                    eval.wEval += (int)(Pieces.Value[Pieces.WhitePawn] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhitePawn] + Weights.Pieces[Pieces.WhitePawn, file, rank]);
                    eval.bEval += (int)(Pieces.Value[Pieces.BlackPawn] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhitePawn] - Weights.Pieces[Pieces.WhitePawn, file, 7-rank]);
                    
                    eval.wEval += (int)(Pieces.Value[Pieces.WhitePawn] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhitePawn] + Weights.EndgamePieces[Pieces.WhitePawn, file, rank]);
                    eval.bEval += (int)(Pieces.Value[Pieces.BlackPawn] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhitePawn] - Weights.EndgamePieces[Pieces.WhitePawn, file, 7-rank]);
                    
                    // protected
                    eval.wEval += Weights.ProtectedPawnBonus * (int)ulong.PopCount(pawnCombination & Bitboards.WhitePawnCaptureMasks[file, rank]);
                    eval.bEval -= Weights.ProtectedPawnBonus * (int)ulong.PopCount(pawnCombination & Bitboards.BlackPawnCaptureMasks[file, rank]);
                    
                    eval.wEvalEndgame += Weights.ProtectedPawnBonus * (int)ulong.PopCount(pawnCombination & Bitboards.WhitePawnCaptureMasks[file, rank]);
                    eval.bEvalEndgame -= Weights.ProtectedPawnBonus * (int)ulong.PopCount(pawnCombination & Bitboards.BlackPawnCaptureMasks[file, rank]);
                    
                    // passed masks
                    wPassedBonuses.Add(new PassedBonus(BitboardUtils.GetWhitePassedPawnMask(file, rank), Weights.WhitePassedPawnBonuses[rank]));
                    bPassedBonuses.Add(new PassedBonus(BitboardUtils.GetBlackPassedPawnMask(file, rank), Weights.BlackPassedPawnBonuses[rank]));
                    
                    wPassedBonusesEndgame.Add(new PassedBonus(BitboardUtils.GetWhitePassedPawnMask(file, rank), Weights.EndgameWhitePassedPawnBonuses[rank]));
                    bPassedBonusesEndgame.Add(new PassedBonus(BitboardUtils.GetBlackPassedPawnMask(file, rank), Weights.EndgameBlackPassedPawnBonuses[rank]));
                    
                    if ((Bitboards.NeighbourMasks[file] & pawnCombination) == 0)
                    {
                        eval.wEval += Weights.IsolatedPawnPenalty;
                        eval.bEval -= Weights.IsolatedPawnPenalty;
                        
                        eval.wEvalEndgame += Weights.IsolatedPawnPenalty;
                        eval.bEvalEndgame -= Weights.IsolatedPawnPenalty;
                    }
                }
            }
        }
        
        eval.wPassedPawnChecks = wPassedBonuses.ToArray();
        eval.bPassedPawnChecks = bPassedBonuses.ToArray();
        eval.wPassedPawnChecksEndgame = wPassedBonusesEndgame.ToArray();
        eval.bPassedPawnChecksEndgame = bPassedBonusesEndgame.ToArray();
        
        return eval;
    }
    
    public class PawnEvaluation
    {
        public int wEval;
        public int bEval;
        public int wEvalEndgame;
        public int bEvalEndgame;
        public PassedBonus[] wPassedPawnChecks = [];
        public PassedBonus[] bPassedPawnChecks = [];
        public PassedBonus[] wPassedPawnChecksEndgame = [];
        public PassedBonus[] bPassedPawnChecksEndgame = [];

        public int GetFinal(ulong enemyPawns, int side)
        {
            int final;
            if (side == 0)
            {
                final = wEval;
                foreach (PassedBonus p in wPassedPawnChecks)
                    if (p.Test(enemyPawns)) final += p.bonus;
            }
            else
            {
                final = bEval;
                foreach (PassedBonus p in bPassedPawnChecks)
                    if (p.Test(enemyPawns)) final += p.bonus;
            }
            
            return final;
        }
        
        public int GetFinalEndgame(ulong enemyPawns, int side)
        {
            int final;
            if (side == 0)
            {
                final = wEvalEndgame;
                foreach (PassedBonus p in wPassedPawnChecksEndgame)
                    if (p.Test(enemyPawns)) final += p.bonus;
            }
            else
            {
                final = bEvalEndgame;
                foreach (PassedBonus p in bPassedPawnChecksEndgame)
                    if (p.Test(enemyPawns)) final += p.bonus;
            }
            
            return final;
        }
    }

    public enum Slice
    {
        First,
        Second,
        Third,
        Fourth,
    }

    public static RookEvaluation GenerateRookEval(ulong combination, Slice slice)
    {
        RookEvaluation eval = new();

        int startRank = slice switch
        {
            Slice.First => 6,
            Slice.Second => 4,
            Slice.Third => 2,
            Slice.Fourth => 0,
            _ => throw new Exception("no")
        };

        List<OpenFileCheck> fileChecks = new();
        List<(int file, int rank)> coords = new();

        for (int rank = startRank; rank < startRank + 2; rank++)
        {
            if ((BitboardUtils.GetRank(rank) & combination) == 0)
                continue;
            
            for (int file = 0; file < 8; file++)
            {
                // square occupied
                if ((combination & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    // material and weight multiplier
                    eval.wEval += (int)(Pieces.Value[Pieces.WhiteRook] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhiteRook]) + Weights.Pieces[Pieces.WhiteRook, file, rank];
                    eval.bEval += (int)(Pieces.Value[Pieces.BlackRook] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhiteRook]) - Weights.Pieces[Pieces.WhiteRook, file, 7-rank];
                    
                    eval.wEvalEndgame += (int)(Pieces.Value[Pieces.WhiteRook] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhiteRook]) + Weights.EndgamePieces[Pieces.WhiteRook, file, rank];
                    eval.bEvalEndgame += (int)(Pieces.Value[Pieces.BlackRook] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[Pieces.WhiteRook]) - Weights.EndgamePieces[Pieces.WhiteRook, file, 7-rank];
                    
                    // open files
                    fileChecks.Add(new(BitboardUtils.GetFile(file)));
                    coords.Add((file, rank));
                }
            }
        }
        
        eval.fileChecks = fileChecks.ToArray();
        eval.coords = coords.ToArray();
        
        return eval;
    }

    public static T GenerateStandardEval<T>(ulong combination, Slice slice, uint wPiece, uint bPiece) where T : StandardEvaluation, new()
    {
        T eval = new T();
        List<(int File, int rank)> coords = new();
        
        int startRank = slice switch
        {
            Slice.First => 6,
            Slice.Second => 4,
            Slice.Third => 2,
            Slice.Fourth => 0,
            _ => throw new Exception("no")
        };
        
        for (int rank = startRank; rank < startRank + 2; rank++)
        {
            if ((BitboardUtils.GetRank(rank) & combination) == 0)
                continue;
            
            for (int file = 0; file < 8; file++)
            {
                // square occupied
                if ((combination & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    coords.Add((file,rank));
                    
                    eval.wEval += (int)(Pieces.Value[wPiece] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[wPiece]) + Weights.Pieces[wPiece, file, rank];
                    eval.bEval += (int)(Pieces.Value[bPiece] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[wPiece]) - Weights.Pieces[wPiece, file, 7-rank];
                    
                    eval.wEvalEndgame += (int)(Pieces.Value[wPiece] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[wPiece]) + Weights.EndgamePieces[wPiece, file, rank];
                    eval.bEvalEndgame += (int)(Pieces.Value[bPiece] * Weights.MaterialMultiplier * Weights.PiecewiseMaterialWeights[wPiece]) - Weights.EndgamePieces[wPiece, file, 7-rank];
                }
            }
        }

        eval.coords = coords.ToArray();
        return eval;
    }

    public class RookEvaluation : StandardEvaluation
    {
        public OpenFileCheck[] fileChecks = [];

        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in coords)
                mobility += MagicLookup.BishopMobilityLookup(pos, blockers);
            
            return mobility;
        }

        public int GetResult(ulong enemyPawns, ulong friendlyPawns, ulong blockers, int side)
        {
            int final;
            if (side == 0)
            {
                final = wEval;
                foreach (OpenFileCheck f in fileChecks)
                    final += f.Test(enemyPawns, friendlyPawns);
            }
            else
            {
                final = bEval;
                foreach (OpenFileCheck f in fileChecks)
                    final -= f.Test(enemyPawns, friendlyPawns);
            }
            
            return final + MobilityLookup(blockers);
        }

        public override int GetFinalEndgame(ulong blockers, int side)
        {
            return (side == 0 ? wEvalEndgame : bEvalEndgame) + MobilityLookup(blockers);
        }

        public override int GetFinal(ulong blockers, int side)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class StandardEvaluation
    {
        public int wEval;
        public int bEval;
        public int wEvalEndgame;
        public int bEvalEndgame;
        public (int file, int rank)[] coords = [];

        public abstract int MobilityLookup(ulong blockers);
        public abstract int GetFinal(ulong blockers, int side);
        public abstract int GetFinalEndgame(ulong blockers, int side);
    }

    public class QueenEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in coords)
            {
                mobility += MagicLookup.RookMobilityLookup(pos, blockers);
                mobility += MagicLookup.BishopMobilityLookup(pos, blockers);
            }

            return mobility;
        }

        public override int GetFinal(ulong blockers, int side)
        {
            return (side == 0 ? wEval : bEval) + MobilityLookup(blockers);
        }

        public override int GetFinalEndgame(ulong blockers, int side)
        {
            return (side == 0 ? wEvalEndgame : bEvalEndgame) + MobilityLookup(blockers);
        }
    }
    
    public class BishopEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in coords)
                mobility += MagicLookup.BishopMobilityLookup(pos, blockers);
            
            return mobility;
        }

        public override int GetFinal(ulong blockers, int side)
        {
            return (side == 0 ? wEval : bEval) + MobilityLookup(blockers);
        }

        public override int GetFinalEndgame(ulong blockers, int side)
        {
            return (side == 0 ? wEvalEndgame : bEvalEndgame) + MobilityLookup(blockers);
        }
    }
    
    public class KnightEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in coords)
                mobility += Bitboards.MagicLookupArrays.KnightMobilityLookup[pos.file, pos.rank];
            
            return mobility;
        }

        public override int GetFinal(ulong blockers, int side)
        {
            return (side == 0 ? wEval : bEval) + MobilityLookup(blockers);
        }

        public override int GetFinalEndgame(ulong blockers, int side)
        {
            return (side == 0 ? wEvalEndgame : bEvalEndgame) + MobilityLookup(blockers);
        }
    }
    
    public class KingEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            throw new NotImplementedException();
        }

        public override int GetFinal(ulong blockers, int side)
        {
            throw new NotImplementedException();
        }

        public override int GetFinalEndgame(ulong blockers, int side)
        {
            throw new NotImplementedException();
        }
    }

    public readonly struct OpenFileCheck(ulong file)
    {
        public int Test(ulong enemy, ulong friendly)
        {
            if ((file & friendly) == 0) // at least semi open
                return (file & enemy) == 0 ? Weights.OpenFileAdvantage : Weights.SemiOpenFileAdvantage;
            return 0;
        }
    }
    
    public readonly struct PassedBonus(ulong mask, int bonus)
    {
        public readonly int bonus = bonus;

        public bool Test(ulong enemyPawns)
        {
            return (mask & enemyPawns) == 0;
        }
    }
}