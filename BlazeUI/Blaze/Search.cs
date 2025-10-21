using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazeUI.Blaze;

public static class Search
{
    public static SearchResult BestMove(Board board, int depth, bool useBook, int bookDepth)
    {
        if (useBook)
        {
            Output output = Book.Retrieve(board, bookDepth);
            if (output.result == Result.Found)
                return new SearchResult(output.move, 1, true, 0);
        }
        
        Timer timer = new Timer();
        timer.Start();
        
        Move[] moves = SearchBoard(board).ToArray();
        int[] evals = new int[moves.Length];
        if (moves.Length == 0) throw new Exception("No move found");

        
        Parallel.For(0, moves.Length, i =>
        {
            Board moveBoard = new(board);
            moveBoard.MakeMove(moves[i]);
            evals[i] = Minimax(moveBoard, depth - 1, int.MinValue, int.MaxValue, moves[i]);
        });
        

        /*
        for (int i = 0; i < moves.Length; i++)
        {
            Board moveBoard = new(board);
            moveBoard.MakeMove(moves[i]);
            evals[i] = Minimax(moveBoard, depth - 1, int.MinValue, int.MaxValue);
        }
        */
        
        if (board.side == 0)
            return  new SearchResult(moves[Array.IndexOf(evals, evals.Max())], evals.Max(), false, timer.Stop()); // white
        return  new SearchResult(moves[Array.IndexOf(evals, evals.Min())], evals.Min(), false, timer.Stop()); // black
    }

    public readonly struct SearchResult(Move move, int eval, bool bookMove, long time)
    {
        public readonly Move move = move;
        public readonly int eval = eval;
        public readonly bool bookMove = bookMove;
        public readonly long time = time;
    }
    
    public static int Minimax(Board board, int depth, int alpha, int beta, Move? previous = null)
    {
        if (board.IsDraw())
            return 0;

        if (depth == 0) // return heuristic evaluation
            return StaticEvaluate(board);
        
        Board moveBoard;
        if (board.side == 0)
        {
            // white - maximizing player
            int eval = int.MinValue;
            Span<Move> moves = SearchBoard(board);
            
            if (moves.Length == 0)
            {
                if (Attacked(board.KingPositions[0], board, 1)) // if the king is in check
                    // black won by checkmate
                    // the higher the depth, the closer to the origin, the worse for white
                    return int.MinValue + 100 - depth;
                return 0; // game is a draw by stalemate
            }
            
            // for each child
            foreach (Move move in moves)
            {
                moveBoard = new(board);
                moveBoard.MakeMove(move);
                
                eval = Math.Max(eval, Minimax(moveBoard, depth - 1, alpha, beta, move));
                alpha = Math.Max(alpha, eval);
                
                if (eval >= beta) // beta cutoff
                {
                    RefutationTable.Set(board.hashKey, move, 100);
                    Counter.Set(previous, move, depth * depth);
                    History.Set(move, 0, depth * depth);
                    break;
                }
            }
            
            return eval;
        }
        else
        {
            // black - minimizing player
            int eval = int.MaxValue;
            Span<Move> moves = SearchBoard(board);

            if (moves.Length == 0)
            {
                if (Attacked(board.KingPositions[1], board, 0)) // if the king is in check
                    // white won by checkmate
                    // the higher the depth, the closer to the origin, and better for white
                    return int.MaxValue - 100 + depth;
                return 0;
            }
            
            foreach (Move move in moves)
            {
                moveBoard = new(board);
                moveBoard.MakeMove(move);
                
                eval = Math.Min(eval, Minimax(moveBoard, depth - 1, alpha, beta, move));
                beta = Math.Min(beta, eval);
                
                if (eval <= alpha) // alpha cutoff
                {
                    RefutationTable.Set(board.hashKey, move, 100);
                    Counter.Set(previous, move, depth * depth);
                    History.Set(move, 1, depth * depth);
                    break;
                }
            }
            
            return eval;
        }
    }

