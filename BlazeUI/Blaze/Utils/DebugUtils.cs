using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze.Utils;
using Board_Representation;
using Interface;
using Move_Generation;
using Search;

public static class DebugUtils
{
    public static void TestGameSpeed(int games, int depth)
    {
        List<Game> gamesPlayed = new();
        for (int i = 0; i < games; i++)
        {
            Game game = Match.RandomGame(depth);
            gamesPlayed.Add(game);
            Console.WriteLine(game.GetPGN());
            Console.WriteLine($"Game {i + 1}/{games}");
        }

        List<GameNode> allNodes = gamesPlayed.Aggregate(((game1, game2) => game1.Join(game2))).ToList();
        
        Console.WriteLine($"Average time per move: {allNodes.Where(node => node.time > 50).Average(node => node.time)}ms");
    }
    
    public static void Breakdown(Board board, int depth)
    {
        Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
        Array.Sort(moves);
        ulong total = 0;

        foreach (Move move in moves)
        {
            Board moveBoard = new Board(board);
            moveBoard.MakeMove(move);
            ulong perftResult = Perft.RunSingle(depth - 1, moveBoard, false, depth > 3, null, false);
            Console.WriteLine($"{move.GetUCI()}: {perftResult}");
            total += perftResult;
        }
        
        Console.WriteLine($"Nodes Searched: {total}");
    }
    
    public static void BreakdownEval(Board board, int depth)
    {
        Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
        Array.Sort(moves);

        foreach (Move move in moves)
        {
            Board moveBoard = new Board(board);
            moveBoard.MakeMove(move);
            Console.WriteLine($"{move.GetUCI()}: {Searcher.Minimax(moveBoard, depth - 1, int.MinValue, int.MaxValue)}");
        }
    }

    public static void ExamineEval(Board board, int depth)
    {
        if (depth < 1)
            return;
        
        BreakdownEval(board, depth);
        
        Console.Write("Examine: ");
        string input = Console.ReadLine()!;
        board.MakeMove(new Move(input, board));
        ExamineEval(board, depth - 1);
    }
    
    public static void BreakdownWithExamine(Board board, int depth)
    {
        Console.WriteLine();
        
        if (depth < 1)
            Console.WriteLine("Depth too low");
        if (depth == 1)
        {
            Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
            Array.Sort(moves);
            
            foreach (Move move in moves)
                Console.WriteLine($"{move.GetUCI()}");
            
            Console.WriteLine($"Found: {moves.Length}");
        }
        else
        {
            Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
            
            Array.Sort(moves);
            ulong total = 0;

            foreach (Move move in moves)
            {
                Board moveBoard = new Board(board);
                moveBoard.MakeMove(move);
                ulong perftResult = Perft.RunSingle(depth - 1, moveBoard, false, depth > 3, null, false);
                Console.WriteLine($"{move.GetUCI()}: {perftResult}");
                total += perftResult;
            }
            Console.WriteLine($"Nodes Searched: {total}");
        }
    }
}