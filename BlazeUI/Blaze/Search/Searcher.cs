using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlazeUI.Blaze.Search;
using Board_Representation;
using Evaluation;
using Move_Generation;
using static Utils.General;
using static Move_Generation.MoveGenerator;

public static class Searcher
{
    public static SearchResult BestMove(Board board, int depth, bool useBook, int bookDepth)
    {
        if (useBook)
            if (Book.Book.TryRetrieve(board, bookDepth, out Move? move))
                return new SearchResult(move!, 1, true, 0);
        
        Timer timer = new Timer();
        timer.Start();
        
        Move[] moves = MoveGenerator.SearchBoard(board).ToArray();
        int[] evals = new int[moves.Length];
        if (moves.Length == 0) throw new Exception("No move found");

        int alpha = int.MinValue;
        int beta = int.MaxValue;
        int eval = board.side == 0 ? int.MinValue : int.MaxValue;
        
        Parallel.For(0, moves.Length, i =>
        {
            Board moveBoard = new(board);
            moveBoard.MakeMove(moves[i]);
            evals[i] = Minimax(moveBoard, depth - 1, alpha, beta, moves[i]);

            if (board.side == 0)
            {
                eval = Math.Max(eval, evals[i]);
                alpha = Math.Max(alpha, eval);
            }
            else
            {
                eval = Math.Min(eval, evals[i]);
                beta = Math.Min(beta, eval);
            }
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
            return Evaluator.StaticEvaluate(board);
        
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
}