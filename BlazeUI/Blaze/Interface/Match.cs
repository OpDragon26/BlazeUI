using System;
using System.Linq;
using System.Threading;

namespace BlazeUI.Blaze.Interface;
using Board_Representation;
using Book;
using Search;
using Move_Generation;
using Magic_Lookup;

public class Match
{
    public readonly Board board;
    internal int depth;
    private readonly int depthFloor;
    private const int depthCeiling = 8;
    private readonly bool dynamicDepth;
    private readonly bool delayBook;
    
    private bool inBook;
    internal int ply;
    public readonly Game game;
    
    internal Match(Board board, int depth, bool dynamicDepth = true, bool useBook = true, bool delayBook = false)
    {
        this.board = board;
        this.depth = depth;
        depthFloor = depth;
        inBook = useBook;
        this.dynamicDepth = dynamicDepth;
        this.delayBook = delayBook;
        game = new(new(board));
        
        Init.Start();
    }

    // attempts to make the given move on the board, returns true if successful 
    // time only required for game keeping purposes
    public bool TryMake(Move move, out GameNode? node, long time = -1)
    {
        Move[] legalMoves = MoveGenerator.SearchBoard(board, false).ToArray();
        node = null;

        if (!legalMoves.Contains(move))
            return false;
        
        game.AddNode(move, time);
        board.MakeMove(move);
        
        node = game.LastMove;
        
        ply++;
        return true;
    }
    
    public bool TryMake(Move move, long time = -1)
    {
        Move[] legalMoves = MoveGenerator.SearchBoard(board, false).ToArray();

        if (!legalMoves.Contains(move))
            return false;
        
        game.AddNode(move, time);
        board.MakeMove(move);
        
        ply++;
        return true;
    }

    public GameNode BotMove()
    {
        Searcher.SearchResult bestMove = Searcher.BestMove(board, depth, inBook, ply);
        
        if (bestMove.bookMove && delayBook)
            Thread.Sleep(500);

        game.AddNode(bestMove.move, bestMove.time);
        board.MakeMove(bestMove.move);
        
        if (dynamicDepth)
            UpdateDepth(bestMove);
        
        inBook = bestMove.bookMove;
        ply++;
        return game[^1];
    }

    private void UpdateDepth(Searcher.SearchResult last)
    {
        if (last.bookMove) return;
        if (last.move.Promotion != 0b111)
            depth--;

        Console.WriteLine("Depth adjustment attempt");
        
        int increase = Thresholds[depthFloor, 0];
        int decrease = board.IsEndgame() ? Thresholds[depthFloor, 2] : Thresholds[depthFloor, 1];

        Console.WriteLine($"window {increase} to {decrease}");
        Console.WriteLine($"Depth before: {depth}");
        
        if (last.time < increase) // the move took a short time, increase depth
            depth++;
        else if (last.time > decrease) // the move took a long time, decrease depth
            depth--;
        depth = Math.Clamp(depth, depthFloor, depthCeiling);
        Console.WriteLine($"Depth after: {depth}");
    }

    public static Game RandomGame(int depth)
    {
        Match match = new(new Board(Presets.StartingBoard), depth, false);
        while (!match.GameEnded())
            match.BotMove();
        return match.game;
    }
    
    internal bool GameEnded()
    {
        return GetOutcome() != Outcome.Ongoing;
    }

    public Outcome GetOutcome()
    {
        return board.GetOutcome();
    }
    
    private static readonly int[,] Thresholds = new[,]
    {
        {0, 0, 0}, // 0
        {0, 1000, 1000}, // 1
        {0, 1000, 1000}, // 2
        {0, 1000, 1000}, // 3
        {50, 1000, 1000}, // 4
        {100, 5000, 2000}, // 5
        {300, 9000, 6000}, // 6
        {750, 15000, 10000}, // 7
        {20000, 300000, 150000}, // 8
    };
}