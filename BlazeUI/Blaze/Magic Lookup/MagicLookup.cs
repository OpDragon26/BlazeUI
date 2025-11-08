using System.Collections.Generic;

namespace BlazeUI.Blaze.Magic_Lookup;
using Move_Generation;
using Utils;

public static class MagicLookup
{
        public static (Move[] moves, ulong captures) RookLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.RookLookup[pos.file, pos.rank]
        [
            ((blockers & Bitboards.RookMasks[pos.file, pos.rank]) // blocker combination
             * Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.MagicLookupArrays.RookMove[pos.file, pos.rank].push
        ];
    }
    
    public static (Move[] moves, ulong captures) BishopLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.MagicLookupArrays.BishopLookup[pos.file, pos.rank]
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
}