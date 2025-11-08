using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Evaluation;
using static EvaluationData;
using Utils;
using Board_Representation;
using Magic_Lookup;

public static class EvaluationPrecomputer
{
    public static T GenerateStandardEval<T>(ulong combination, Slice slice, uint piece) where T : StandardEvaluation, new()
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
                    eval.PhaseInc += PestoEval.PhaseIncrement[piece];
                    
                    coords.Add((file,rank));
                    
                    eval.MgWhite += (int)(PestoEval.MgValue[piece] * Weights.MaterialMultiplier) + PestoEval.GetWhiteMgEval(piece, file, rank);
                    eval.MgBlack += (int)(-PestoEval.MgValue[piece] * Weights.MaterialMultiplier) - PestoEval.GetBlackMgEval(piece, file, rank);
                    
                    eval.EgWhite += (int)(PestoEval.EgValue[piece] * Weights.MaterialMultiplier) + PestoEval.GetWhiteEgEval(piece, file, rank);
                    eval.EgBlack += (int)(-PestoEval.EgValue[piece] * Weights.MaterialMultiplier) - PestoEval.GetBlackEgEval(piece, file, rank);
                }
            }
        }

        eval.Coords = coords.ToArray();
        return eval;
    }
            
    public static PawnEvaluation GeneratePawnEval(ulong pawnCombination, Section boardSide)
    {
        PawnEvaluation eval = new();
        List<PassedBonus> mgPassedWhite = new();
        List<PassedBonus> mgPassedBlack = new();
        List<PassedBonus> egPassedWhite = new();
        List<PassedBonus> egPassedBlack = new();
        ulong controlledWhite = 0;
        ulong controlledBlack = 0;

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
                    eval.MgScoreWhite += (int)(PestoEval.MgValue[Pieces.WhitePawn] * Weights.MaterialMultiplier) + PestoEval.GetWhiteMgEval(Pieces.WhitePawn, file, rank);
                    eval.MgScoreBlack += (int)(-PestoEval.MgValue[Pieces.WhitePawn] * Weights.MaterialMultiplier) - PestoEval.GetBlackMgEval(Pieces.WhitePawn, file, rank);
                    
                    eval.EgScoreWhite += (int)(PestoEval.EgValue[Pieces.WhitePawn] * Weights.MaterialMultiplier) + PestoEval.GetWhiteEgEval(Pieces.WhitePawn, file, rank);
                    eval.EgScoreBlack += (int)(-PestoEval.EgValue[Pieces.WhitePawn] * Weights.MaterialMultiplier) - PestoEval.GetBlackEgEval(Pieces.WhitePawn, file, rank);
                    
                    controlledWhite |= Bitboards.WhitePawnCaptureMasks[file, rank];
                    controlledBlack |= Bitboards.BlackPawnCaptureMasks[file, rank];
                    
                    // passed masks
                    mgPassedWhite.Add(new PassedBonus(BitboardUtils.GetWhitePassedPawnMask(file, rank), Weights.MgPassedBonus[rank]));
                    mgPassedBlack.Add(new PassedBonus(BitboardUtils.GetBlackPassedPawnMask(file, rank), -Weights.MgPassedBonus[7 - rank]));
                    
                    egPassedWhite.Add(new PassedBonus(BitboardUtils.GetWhitePassedPawnMask(file, rank), Weights.EgPassedBonus[rank]));
                    egPassedBlack.Add(new PassedBonus(BitboardUtils.GetBlackPassedPawnMask(file, rank), -Weights.EgPassedBonus[7 - rank]));
                    
                    if ((Bitboards.NeighbourMasks[file] & pawnCombination) == 0)
                    {
                        eval.MgScoreWhite -= Weights.IsolatedPawnPenalty;
                        eval.MgScoreBlack += Weights.IsolatedPawnPenalty;
                        
                        eval.EgScoreWhite -= Weights.IsolatedPawnPenalty;
                        eval.EgScoreBlack += Weights.IsolatedPawnPenalty;
                    }
                }
            }
        }
        
        // center control
        eval.MgScoreWhite += (int)(ulong.PopCount(controlledWhite & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
        eval.MgScoreBlack -= (int)(ulong.PopCount(controlledBlack & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
        
        eval.MgPassedWhite = mgPassedWhite.ToArray();
        eval.MgPassedBlack = mgPassedBlack.ToArray();
        eval.EgPassedWhite = egPassedWhite.ToArray();
        eval.EgPassedBlack = egPassedBlack.ToArray();
        
        return eval;
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
                    eval.PhaseInc += PestoEval.PhaseIncrement[Pieces.WhiteRook];
                        
                    // material and weight multiplier
                    eval.MgWhite += (int)(PestoEval.MgValue[Pieces.WhiteRook] * Weights.MaterialMultiplier) + PestoEval.GetWhiteMgEval(Pieces.WhiteRook, file, rank);
                    eval.MgBlack += (int)(-PestoEval.MgValue[Pieces.WhiteRook] * Weights.MaterialMultiplier) - PestoEval.GetBlackMgEval(Pieces.WhiteRook, file, rank);
                        
                    eval.EgWhite += (int)(PestoEval.EgValue[Pieces.WhiteRook] * Weights.MaterialMultiplier) + PestoEval.GetWhiteEgEval(Pieces.WhiteRook, file, rank);
                    eval.EgBlack += (int)(-PestoEval.EgValue[Pieces.WhiteRook] * Weights.MaterialMultiplier) - PestoEval.GetBlackEgEval(Pieces.WhiteRook, file, rank);
                        
                        // open files
                    fileChecks.Add(new(BitboardUtils.GetFile(file)));
                    coords.Add((file, rank));
                }
            }
        }
            
        eval.fileChecks = fileChecks.ToArray();
        eval.Coords = coords.ToArray();
            
        return eval;
    }

    public static KingEvaluation GenerateKingEval(int file, int rank)
    {
        return new KingEvaluation
        (
            (int)(PestoEval.EgValue[Pieces.WhiteKing] * Weights.MaterialMultiplier) + PestoEval.GetWhiteMgEval(Pieces.WhiteKing, file, rank),
             (int)(-PestoEval.EgValue[Pieces.WhiteKing] * Weights.MaterialMultiplier) - PestoEval.GetBlackMgEval(Pieces.WhiteKing, file, rank),
             (int)(PestoEval.EgValue[Pieces.WhiteKing] * Weights.MaterialMultiplier) + PestoEval.GetWhiteEgEval(Pieces.WhiteKing, file, rank),
             (int)(-PestoEval.EgValue[Pieces.WhiteKing] * Weights.MaterialMultiplier) - PestoEval.GetBlackEgEval(Pieces.WhiteKing, file, rank)
        );
    }
}