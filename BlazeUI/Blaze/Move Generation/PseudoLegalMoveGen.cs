using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze.Move_Generation;
using Board_Representation;
using static MoveGenerator;
using Magic_Lookup;
using Utils;

public static class PseudoLegalMoveGen
{
        // pseudolegal generation (unordered)
    public static Span<Move> SearchBoard(Board board)
    {
        Move[] moveArray = new Move[219]; // max moves possible from 1 position
        bool enPassant = board.enPassant.file != 8; // if there is an en passant square

        int index = 0;
        // loop through every square
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                // the square is only worth checking if the searched side has a piece there
                if ((board.GetBitboard(board.side) & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    Span<Move> moveSpan = new Span<Move>(moveArray, index, moveArray.Length - index); // creates a span to fill with moves
                    index += SearchPiece(board, board.GetPiece(file, rank), (file, rank), board.side, moveSpan, enPassant);
                }
            }
        }

        return new Span<Move>(moveArray, 0, index);
    }
    
    public static Move[] FilterChecks(Span<Move> moves, Board board)
    {
        List<Move> MoveList = moves.ToArray().ToList();

        for (int i = moves.Length - 1; i >= 0; i--)
        {
            Board moveBoard = new(board);
            moveBoard.MakeMove(MoveList[i]);
            // if the king of the moving side is in check after the move, the move is illegal
            if (Attacked(moveBoard.KingPositions[1-moveBoard.side], moveBoard, moveBoard.side))
                MoveList.RemoveAt(i);
        }
        
        return MoveList.ToArray();
    }
    
    private static int SearchPiece(Board board, ulong piece, (int file, int rank) pos, int side, Span<Move> moveSpan, bool enPassant = false)
    {
        int index = 0;
        Span<Move> captures;
        
        switch (piece & Pieces.TypeMask)
        {
            case Pieces.WhitePawn:
                if (side == 0) // white
                {
                    Span<Move> WPawnMoves = new(MagicLookup.WhitePawnLookupMoves(pos, board.AllPieces()));
                    WPawnMoves.CopyTo(moveSpan);
                    index += WPawnMoves.Length;
                    captures = new(MagicLookup.WhitePawnLookupCaptures(pos, board.BlackPieces()));
                    captures.CopyTo(moveSpan.Slice(index));
                    index += captures.Length;
                    
                    // if there is an en passant capture available, and it can be made from the current square
                    if (enPassant && (Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] & BitboardUtils.GetSquare(board.enPassant)) != 0)
                        moveSpan[index++] = MagicLookup.EnPassantLookup(BitboardUtils.GetSquare(pos) | BitboardUtils.GetSquare(board.enPassant));
                    
                }
                else // black
                {
                    Span<Move> BPawnMoves = new(MagicLookup.BlackPawnLookupMoves(pos, board.AllPieces()));
                    BPawnMoves.CopyTo(moveSpan);
                    index += BPawnMoves.Length;                                                                 
                    captures = new(MagicLookup.BlackPawnLookupCaptures(pos, board.WhitePieces()));
                    captures.CopyTo(moveSpan.Slice(index));
                    index += captures.Length;
                    
                    // if there is an en passant capture available, and it can be made from the current square
                    if (enPassant && (Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank] & BitboardUtils.GetSquare(board.enPassant)) != 0)
                        moveSpan[index++] = MagicLookup.EnPassantLookup(BitboardUtils.GetSquare(pos) | BitboardUtils.GetSquare(board.enPassant));
                }
                break;
            
            case Pieces.WhiteRook:
                // magic lookup moves
                // no captures
                (Move[] moves, ulong captures) rMoves = MagicLookup.RookLookupMoves(pos, board.AllPieces());
                new Span<Move>(rMoves.moves).CopyTo(moveSpan);
                index += rMoves.moves.Length;

                // magic lookup of only captures
                // form a slice out of the span to ensure that none of the already added moves are overwritten
                captures = new(MagicLookup.RookLookupCaptures(pos, rMoves.captures & board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteBishop:
                (Move[] moves, ulong captures) bMoves = MagicLookup.BishopLookupMoves(pos, board.AllPieces());
                new Span<Move>(bMoves.moves).CopyTo(moveSpan);
                index += bMoves.moves.Length;
                
                captures = new(MagicLookup.BishopLookupCaptures(pos, bMoves.captures & board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteQueen:
                // find rook moves
                (Move[] moves, ulong captures) moves = MagicLookup.RookLookupMoves(pos, board.AllPieces());
                new Span<Move>(moves.moves).CopyTo(moveSpan);
                index += moves.moves.Length;
                
                captures = new(MagicLookup.RookLookupCaptures(pos, moves.captures & board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
                
                // find bishop moves
                moves = MagicLookup.BishopLookupMoves(pos, board.AllPieces());
                new Span<Move>(moves.moves).CopyTo(moveSpan.Slice(index));
                index += moves.moves.Length;
                
                captures = new(MagicLookup.BishopLookupCaptures(pos, moves.captures & board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteKnight:
                // find moves, no captures
                Span<Move> knightMoves = new(MagicLookup.KnightLookupMoves(pos, board.AllPieces()));
                knightMoves.CopyTo(moveSpan);
                index += knightMoves.Length;
                
                // find only captures
                captures = new(MagicLookup.KnightLookupCaptures(pos, board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteKing:
                Span<Move> kingMoves = new(MagicLookup.KingLookupMoves(pos, board.AllPieces()));
                kingMoves.CopyTo(moveSpan);
                index += kingMoves.Length;
                
                captures = new(MagicLookup.KingLookupCaptures(pos, board.GetBitboard(1-side)));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
                
                // castling
                if (side == 0) // white
                {
                    int check = 0; // 0: not checked
                    
                    if ((board.castling & 0b1000) != 0 && (board.AllPieces() & Bitboards.WhiteShortCastleMask) == 0) // white can castle short
                    {
                        check = Attacked(board.KingPositions[0], board, 1) ? 1 : 2; // check here whether the king is in check 1 if it is, 2 if it isn't

                        if (check == 2 && !Attacked((5,0), board, 1) && !Attacked((6,0), board, 1))
                            moveSpan[index++] = Bitboards.WhiteShortCastle;
                    }

                    if ((board.castling & 0b0100) != 0 && (board.AllPieces() & Bitboards.WhiteLongCastleMask) == 0) // white can castle long
                    {
                        if (check == 0) // if the king check hasn't been checked before
                            check = Attacked(board.KingPositions[0], board, 1) ? 1 : 2; // check here whether the king is in check 1 if it is, 2 if it isn't
                            
                        if (check == 2 && !Attacked((3,0), board, 1) && !Attacked((2,0), board, 1))
                            moveSpan[index++] = Bitboards.WhiteLongCastle;
                    }
                }
                else // black
                {
                    int check = 0; // 0: not checked
                    
                    if ((board.castling & 0b0010) != 0 && (board.AllPieces() & Bitboards.BlackShortCastleMask) == 0) // black can castle short
                    {
                        check = Attacked(board.KingPositions[1], board, 0) ? 1 : 2; // check here whether the king is in check 1 if it is, 2 if it isn't

                        if (check == 2 && !Attacked((5,7), board, 0) && !Attacked((6,7), board, 0))
                            moveSpan[index++] = Bitboards.BlackShortCastle;
                    }

                    if ((board.castling & 0b0001) != 0 && (board.AllPieces() & Bitboards.BlackLongCastleMask) == 0) // black can castle long
                    {
                        if (check == 0) // if the king check hasn't been checked before
                            check = Attacked(board.KingPositions[1], board, 0) ? 1 : 2; // check here whether the king is in check 1 if it is, 2 if it isn't
                            
                        if (check == 2 && !Attacked((3,7), board, 0) && !Attacked((2,7), board, 0))
                            moveSpan[index++] = Bitboards.BlackLongCastle;
                    }
                }
                break;
        }

        return index;
    }
}