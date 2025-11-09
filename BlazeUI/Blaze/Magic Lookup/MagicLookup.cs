using System.Collections.Generic;

namespace BlazeUI.Blaze.Magic_Lookup;
using Move_Generation;
using Utils;
using static Masks;
using static Bitboards.Lookup;
using static Evaluation.EvaluationLookup;

public static class MagicLookup
{
    /*
    The magic lookup returns a span of moves to be copied into the move array and its lenght, and a bitboard with squares that are captures, but might land on a friendly piece
    The returned moves all land on empty squares, while the bitboard shows moves that land on occupied squares.
    Select the enemy pieces from those captured using the AND operation and a second magic lookup is initiated using that bitboard, which returns another span of moves
    */
    
    public static (Move[] moves, ulong captures) RookLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return RookLookup[pos.file, pos.rank]
        [
            ((blockers & RookMasks[pos.file, pos.rank]) // blocker combination
             * RookMove[pos.file, pos.rank].magicNumber) >> RookMove[pos.file, pos.rank].push
        ];
    }
    
    public static (Move[] moves, ulong captures) BishopLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return BishopLookup[pos.file, pos.rank]
        [
            ((blockers & BishopMasks[pos.file, pos.rank]) // blocker combination
            * BishopMove[pos.file, pos.rank].magicNumber) >> BishopMove[pos.file, pos.rank].push
        ];
    }
    
    public static ref Move[] RookLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref RookCaptureLookup[pos.file, pos.rank]
            [(captures * RookCapture[pos.file, pos.rank].magicNumber) >> RookCapture[pos.file, pos.rank].push];
    }
    
    public static ulong RookLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return RookLookupCapturesArray[pos.file, pos.rank]
            [((blockers & RookMasks[pos.file, pos.rank]) * RookMove[pos.file, pos.rank].magicNumber) >> RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopLookupCaptureBitboards((int file, int rank) pos, ulong blockers)
    {
        return BishopLookupCapturesArray[pos.file, pos.rank]
            [((blockers & BishopMasks[pos.file, pos.rank]) * BishopMove[pos.file, pos.rank].magicNumber) >> BishopMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BishopLookupCaptures((int file, int rank) pos, ulong captures)
    {
        return ref BishopCaptureLookup[pos.file, pos.rank]
            [(captures * BishopCapture[pos.file, pos.rank].magicNumber) >> BishopCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref KnightLookup[pos.file, pos.rank]
            [((~blockers & KnightMasks[pos.file, pos.rank]) * KnightMove[pos.file, pos.rank].magicNumber) >> KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KnightLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref KnightCaptureLookup[pos.file, pos.rank]
            [((enemy & KnightMasks[pos.file, pos.rank]) * KnightMove[pos.file, pos.rank].magicNumber) >> KnightMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref KingLookup[pos.file, pos.rank]
            [((~blockers & KingMasks[pos.file, pos.rank]) * KingMove[pos.file, pos.rank].magicNumber) >> KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] KingLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref KingCaptureLookup[pos.file, pos.rank]
            [((enemy & KingMasks[pos.file, pos.rank]) * KingMove[pos.file, pos.rank].magicNumber) >> KingMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref WhitePawnLookup[pos.file, pos.rank]
            [((blockers & WhitePawnMoveMasks[pos.file, pos.rank]) * WhitePawnMove[pos.file, pos.rank].magicNumber) >> WhitePawnMove[pos.file, pos.rank].push];
    }

    public static ref Move[] BlackPawnLookupMoves((int file, int rank) pos, ulong blockers)
    {
        return ref BlackPawnLookup[pos.file, pos.rank]
            [((blockers & BlackPawnMoveMasks[pos.file, pos.rank]) * BlackPawnMove[pos.file, pos.rank].magicNumber) >> BlackPawnMove[pos.file, pos.rank].push];
    }
    
    public static ref Move[] WhitePawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref WhitePawnCaptureLookup[pos.file, pos.rank]
            [((enemy & WhitePawnCaptureMasks[pos.file, pos.rank]) * WhitePawnCapture[pos.file, pos.rank].magicNumber) >> WhitePawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move[] BlackPawnLookupCaptures((int file, int rank) pos, ulong enemy)
    {
        return ref BlackPawnCaptureLookup[pos.file, pos.rank]
            [((enemy & BlackPawnCaptureMasks[pos.file, pos.rank]) * BlackPawnCapture[pos.file, pos.rank].magicNumber) >> BlackPawnCapture[pos.file, pos.rank].push];
    }
    
    public static ref Move EnPassantLookup(ulong enPassant)
    {
        return ref EnPassantLookupArray[(enPassant * EnPassantNumbers.magicNumber) >> EnPassantNumbers.push];
    }

    public static int KingSafetyBonusLookup((int file, int rank) pos, ulong blockers)
    {
        return KingSafetyLookup[pos.file, pos.rank]
            [((~blockers & KingMasks[pos.file, pos.rank]) * KingMove[pos.file, pos.rank].magicNumber) >> KingMove[pos.file, pos.rank].push];
    }

    public static ulong RookMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return RookBitboardLookup[pos.file, pos.rank]
            [((blockers & SmallRookMasks[pos.file, pos.rank]) * RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int RookMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return RookMobilityLookupArray[pos.file, pos.rank]
            [((blockers & SmallRookMasks[pos.file, pos.rank]) * RookBitboardNumbers[pos.file, pos.rank].magicNumber) >> RookBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static ulong BishopMoveBitboardLookup((int file, int rank) pos, ulong blockers)
    {
        return BishopBitboardLookup[pos.file, pos.rank]
            [((blockers & SmallBishopMasks[pos.file, pos.rank]) * BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> BishopBitboardNumbers[pos.file, pos.rank].push];
    }
    
    public static int BishopMobilityLookup((int file, int rank) pos, ulong blockers)
    {
        return BishopMobilityLookupArray[pos.file, pos.rank]
            [((blockers & SmallBishopMasks[pos.file, pos.rank]) * BishopBitboardNumbers[pos.file, pos.rank].magicNumber) >> BishopBitboardNumbers[pos.file, pos.rank].push];
    }

    public static ulong RookPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return RookPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & RookMasks[pos.file, pos.rank]) * RookMove[pos.file, pos.rank].magicNumber) >> RookMove[pos.file, pos.rank].push];
    }
    
    public static ulong BishopPinLineLookup((int file, int rank) pos, ulong blockers)
    {
        return BishopPinLineBitboardLookup[pos.file, pos.rank]
            [((blockers & BishopMasks[pos.file, pos.rank]) * BishopMove[pos.file, pos.rank].magicNumber) >> BishopMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> RookPinSearch((int file, int rank) pos, ulong selected)
    {
        return RookPinLookup[pos.file, pos.rank]
            [((selected & RookMasks[pos.file, pos.rank]) * RookMove[pos.file, pos.rank].magicNumber) >> RookMove[pos.file, pos.rank].push];
    }
    
    public static List<BitboardUtils.PinSearchResult> BishopPinSearch((int file, int rank) pos, ulong selected)
    {
        return BishopPinLookup[pos.file, pos.rank]
            [((selected & BishopMasks[pos.file, pos.rank]) * BishopMove[pos.file, pos.rank].magicNumber) >> BishopMove[pos.file, pos.rank].push];
    }

    public static Move BlockCaptureLookup((int file, int rank) pos, ulong square)
    {
        return BlockCaptureMoveLookup[pos.file, pos.rank]
            [(square * BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockLookup((int file, int rank) pos, ulong squares)
    {
        return BlockMoveLookup[pos.file, pos.rank]
            [(squares * BlockMoveNumber.magicNumber) >> BlockMoveNumber.push];
    }
    
    public static Move[] BlockCapturePawnLookup((int file, int rank) pos, ulong square)
    {
        return BlockCaptureMovePawnLookup[pos.file, pos.rank]
            [(square * BlockCaptureNumbers[pos.file, pos.rank].magicNumber) >> BlockCaptureNumbers[pos.file, pos.rank].push];
    }
    
    public static Move[] BlockPawnLookup((int file, int rank) pos, ulong squares)
    {
        return BlockMovePawnLookup[pos.file, pos.rank]
            [(squares * BlockMoveNumber.magicNumber) >> BlockMoveNumber.push];
    }

    public static ulong AttackLineLookup((int file, int rank) pos, ulong attackers)
    {
        return Bitboards.Lookup.AttackLineLookup[pos.file, pos.rank]
            [(attackers * AttackLineNumber.magicNumber) >> AttackLineNumber.push];
    }
    
    // evaluation lookup
    public static SliceGroup FirstSliceEvalLookup(ulong bitboard)
    {
        return FirstSliceLookup[bitboard & Slice];
    }

    public static SliceGroup SecondSliceEvalLookup(ulong bitboard)
    {
        return SecondSliceLookup[(bitboard >> 16) & Slice];
    }

    public static SliceGroup ThirdSliceEvalLookup(ulong bitboard)
    {
        return ThirdSliceLookup[(bitboard >> 32) & Slice];
    }

    public static SliceGroup FourthSliceEvalLookup(ulong bitboard)
    {
        return FourthSliceLookup[(bitboard >> 48) & Slice];
    }
}