    // returns the heuristic evaluation of the board
    private static int StaticEvaluate(Board board)
    {
        int eval = 0;

        ulong all = board.AllPieces();
        
        if (!board.IsEndgame())
        {
            // pawns
            eval += Evaluation.Lookup.PawnRegular(0, board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += Evaluation.Lookup.PawnRegular(1, board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // rooks
            eval += Evaluation.Lookup.RookRegular(0, board.bitboards[Pieces.WhiteRook], all, 
                board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += Evaluation.Lookup.RookRegular(1, board.bitboards[Pieces.BlackRook], all, 
                board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // queens
            eval += Evaluation.Lookup.QueenRegular(0, board.bitboards[Pieces.WhiteQueen], all);
            eval += Evaluation.Lookup.QueenRegular(1, board.bitboards[Pieces.BlackQueen], all);
            
            // knight
            eval += Evaluation.Lookup.KnightRegular(0, board.bitboards[Pieces.WhiteKnight], all);
            eval += Evaluation.Lookup.KnightRegular(1, board.bitboards[Pieces.BlackKnight], all);
            
            // bishop
            eval += Evaluation.Lookup.BishopRegular(0, board.bitboards[Pieces.WhiteBishop], all);
            eval += Evaluation.Lookup.BishopRegular(1, board.bitboards[Pieces.BlackBishop], all);
            
            // king
            eval += MagicLookup.KingEvalLookup(board.KingPositions[0]).wEval;
            eval += MagicLookup.KingEvalLookup(board.KingPositions[1]).bEval;
            
            for (int file = 0; file < 8; file++)
            {
                // counts pawns on the file and applies a penalty for multiple on one file
                eval += Weights.DoublePawnPenalties[UInt64.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn])];
                eval -= Weights.DoublePawnPenalties[UInt64.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn])];
            }

            // add or take eval according to which side has castled
            if ((board.castled & 0b10) != 0) // white castled
                eval += Weights.CastlingBonus;
            else
            {
                if ((board.castling & 0b1000) != 0) // can't short castle
                    eval -= Weights.NoCastlingPenalty;
                if ((board.castling & 0b100) != 0) // can't long castle
                    eval -= Weights.NoCastlingPenalty;
            }
            
            if ((board.castled & 0b1) != 0) // black castled
                eval -= Weights.CastlingBonus;
            else
            {
                if ((board.castling & 0b10) != 0) // can't short castle
                    eval += Weights.NoCastlingPenalty;
                if ((board.castling & 0b1) != 0) // can't long castle
                    eval += Weights.NoCastlingPenalty;
            }
            
            
            // check if white's king is in the right spot (likely castled) to have its safety evaluated
            if ((Bitboards.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
            {
                // add to the eval based on the safety of white's king
                eval += MagicLookup.KingSafetyBonusLookup(board.KingPositions[0], board.WhitePieces());
                if ((Bitboards.KingMasks[board.KingPositions[0].file, board.KingPositions[0].rank] & board.BlackPieces()) != 0) // if there is an enemy piece adjacent to the king
                    eval -= 30;
            
                // take from eval if the pawns in front of the king are missing
                foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[0].file])
                    if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn]) == 0)
                        eval -= 30;
            }

            if ((Bitboards.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
            {
                eval -= MagicLookup.KingSafetyBonusLookup(board.KingPositions[1], board.BlackPieces());
                if ((Bitboards.KingMasks[board.KingPositions[1].file, board.KingPositions[1].rank] & board.WhitePieces()) != 0) // if there is an enemy piece adjacent to the king
                    eval += 30;
            
                foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[1].file])
                    if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn]) == 0)
                        eval += 30;
            }
        }
        else
        {
            // pawns
            eval += Evaluation.Lookup.PawnEndgame(0, board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += Evaluation.Lookup.PawnEndgame(1, board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // rooks
            eval += Evaluation.Lookup.RookEndgame(0, board.bitboards[Pieces.WhiteRook], all);
            eval += Evaluation.Lookup.RookEndgame(1, board.bitboards[Pieces.BlackRook], all);
            
            // queens
            eval += Evaluation.Lookup.QueenEndgame(0, board.bitboards[Pieces.WhiteQueen], all);
            eval += Evaluation.Lookup.QueenEndgame(1, board.bitboards[Pieces.BlackQueen], all);
            
            // knights
            eval += Evaluation.Lookup.KnightEndgame(0, board.bitboards[Pieces.WhiteKnight], all);
            eval += Evaluation.Lookup.KnightEndgame(1, board.bitboards[Pieces.BlackKnight], all);
            
            // bishops
            eval += Evaluation.Lookup.BishopEndgame(0, board.bitboards[Pieces.WhiteBishop], all);
            eval += Evaluation.Lookup.BishopEndgame(1, board.bitboards[Pieces.BlackBishop], all);
            
            // king
            eval += MagicLookup.KingEvalLookup(board.KingPositions[0]).wEvalEndgame;
            eval += MagicLookup.KingEvalLookup(board.KingPositions[1]).bEvalEndgame;
            
            for (int file = 0; file < 8; file++)
            {
                // counts pawns on the file and applies a penalty for multiple on one file
                eval += Weights.DoublePawnPenalties[UInt64.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn])];
                eval -= Weights.DoublePawnPenalties[UInt64.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn])];
            }
        }

        return eval;
    }
    
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
        
        if (ordering)
        {
            Span<Move> moveSpan = new Span<Move>(moveArray, 0, index);
            
            int[] keys = new int[moveSpan.Length];
            
            for (int i = 0; i < moveSpan.Length; i++)
                keys[i] = Reevaluate(board, moveSpan[i], previous);

            new Span<int>(keys).Sort(moveSpan, (x, y) => y.CompareTo(x));
            
            return moveSpan;
        }

        return new Span<Move>(moveArray, 0, index);
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
                if ((Bitboards.WhitePawnCaptureMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackPawn:
                if ((Bitboards.BlackPawnCaptureMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
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
                if ((Bitboards.KnightMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
                    priority += 50;
                break;
            case Pieces.BlackKnight:
                if ((Bitboards.KnightMasks[move.Destination.file, move.Destination.rank] & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
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

    public static Move[] FilterChecks(Move[] moves, Board board)
    {
        List<Move> MoveList = moves.ToList();

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
    
    // pseudolegal generation (unordered)
    public static Span<Move> PseudolegalSearchBoard(Board board)
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
                    index += PseudolegalSearchPiece(board, board.GetPiece(file, rank), (file, rank), board.side, moveSpan, enPassant);
                }
            }
        }

        return new Span<Move>(moveArray, 0, index);
    }
    
    private static int PseudolegalSearchPiece(Board board, ulong piece, (int file, int rank) pos, int side, Span<Move> moveSpan, bool enPassant = false)
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