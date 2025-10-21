using System;
using System.Collections.Generic;
using System.Threading;

namespace BlazeUI.Blaze;

public static class Book
{
    static readonly List<Entry>[] book = new List<Entry>[15];
    private static bool init;
    private static readonly Random random = new();

    public static void Init(string[] origin)
    {
        // ensures that the code only runs once
        if (init) return;
        init = true;
        
        // set the first board to the staring board
        book[0] = [new Entry { board = new Board(Presets.StartingBoard), moves = new List<BookMove>() }];
        for (int i = 1; i < 15; i++)
            book[i] = new List<Entry>();
        
        foreach (string line in origin)
        {
            //Console.WriteLine(line);
            AddLine(Parser.ParseUCI(line));
        }
    }
    
    // tries to find the given board at the given depth, and if it found it, return a random stored move for that board
    public static Output Retrieve(Board board, int depth)
    {
        if (depth > 17)
            return new Output { move = new Move((8,8),(8,8)), result = Result.NotFound };
        
        foreach (Entry entry in book[depth])
        {
            // if the board that belongs to the entry is the given board
            if (entry.board.Equals(board))
            {
                return Pick(entry.moves);
            }
        }
        
        return new Output { move = new Move((8,8), (8,8)), result = Result.NotFound };
    }

    private static Output Pick(List<BookMove> moves)
    {
        if (moves.Count == 0)
            return new Output { move = new Move((8,8),(8,8)), result = Result.NotFound };
        if (moves.Count == 1)
            return new Output { move = moves[0].move, result = Result.Found };
        
        List<int> indices = new List<int>();
        for (int move = 0; move < moves.Count; move++)
        {
            for (int i = 0; i < moves[move].weight; i++)
            {
                indices.Add(move);
            }
        }
        
        Move picked = moves[indices[random.Next(indices.Count)]].move;
        return new Output { move = picked, result = Result.Found };
    }

    private static void AddLine(PGNNode[] line)
    {
        // for the first node
        // if the board has the move, increase its weight
        // for each move of the board
        bool found = false;
        foreach (BookMove move in book[0][0].moves)
        {
            // if the board contains the move, increase its weight
            if (move.move.Equals(line[0].move))
            {
                found = true;
                move.weight += 1;
                break;
            }
        }
        // if the board does not contain the move, add it
        if (!found)
        {
            book[0][0].moves.Add(new BookMove { move = line[0].move, weight = 1 });
            // add the resulting board to the next list
            book[1].Add(new Entry { board = line[0].board, moves = new List<BookMove>() });
        }
        
        // for each node
        for (int i = 1; i < 14; i++)
        {
            // for each board at the given depth
            for (int board = 0; board < book[i].Count; board++)
            {
                // if the board belongs to the previous node
                if (line[i-1].board.Equals(book[i][board].board))
                {
                    // board is book[i][board]
                    
                    // if the board has the move, increase its weight
                    // for each move of the board
                    found = false;
                    foreach (BookMove move in book[i][board].moves)
                    {
                        // if the board contains the move, increase its weight
                        if (move.move.Equals(line[i].move))
                        {
                            found = true;
                            move.weight += 1;
                            break;
                        }
                    }
                    // if the board does not contain the move, add it
                    if (!found)
                    {
                        book[i][board].moves.Add(new BookMove { move = line[i].move, weight = 1 });
                        // add the resulting board to the next list
                        if (i != 13)
                            book[i+1].Add(new Entry { board = line[i].board, moves = new List<BookMove>() });
                    }
                    
                    break;
                }
            }
        }
    }
}

public struct Entry
{
    public Board board;
    public List<BookMove> moves;
}

public class BookMove
{
    public required Move move;
    public int weight;
}

public enum Result
{
    Found,
    NotFound,
}
public struct Output
{
    public Result result;
    public Move move;
}

public static class Parser
{
    public static void PrintGame(PGNNode[] game, int perspective, int pause = 10)
    {
        foreach (PGNNode node in game)
        {
            Console.Clear();
            CLIMatch.PrintBoard(node.board, perspective);
            Thread.Sleep(pause * 100);
        }
    }

    public static PGNNode[] ParsePGN(string pgn)
    {
        List<PGNNode> nodes = new List<PGNNode>();
        string[] game = pgn.Replace("\n", " ").Split(' ');
        Board board = new Board(Presets.StartingBoard);

        foreach (string alg in game)
        {
            if (alg.Equals(string.Empty) || alg[^1] == '.' || alg.Equals("0-1") || alg.Equals("1-0") || alg.Equals("1/2-1/2")) // notates the index of the move, or end of game
                continue;
            Move move;
            try
            {
                move = Move.Parse(alg, board); // converts the move from algebraic notation to Move
            }
            catch
            {
                Console.WriteLine(alg);
                throw;
            }

            board.MakeMove(move);

            nodes.Add(new PGNNode { board = new Board(board), move = move });
        }

        return nodes.ToArray();
    }

    public static string ToUCI(PGNNode[] game)
    {
        List<string> UCI = [];
        foreach (PGNNode node in game)
            UCI.Add(node.move.GetUCI());
        
        return string.Join(' ', UCI);
    }

    public static PGNNode[] ParseUCI(string pgn)
    {
        List<PGNNode> nodes = new List<PGNNode>();
        string[] game = pgn.Replace("\n", " ").Split(' ');
        Board board = new Board(Presets.StartingBoard);

        foreach (string uci in game)
        {
            if (uci.Equals(string.Empty) || uci[^1] == '.') // notates the index of the move
                continue;
            Move move = new Move(uci, board); // converts the move from UCI notation to Move
            board.MakeMove(move);
            
            nodes.Add(new PGNNode { board = new Board(board), move = move });
        }

        return nodes.ToArray();
    }
}

public struct PGNNode(Board board, Move move, long time = 0)
{
    public Board board = board;
    public Move move = move;
    public readonly long time = time;
}
