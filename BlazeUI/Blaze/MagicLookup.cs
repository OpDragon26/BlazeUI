using System.Collections.Generic;

namespace BlazeUI.Blaze;

public static class MagicLookup
{
        public static ref (Move[] moves, ulong captures) RookLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.RookLookup[pos.file, pos.rank]
        [
            ((blockers & Bitboards.RookMasks[pos.file, pos.rank]) // blocker combination
             * Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].push
        ];
    }
    
    public static ref (Move[] moves, ulong captures) BishopLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.BishopLookup[pos.file, pos.rank]
        [
            ((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) // blocker combination
            * Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].push
        ];
    }
    
    public static ref Move[] RookLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref Bitboards.MagicLookupArrays.RookCaptureLookup[pos.file, pos.rank]
            [(captures * Bitboards.MagicLookupArrays.RookCapture[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookCapture[pos.file, pos.rank].push];
    }
    
    public static ulong RookLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.RookLookupCapturesArray[pos.file, pos.rank]
            [((blockers & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.BishopLookupCapturesArray[pos.file, pos.rank]
            [((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BishopLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref Bitboards.MagicLookupArrays.BishopCaptureLookup[pos.file, pos.rank]
            [(captures * Bitboards.MagicLookupArrays.BishopCapture[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.KnightLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KnightMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.KnightMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.MagicLookupArrays.KnightCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.KnightMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.KnightMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.KingLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.MagicLookupArrays.KingCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.WhitePawnLookup[pos.file, pos.rank]
            [((blockers & Bitboards.WhitePawnMoveMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.WhitePawnMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.WhitePawnMove[pos.file, pos.rank].push];
    }

    public static ref Move[] BlackPawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.MagicLookupArrays.BlackPawnLookup[pos.file, pos.rank]
            [((blockers & Bitboards.BlackPawnMoveMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BlackPawnMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BlackPawnMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.MagicLookupArrays.WhitePawnCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.WhitePawnCapture[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.WhitePawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BlackPawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.MagicLookupArrays.BlackPawnCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BlackPawnCapture[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BlackPawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move EnPassantLookup(ulong enPassant)
    {
        return ref Bitboards.MagicLookupArrays.EnPassantLookupArray[(enPassant * Bitboards.MagicLookupArrays.EnPassantNumbers.magicNumber) >> Bitboards.MagicLookupArrays.EnPassantNumbers.push];
    }

    public static int KingSafetyBonusLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.KingSafetyLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.KingMove[pos.file, pos.rank].push];
    }

    public static ulong RookMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.RookBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.SmallRookMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int RookMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.RookMobilityLookupArray[pos.file, pos.rank]
            [((blockers & Bitboards.SmallRookMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static ulong BishopMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.BishopBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.SmallBishopMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int BishopMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.BishopMobilityLookupArray[pos.file, pos.rank]
            [((blockers & Bitboards.SmallBishopMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopBitboardNumbers[pos.file, pos.rank].push];
    }

    public static ulong RookPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.RookPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.BishopPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> RookPinSearch((int file, int rank) pos, ulong selected)
    {
        return Bitboards.MagicLookupArrays.RookPinLookup[pos.file, pos.rank]
            [((selected & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> BishopPinSearch((int file, int rank) pos, ulong selected)
    {
        return Bitboards.MagicLookupArrays.BishopPinLookup[pos.file, pos.rank]
            [((selected & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BishopMove[pos.file, pos.rank].push];
    }

    public static Move BlockCaptureLookup((int file, int rank) pos, ulong square)
    {
        return Bitboards.MagicLookupArrays.BlockCaptureMoveLookup[pos.file, pos.rank]
            [(square * Bitboards.MagicLookupArrays.BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockLookup((int file, int rank) pos, ulong squares)
    {
        return Bitboards.MagicLookupArrays.BlockMoveLookup[pos.file, pos.rank]
            [(squares * Bitboards.MagicLookupArrays.BlockMoveNumber.magicNumber) >> Bitboards.MagicLookupArrays.BlockMoveNumber.push];
    }
    
    public static Move[] BlockCapturePawnLookup((int file, int rank) pos, ulong square)
    {
        return Bitboards.MagicLookupArrays.BlockCaptureMovePawnLookup[pos.file, pos.rank]
            [(square * Bitboards.MagicLookupArrays.BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockPawnLookup((int file, int rank) pos, ulong squares)
    {
        return Bitboards.MagicLookupArrays.BlockMovePawnLookup[pos.file, pos.rank]
            [(squares * Bitboards.MagicLookupArrays.BlockMoveNumber.magicNumber) >> Bitboards.MagicLookupArrays.BlockMoveNumber.push];
    }

    public static ulong AttackLineLookup((int file, int rank) pos, ulong attackers)
    {
        return Bitboards.MagicLookupArrays.AttackLineLookup[pos.file, pos.rank]
            [(attackers * Bitboards.MagicLookupArrays.AttackLineNumber.magicNumber) >> Bitboards.MagicLookupArrays.AttackLineNumber.push];
    }
    

    public static Evaluation.PawnEvaluation PawnEvaluationLookupRight(ulong pawns)
    {
        return Bitboards.MagicLookupArrays.RightPawnEvalLookup[((pawns & Bitboards.RightPawns) * Bitboards.MagicLookupArrays.RightPawnEvalNumber.magicNumber) >> Bitboards.MagicLookupArrays.RightPawnEvalNumber.push];
    }
    
    public static Evaluation.PawnEvaluation PawnEvaluationLookupLeft(ulong pawns)
    {
        return Bitboards.MagicLookupArrays.LeftPawnEvalLookup[((pawns & Bitboards.LeftPawns) * Bitboards.MagicLookupArrays.LeftPawnEvalNumber.magicNumber) >> Bitboards.MagicLookupArrays.LeftPawnEvalNumber.push];
    }
    
    public static Evaluation.PawnEvaluation PawnEvaluationLookupCenter(ulong pawns)
    {
        return Bitboards.MagicLookupArrays.CenterPawnEvalLookup[((pawns & Bitboards.CenterPawns) * Bitboards.MagicLookupArrays.CenterPawnEvalNumber.magicNumber) >> Bitboards.MagicLookupArrays.CenterPawnEvalNumber.push];
    }

    public static Evaluation.RookEvaluation FirstRookEvalLookup(ulong rooks)
    {
        return Bitboards.MagicLookupArrays.FirstRookEvaluationLookup[rooks & Bitboards.FirstSlice];
    }

    public static Evaluation.RookEvaluation SecondRookEvalLookup(ulong rooks)
    {
        return Bitboards.MagicLookupArrays.SecondRookEvaluationLookup[(rooks & Bitboards.SecondSlice) >> 16];
    }

    public static Evaluation.RookEvaluation ThirdRookEvalLookup(ulong rooks)
    {
        return Bitboards.MagicLookupArrays.ThirdRookEvaluationLookup[(rooks & Bitboards.ThirdSlice) >> 32];
    }

    public static Evaluation.RookEvaluation FourthRookEvalLookup(ulong rooks)
    {
        return Bitboards.MagicLookupArrays.FourthRookEvaluationLookup[(rooks & Bitboards.FourthSlice) >> 48];
    }
    
    
    public static Evaluation.QueenEvaluation FirstQueenEvalLookup(ulong queen)
    {
        return Bitboards.MagicLookupArrays.FirstQueenEvaluationLookup[queen & Bitboards.FirstSlice];
    }

    public static Evaluation.QueenEvaluation SecondQueenEvalLookup(ulong queen)
    {
        return Bitboards.MagicLookupArrays.SecondQueenEvaluationLookup[(queen & Bitboards.SecondSlice) >> 16];
    }

    public static Evaluation.QueenEvaluation ThirdQueenEvalLookup(ulong queen)
    {
        return Bitboards.MagicLookupArrays.ThirdQueenEvaluationLookup[(queen & Bitboards.ThirdSlice) >> 32];
    }

    public static Evaluation.QueenEvaluation FourthQueenEvalLookup(ulong queen)
    {
        return Bitboards.MagicLookupArrays.FourthQueenEvaluationLookup[(queen & Bitboards.FourthSlice) >> 48];
    }
    
    public static Evaluation.KnightEvaluation FirstKnightEvalLookup(ulong knights)
    {
        return Bitboards.MagicLookupArrays.FirstKnightEvaluationLookup[knights & Bitboards.FirstSlice];
    }

    public static Evaluation.KnightEvaluation SecondKnightEvalLookup(ulong knights)
    {
        return Bitboards.MagicLookupArrays.SecondKnightEvaluationLookup[(knights & Bitboards.SecondSlice) >> 16];
    }

    public static Evaluation.KnightEvaluation ThirdKnightEvalLookup(ulong knights)
    {
        return Bitboards.MagicLookupArrays.ThirdKnightEvaluationLookup[(knights & Bitboards.ThirdSlice) >> 32];
    }

    public static Evaluation.KnightEvaluation FourthKnightEvalLookup(ulong knights)
    {
        return Bitboards.MagicLookupArrays.FourthKnightEvaluationLookup[(knights & Bitboards.FourthSlice) >> 48];
    }
    
    public static Evaluation.BishopEvaluation FirstBishopEvalLookup(ulong bishops)
    {
        return Bitboards.MagicLookupArrays.FirstBishopEvaluationLookup[bishops & Bitboards.FirstSlice];
    }

    public static Evaluation.BishopEvaluation SecondBishopEvalLookup(ulong bishops)
    {
        return Bitboards.MagicLookupArrays.SecondBishopEvaluationLookup[(bishops & Bitboards.SecondSlice) >> 16];
    }

    public static Evaluation.BishopEvaluation ThirdBishopEvalLookup(ulong bishops)
    {
        return Bitboards.MagicLookupArrays.ThirdBishopEvaluationLookup[(bishops & Bitboards.ThirdSlice) >> 32];
    }

    public static Evaluation.BishopEvaluation FourthBishopEvalLookup(ulong bishops)
    {
        return Bitboards.MagicLookupArrays.FourthBishopEvaluationLookup[(bishops & Bitboards.FourthSlice) >> 48];
    }

    public static Evaluation.KingEvaluation KingEvalLookup((int file, int rank) pos)
    {
        return Bitboards.MagicLookupArrays.KingEvaluationLookup[pos.file, pos.rank];
    }
}