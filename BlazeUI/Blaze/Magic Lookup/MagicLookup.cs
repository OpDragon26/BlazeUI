using System.Collections.Generic;

namespace BlazeUI.Blaze.Magic_Lookup;
using Move_Generation;
using Utils;

public static class MagicLookup
{
        public static (Move[] moves, ulong captures) RookLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.RookLookup[pos.file, pos.rank]
        [
            ((blockers & Bitboards.RookMasks[pos.file, pos.rank]) // blocker combination
             * Bitboards.Lookup.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookMove[pos.file, pos.rank].push
        ];
    }
    
    public static (Move[] moves, ulong captures) BishopLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.BishopLookup[pos.file, pos.rank]
        [
            ((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) // blocker combination
            * Bitboards.Lookup.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopMove[pos.file, pos.rank].push
        ];
    }
    
    public static ref Move[] RookLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref Bitboards.Lookup.RookCaptureLookup[pos.file, pos.rank]
            [(captures * Bitboards.Lookup.RookCapture[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookCapture[pos.file, pos.rank].push];
    }
    
    public static ulong RookLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.RookLookupCapturesArray[pos.file, pos.rank]
            [((blockers & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.Lookup.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.BishopLookupCapturesArray[pos.file, pos.rank]
            [((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.Lookup.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BishopLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref Bitboards.Lookup.BishopCaptureLookup[pos.file, pos.rank]
            [(captures * Bitboards.Lookup.BishopCapture[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.Lookup.KnightLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KnightMasks[pos.file, pos.rank]) * Bitboards.Lookup.KnightMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.Lookup.KnightCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.KnightMasks[pos.file, pos.rank]) * Bitboards.Lookup.KnightMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.Lookup.KingLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.Lookup.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.Lookup.KingCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.Lookup.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.Lookup.WhitePawnLookup[pos.file, pos.rank]
            [((blockers & Bitboards.WhitePawnMoveMasks[pos.file, pos.rank]) * Bitboards.Lookup.WhitePawnMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.WhitePawnMove[pos.file, pos.rank].push];
    }

    public static ref Move[] BlackPawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref Bitboards.Lookup.BlackPawnLookup[pos.file, pos.rank]
            [((blockers & Bitboards.BlackPawnMoveMasks[pos.file, pos.rank]) * Bitboards.Lookup.BlackPawnMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BlackPawnMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.Lookup.WhitePawnCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank]) * Bitboards.Lookup.WhitePawnCapture[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.WhitePawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BlackPawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref Bitboards.Lookup.BlackPawnCaptureLookup[pos.file, pos.rank]
            [((enemy & Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank]) * Bitboards.Lookup.BlackPawnCapture[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BlackPawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move EnPassantLookup(ulong enPassant)
    {
        return ref Bitboards.Lookup.EnPassantLookupArray[(enPassant * Bitboards.Lookup.EnPassantNumbers.magicNumber) >> Bitboards.Lookup.EnPassantNumbers.push];
    }

    public static int KingSafetyBonusLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.KingSafetyLookup[pos.file, pos.rank]
            [((~blockers & Bitboards.KingMasks[pos.file, pos.rank]) * Bitboards.Lookup.KingMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.KingMove[pos.file, pos.rank].push];
    }

    public static ulong RookMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.RookBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.SmallRookMasks[pos.file, pos.rank]) * Bitboards.Lookup.RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int RookMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.RookMobilityLookupArray[pos.file, pos.rank]
            [((blockers & Bitboards.SmallRookMasks[pos.file, pos.rank]) * Bitboards.Lookup.RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static ulong BishopMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.BishopBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.SmallBishopMasks[pos.file, pos.rank]) * Bitboards.Lookup.BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int BishopMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.BishopMobilityLookupArray[pos.file, pos.rank]
            [((blockers & Bitboards.SmallBishopMasks[pos.file, pos.rank]) * Bitboards.Lookup.BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopBitboardNumbers[pos.file, pos.rank].push];
    }

    public static ulong RookPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.RookPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.Lookup.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return Bitboards.Lookup.BishopPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.Lookup.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> RookPinSearch((int file, int rank) pos, ulong selected)
    {
        return Bitboards.Lookup.RookPinLookup[pos.file, pos.rank]
            [((selected & Bitboards.RookMasks[pos.file, pos.rank]) * Bitboards.Lookup.RookMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.RookMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> BishopPinSearch((int file, int rank) pos, ulong selected)
    {
        return Bitboards.Lookup.BishopPinLookup[pos.file, pos.rank]
            [((selected & Bitboards.BishopMasks[pos.file, pos.rank]) * Bitboards.Lookup.BishopMove[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BishopMove[pos.file, pos.rank].push];
    }

    public static Move BlockCaptureLookup((int file, int rank) pos, ulong square)
    {
        return Bitboards.Lookup.BlockCaptureMoveLookup[pos.file, pos.rank]
            [(square * Bitboards.Lookup.BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockLookup((int file, int rank) pos, ulong squares)
    {
        return Bitboards.Lookup.BlockMoveLookup[pos.file, pos.rank]
            [(squares * Bitboards.Lookup.BlockMoveNumber.magicNumber) >> Bitboards.Lookup.BlockMoveNumber.push];
    }
    
    public static Move[] BlockCapturePawnLookup((int file, int rank) pos, ulong square)
    {
        return Bitboards.Lookup.BlockCaptureMovePawnLookup[pos.file, pos.rank]
            [(square * Bitboards.Lookup.BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> Bitboards.Lookup.BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockPawnLookup((int file, int rank) pos, ulong squares)
    {
        return Bitboards.Lookup.BlockMovePawnLookup[pos.file, pos.rank]
            [(squares * Bitboards.Lookup.BlockMoveNumber.magicNumber) >> Bitboards.Lookup.BlockMoveNumber.push];
    }

    public static ulong AttackLineLookup((int file, int rank) pos, ulong attackers)
    {
        return Bitboards.Lookup.AttackLineLookup[pos.file, pos.rank]
            [(attackers * Bitboards.Lookup.AttackLineNumber.magicNumber) >> Bitboards.Lookup.AttackLineNumber.push];
    }
}