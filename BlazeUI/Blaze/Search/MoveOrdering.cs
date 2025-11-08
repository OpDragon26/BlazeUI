using System;

namespace BlazeUI.Blaze.Search;
using Magic_Lookup;
using Board_Representation;
using Move_Generation;
using Utils;

public static class MoveOrdering
{
    public static void SortMoves(Span<Move> moveSpan, Board board, Move? previous)
    {
        int[] keys = new int[moveSpan.Length];
            
        for (int i = 0; i < moveSpan.Length; i++)
            keys[i] = Reevaluate(board, moveSpan[i], previous);

        new Span<int>(keys).Sort(moveSpan, (x, y) => y.CompareTo(x));
    }
    
    private static int Reevaluate(Board board, Move move, Move? previous)
    {
        int priority = move.Priority;
        
        priority += Pieces.Value[board.GetPiece(move.Destination) & Pieces.TypeMask];

        if (RefutationTable.TryGet(board.hashKey, out RefutationTable.HashEntry result))
            if (move.Equals(result.move))
                priority += result.bonus;
        
        priority += History.Get(move, board.side);
        priority += Counter.Get(previous, move); // if the move is a counter to the previous move made
        
        switch (board.GetPiece(move.Source))
        {
            case Pieces.WhitePawn:
                if ((Masks.WhitePawnCaptureMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackPawn:
                if ((Masks.BlackPawnCaptureMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
                    priority += 50;
                break;
            case Pieces.WhiteRook:
                if ((MagicLookup.RookLookupCaptureBitboards(move.Destination, board.BlackPieces()) & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackRook:
                if ((MagicLookup.RookLookupCaptureBitboards(move.Destination, board.WhitePieces()) & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
                    priority += 50;
                break;
            case Pieces.WhiteKnight:
                if ((Masks.KnightMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackKnight:
                if ((Masks.KnightMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
                    priority += 50;
                break;
            case Pieces.WhiteBishop:
                if ((MagicLookup.BishopLookupCaptureBitboards(move.Destination, board.BlackPieces()) & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackBishop:
                if ((MagicLookup.BishopLookupCaptureBitboards(move.Destination, board.WhitePieces()) & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
                    priority += 50;
                break;
            case Pieces.WhiteQueen:
                ulong wCaptures = MagicLookup.BishopLookupCaptureBitboards(move.Destination, board.BlackPieces()) | MagicLookup.RookLookupCaptureBitboards(move.Destination, board.BlackPieces());
                if ((wCaptures & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackQueen:
                ulong bCaptures = MagicLookup.BishopLookupCaptureBitboards(move.Destination, board.WhitePieces()) | MagicLookup.RookLookupCaptureBitboards(move.Destination, board.WhitePieces());
                if ((bCaptures & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
                    priority += 50;
                break;
        }
        
        return priority;
    }
}