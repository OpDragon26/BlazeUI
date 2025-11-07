using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Move_Generation;
using Board_Representation;
using Search;
using Magic_Lookup;
using Utils;

public static class MoveGenerator
{
    // returns pseudo legal moves: abides by the rules of piece movement, but does not account for checks
    public static Span<Move> SearchBoard(Board board, bool ordering = true, Move? previous = null)
    {
        Move[] moveArray = new Move[219]; // max moves possible from 1 position
        bool enPassant = board.enPassant.file != 8; // if there is an en passant square
        (ulong pinned, Dictionary<ulong, ulong> pinStates) pinState = GetPinStates(board, board.side);
        (bool attacked, bool doubleAttack, ulong attackLines) kingInCheck = GetAttackLines(board.KingPositions[board.side], board, 1 - board.side);
        ulong enemyAttacked = GetAttackedBitboard(board, 1 - board.side, board.KingPositions[board.side]);

        int index = 0;
        // loop through every square
        if (!kingInCheck.attacked) // not a check
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 7; file >= 0; file--)
                {
                    // the square is only worth checking if the searched side has a piece there
                    if ((board.GetBitboard(board.side) & BitboardUtils.GetSquare(file, rank)) != 0)
                    {
                        // if the piece is pinned, get the pin path
                        ulong blockMoves = (pinState.pinned & BitboardUtils.GetSquare(file, rank)) != 0 ? ~pinState.pinStates[BitboardUtils.GetSquare(file, rank)] : 
                            (file, rank) == board.KingPositions[board.side] ? enemyAttacked : 0; // if the searched piece is the king, don't allow it to move into check
                    
                        Span<Move> moveSpan = new Span<Move>(moveArray, index, moveArray.Length - index); // creates a span to fill with moves
                        index += SearchPiece(board, board.GetPiece(file, rank), (file, rank), board.side, moveSpan, enPassant, blockMoves: blockMoves, enemyAttacked: enemyAttacked);
                    }
                }
            }
        }
        else if (!kingInCheck.doubleAttack) // not a double check
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 7; file >= 0; file--)
                {
                    // the square is only worth checking if the searched side has a piece there
                    if ((board.GetBitboard(board.side) & BitboardUtils.GetSquare(file, rank)) != 0)
                    {
                        Span<Move> moveSpan = new Span<Move>(moveArray, index, moveArray.Length - index); // creates a span to fill with moves
                        index += SearchPieceCheck(board, board.GetPiece(file, rank), (file, rank), board.side, moveSpan, kingInCheck.attackLines, enPassant, pinState.pinned, enemyAttacked);
                    }
                }
            }
        }
        else // double check -> only the king can move
        {
            Span<Move> moveSpan = new(moveArray); // creates a span to fill with moves
            index += SearchPieceCheck(board, Pieces.WhiteKing, board.KingPositions[board.side], board.side, moveSpan, kingInCheck.attackLines, enPassant, pinState.pinned, enemyAttacked, true);
        }
        
        Span<Move> moves = new Span<Move>(moveArray, 0, index);
        
        if (ordering)
            MoveOrdering.SortMoves(moves, board, previous);
            
        return moves;
    }
    
    private static int SearchPiece(Board board, ulong piece, (int file, int rank) pos, int side, Span<Move> moveSpan, bool enPassant = false, ulong blockMoves = 0, ulong enemyAttacked = 0)
    {
        int index = 0;
        Span<Move> captures;
        
        switch (piece & Pieces.TypeMask)
        {
            case Pieces.WhitePawn:
                if (side == 0) // white
                {
                    Span<Move> WPawnMoves = new(MagicLookup.WhitePawnLookupMoves(pos, board.AllPieces() | blockMoves));
                    WPawnMoves.CopyTo(moveSpan);
                    index += WPawnMoves.Length;
                    captures = new(MagicLookup.WhitePawnLookupCaptures(pos, board.BlackPieces() & ~blockMoves));
                    captures.CopyTo(moveSpan.Slice(index));
                    index += captures.Length;
                    
                    // if there is an en passant capture available, and it can be made from the current square
                    if (enPassant && (Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] & BitboardUtils.GetSquare(board.enPassant)) != 0)
                        moveSpan[index++] = MagicLookup.EnPassantLookup(BitboardUtils.GetSquare(pos) | BitboardUtils.GetSquare(board.enPassant));
                    
                }
                else // black
                {
                    Span<Move> BPawnMoves = new(MagicLookup.BlackPawnLookupMoves(pos, board.AllPieces() | blockMoves));
                    BPawnMoves.CopyTo(moveSpan);
                    index += BPawnMoves.Length;                                                                 
                    captures = new(MagicLookup.BlackPawnLookupCaptures(pos, board.WhitePieces() & ~blockMoves));
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
                (Move[] moves, ulong captures) rMoves = MagicLookup.RookLookupMoves(pos, board.AllPieces() | blockMoves);
                new Span<Move>(rMoves.moves).CopyTo(moveSpan);
                index += rMoves.moves.Length;

                // magic lookup of only captures
                // form a slice out of the span to ensure that none of the already added moves are overwritten
                captures = new(MagicLookup.RookLookupCaptures(pos, rMoves.captures & board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteBishop:
                (Move[] moves, ulong captures) bMoves = MagicLookup.BishopLookupMoves(pos, board.AllPieces() | blockMoves);
                new Span<Move>(bMoves.moves).CopyTo(moveSpan);
                index += bMoves.moves.Length;
                
                captures = new(MagicLookup.BishopLookupCaptures(pos, bMoves.captures & board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteQueen:
                // find rook moves
                (Move[] moves, ulong captures) moves = MagicLookup.RookLookupMoves(pos, board.AllPieces() | blockMoves);
                new Span<Move>(moves.moves).CopyTo(moveSpan);
                index += moves.moves.Length;
                
                captures = new(MagicLookup.RookLookupCaptures(pos, moves.captures & board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
                
                // find bishop moves
                moves = MagicLookup.BishopLookupMoves(pos, board.AllPieces() | blockMoves);
                new Span<Move>(moves.moves).CopyTo(moveSpan.Slice(index));
                index += moves.moves.Length;
                
                captures = new(MagicLookup.BishopLookupCaptures(pos, moves.captures & board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteKnight:
                // find moves, no captures
                Span<Move> knightMoves = new(MagicLookup.KnightLookupMoves(pos, board.AllPieces() | blockMoves));
                knightMoves.CopyTo(moveSpan);
                index += knightMoves.Length;
                
                // find only captures
                captures = new(MagicLookup.KnightLookupCaptures(pos, board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
            break;
            
            case Pieces.WhiteKing:
                Span<Move> kingMoves = new(MagicLookup.KingLookupMoves(pos, board.AllPieces() | blockMoves));
                kingMoves.CopyTo(moveSpan);
                index += kingMoves.Length;
                
                captures = new(MagicLookup.KingLookupCaptures(pos, board.GetBitboard(1-side) & ~blockMoves));
                captures.CopyTo(moveSpan.Slice(index));
                index += captures.Length;
                
                // castling
                if (side == 0) // white
                {
                    if ((board.castling & 0b1000) != 0 && ((board.AllPieces() | enemyAttacked) & Bitboards.WhiteShortCastleMask) == 0) // white can castle short
                        moveSpan[index++] = Bitboards.WhiteShortCastle;
                    
                    if ((board.castling & 0b0100) != 0 && ((board.AllPieces() | enemyAttacked) & Bitboards.WhiteLongCastleMask) == 0) // white can castle long
                        moveSpan[index++] = Bitboards.WhiteLongCastle;
                }
                else // black
                {
                    if ((board.castling & 0b0010) != 0 && ((board.AllPieces() | enemyAttacked) & Bitboards.BlackShortCastleMask) == 0) // black can castle short
                        moveSpan[index++] = Bitboards.BlackShortCastle;
                    
                    if ((board.castling & 0b0001) != 0 && ((board.AllPieces() | enemyAttacked) & Bitboards.BlackLongCastleMask) == 0) // black can castle long
                        moveSpan[index++] = Bitboards.BlackLongCastle;
                }
                break;
        }

        return index;
    }
    
    private static int SearchPieceCheck(Board board, ulong piece, (int file, int rank) pos, int side, Span<Move> moveSpan, ulong blockPath, bool enPassant = false, ulong pinned = 0, ulong enemyAttacked = 0, bool doubleCheck = false)
    {
        if ((BitboardUtils.GetSquare(pos) & pinned) != 0) // piece pinned
            return 0;

        int index = 0;
        
        if (doubleCheck || (piece & Pieces.TypeMask) == Pieces.WhiteKing)
        {
            Span<Move> kingMoves = new(MagicLookup.KingLookupMoves(pos, board.AllPieces() | enemyAttacked));
            kingMoves.CopyTo(moveSpan);
            index += kingMoves.Length;
                
            Span<Move> captures = new(MagicLookup.KingLookupCaptures(pos, board.GetBitboard(1-side) & ~enemyAttacked));
            captures.CopyTo(moveSpan.Slice(index));
            index += captures.Length;

            return index;
        }
        
        if ((piece & Pieces.TypeMask) != Pieces.WhitePawn) // not pawn
        {
            // moves that can block the check (only single checks)
            // get the bitboard for potential moves (piece can be pinned) AND it with the path the king is checked on to see where the piece can block
            ulong pieceBitboard = SearchPieceBitboard(board, piece, pos, side) & blockPath; 
            ulong captureBitboard = pieceBitboard & board.GetBitboard(1-side); // blocks that land on an enemy piece
            ulong moveBitboard = pieceBitboard & ~captureBitboard; // blocks that aren't
            
            if (captureBitboard != 0)
                moveSpan[index++] = MagicLookup.BlockCaptureLookup(pos, captureBitboard);

            Span<Move> moves = new(MagicLookup.BlockLookup(pos, moveBitboard));
            moves.CopyTo(moveSpan.Slice(index));
            index += moves.Length;
        }
        else // pawn
        {
            ulong attacked = side == 0 ? Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] : Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank];
            ulong capture = attacked & blockPath & board.GetBitboard(1-side);
            ulong move = (side == 0 ? Bitboards.WhitePawnMoveMasks[pos.file, pos.rank] : Bitboards.BlackPawnMoveMasks[pos.file, pos.rank]) & blockPath & ~board.GetBitboard(1-side);
            
            if (move != 0)
            {
                Span<Move> moves = new(MagicLookup.BlockPawnLookup(pos, move));
                moves.CopyTo(moveSpan.Slice(index));
                index += moves.Length;
            }
            else if (capture != 0)
            {
                Span<Move> moves = new(MagicLookup.BlockCapturePawnLookup(pos, capture));
                moves.CopyTo(moveSpan.Slice(index));
                index += moves.Length;
            }
            else if (enPassant)
            {
                if ((BitboardUtils.GetPossibleEnPassantSquare(board.enPassant.file, side) & blockPath) != 0 // gets the pawn that can be taken en passant, if it's in the block path, can take it
                    && (BitboardUtils.GetSquare(board.enPassant) & attacked) != 0) // if the en passant square is within the attacked squares
                    moveSpan[index++] = MagicLookup.EnPassantLookup(BitboardUtils.GetSquare(pos) | BitboardUtils.GetSquare(board.enPassant));
            }
        }
        
        return index;
    }
    
    public static bool Attacked((int file, int rank) pos, Board board, int side) // attacker side
    {
        ulong rookAttack = MagicLookup.RookLookupCaptureBitboards(pos, board.AllPieces()) & (board.GetBitboard(side, Pieces.WhiteRook) | board.GetBitboard(side, Pieces.WhiteQueen));
        ulong bishopAttack = MagicLookup.BishopLookupCaptureBitboards(pos, board.AllPieces()) & (board.GetBitboard(side, Pieces.WhiteBishop) | board.GetBitboard(side, Pieces.WhiteQueen));
        ulong knightAttacks = Bitboards.KnightMasks[pos.file, pos.rank] & board.GetBitboard(side, Pieces.WhiteKnight);
        ulong pawnAttacks = side == 0 ? Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank] & board.bitboards[Pieces.WhitePawn] : Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] & board.bitboards[Pieces.BlackPawn];
        ulong kingAttacks = Bitboards.KingMasks[pos.file, pos.rank] & board.GetBitboard(side, Pieces.WhiteKing);

        return (rookAttack | bishopAttack | knightAttacks | pawnAttacks | kingAttacks) != 0;
    }
    
    private static ulong SearchPieceBitboard(Board board, ulong piece, (int file, int rank) pos, int side)
    {
        switch (piece & Pieces.TypeMask)
        {
            case Pieces.WhitePawn:
                return (side == 0 ? Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] : Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank]);
            case Pieces.WhiteRook:
                return MagicLookup.RookMoveBitboardLookup(pos, board.AllPieces());
            case Pieces.WhiteBishop:
                return MagicLookup.BishopMoveBitboardLookup(pos, board.AllPieces());
            case Pieces.WhiteKnight:
                return Bitboards.KnightMasks[pos.file, pos.rank];
            case Pieces.WhiteQueen:
                return MagicLookup.RookMoveBitboardLookup(pos, board.AllPieces()) | MagicLookup.BishopMoveBitboardLookup(pos, board.AllPieces());
            case Pieces.WhiteKing:
                return Bitboards.KingMasks[pos.file, pos.rank];
            default:
                throw new Exception($"Unknown piece: {piece & Pieces.TypeMask}");
        }
    }
    
    private static ulong SearchPieceBitboard(Board board, ulong piece, (int file, int rank) pos, int side, (int file, int rank) skipSquare)
    {
        switch (piece & Pieces.TypeMask)
        {
            case Pieces.WhitePawn:
                return (side == 0 ? Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] : Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank]);
            case Pieces.WhiteRook:
                return MagicLookup.RookMoveBitboardLookup(pos, board.AllPieces() & ~BitboardUtils.GetSquare(skipSquare));
            case Pieces.WhiteBishop:
                return MagicLookup.BishopMoveBitboardLookup(pos, board.AllPieces() & ~BitboardUtils.GetSquare(skipSquare));
            case Pieces.WhiteKnight:
                return Bitboards.KnightMasks[pos.file, pos.rank];
            case Pieces.WhiteQueen:
                return MagicLookup.RookMoveBitboardLookup(pos, board.AllPieces() & ~BitboardUtils.GetSquare(skipSquare)) | MagicLookup.BishopMoveBitboardLookup(pos, board.AllPieces() & ~BitboardUtils.GetSquare(skipSquare));
            case Pieces.WhiteKing:
                return Bitboards.KingMasks[pos.file, pos.rank];
            default:
                throw new Exception($"Unknown piece: {piece & Pieces.TypeMask}");
        }
    }
    
    private static (bool attacked, bool doubleAttack, ulong attackLines) GetAttackLines((int file, int rank) pos, Board board, int side) // side is attacker side
    {
        ulong rookAttack = MagicLookup.RookLookupCaptureBitboards(pos, board.AllPieces()) & (board.GetBitboard(side, Pieces.WhiteRook) | board.GetBitboard(side, Pieces.WhiteQueen));
        ulong bishopAttack = MagicLookup.BishopLookupCaptureBitboards(pos, board.AllPieces()) & (board.GetBitboard(side, Pieces.WhiteBishop) | board.GetBitboard(side, Pieces.WhiteQueen));
        ulong knightAttacks = Bitboards.KnightMasks[pos.file, pos.rank] & board.GetBitboard(side, Pieces.WhiteKnight);
        ulong pawnAttacks = side == 0 ? Bitboards.BlackPawnCaptureMasks[pos.file, pos.rank] & board.bitboards[Pieces.WhitePawn] : Bitboards.WhitePawnCaptureMasks[pos.file, pos.rank] & board.bitboards[Pieces.BlackPawn];
        ulong kingAttacks = Bitboards.KingMasks[pos.file, pos.rank] & board.GetBitboard(side, Pieces.WhiteKing);

        ulong allAttackers = rookAttack | bishopAttack | knightAttacks | pawnAttacks | kingAttacks;
        
        if (allAttackers == 0) // if no pieces could attack a certain square, there is no need to look further
            return (false, false, 0);

        ulong attackLines = 0;
        int attackersFound = (int)ulong.PopCount(allAttackers);
        
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                if ((allAttackers & BitboardUtils.GetSquare(file, rank)) != 0)
                    attackLines |= Bitboards.PathLookup[pos.file, pos.rank, file, rank] & ~BitboardUtils.GetSquare(pos);
            }
        }

        // attackLines = MagicLookup.AttackLineLookup(pos, allAttackers);

        return (attackersFound > 0, attackersFound > 1, attackLines);
    }

    private static ulong GetAttackedBitboard(Board board, int side, (int file, int rank) skipSquare)
    {
        ulong attacked = 0;

        for (int rank = 0; rank < 8; rank++)
        for (int file = 7; file >= 0; file--)
            if ((board.GetBitboard(side) & BitboardUtils.GetSquare(file, rank)) != 0)
                attacked |= SearchPieceBitboard(board, board.GetPiece(file, rank), (file, rank), side, skipSquare);
        
        return attacked;
    }

    private static (ulong pinned, Dictionary<ulong, ulong> pinStates) GetPinStates(Board board, int side)
    {
        Dictionary<ulong, ulong> pinStates = new();
        ulong pinned = 0;

        ulong rookSelected;
        ulong bishopSelected;

        // king is on the same file where the pawn is taken
        if (board.enPassant.file != 8 && board.enPassant.rank + (side * 2 - 1) == board.KingPositions[side].rank)
        {
            // the en passant cannot happen if the moving pawn would be pinned if the taken pawn is disregarded
            rookSelected = MagicLookup.RookPinLineLookup(board.KingPositions[side], board.GetBitboard(1-side)) & board.AllPieces() & ~BitboardUtils.GetSquare(board.enPassant.file, board.enPassant.rank + (side * 2 - 1));
            bishopSelected = MagicLookup.BishopPinLineLookup(board.KingPositions[side], board.GetBitboard(1-side)) & board.AllPieces() & ~BitboardUtils.GetSquare(board.enPassant.file, board.enPassant.rank + (side * 2 - 1));
        }
        else // no en passant -> everything is normal
        {
            rookSelected = MagicLookup.RookPinLineLookup(board.KingPositions[side], board.GetBitboard(1-side)) & board.AllPieces();
            bishopSelected = MagicLookup.BishopPinLineLookup(board.KingPositions[side], board.GetBitboard(1-side)) & board.AllPieces();   
        }

        List<BitboardUtils.PinSearchResult> rookPinSearch = MagicLookup.RookPinSearch(board.KingPositions[side], rookSelected);
        List<BitboardUtils.PinSearchResult> bishopPinSearch = MagicLookup.BishopPinSearch(board.KingPositions[side], bishopSelected);

        foreach (BitboardUtils.PinSearchResult result in rookPinSearch)
        {
            if ((BitboardUtils.GetSquare(result.pinningPos) & (board.GetBitboard(1-side, Pieces.WhiteQueen) | board.GetBitboard(1-side, Pieces.WhiteRook))) != 0) // is pinned
            {
                pinStates.Add(result.pinnedPiece, result.path);
                pinned |= result.pinnedPiece;
            }
        }
        
        foreach (BitboardUtils.PinSearchResult result in bishopPinSearch)
        {
            if ((BitboardUtils.GetSquare(result.pinningPos) & (board.GetBitboard(1-side, Pieces.WhiteQueen) | board.GetBitboard(1-side, Pieces.WhiteBishop))) != 0) // is pinned
            {
                pinStates.Add(result.pinnedPiece, result.path);
                pinned |= result.pinnedPiece;
            }
        }
        
        return (pinned, pinStates);
    }
}