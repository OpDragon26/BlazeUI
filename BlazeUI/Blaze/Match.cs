using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze;

public class Match
{
    public readonly Board board;
    internal int depth;
    private readonly int depthFloor;
    private const int depthCeiling = 8;
    private readonly bool dynamicDepth;
    
    private bool inBook;
    internal int ply;
    public List<PGNNode> game = new();
    
    internal Match(Board board, int depth, bool dynamicDepth = true, bool useBook = true)
    {
        this.board = board;
        this.depth = depth;
        depthFloor = depth;
        inBook = useBook;
        this.dynamicDepth = dynamicDepth;
        
        RefutationTable.Init((int)Math.Pow(2, 20) + 7);
        Bitboards.Init();
        Hasher.Init();
        Book.Init(Books.Standard);
    }

    // attempts to make the given move on the board, returns true if successful 
    // time only required for game keeping purposes
    public bool TryMake(Move move, out PGNNode node, long time = -1)
    {
        Move[] legalMoves = Search.SearchBoard(board, false).ToArray();
        node = new PGNNode();

        if (!legalMoves.Contains(move))
            return false;
        
        board.MakeMove(move);
        
        node = new PGNNode(new(board), move, time);
        game.Add(node);
        
        ply++;
        return true;
    }
    
    public bool TryMake(Move move, long time = -1)
    {
        Move[] legalMoves = Search.SearchBoard(board, false).ToArray();

        if (!legalMoves.Contains(move))
            return false;
        
        board.MakeMove(move);
        
        PGNNode node = new PGNNode(new(board), move, time);
        game.Add(node);
        
        ply++;
        return true;
    }

    public PGNNode BotMove()
    {
        Search.SearchResult bestMove = Search.BestMove(board, depth, inBook, ply);
        
        board.MakeMove(bestMove.move);

        PGNNode node = new PGNNode(new(board), bestMove.move, bestMove.time);
        game.Add(node);
        
        if (dynamicDepth)
            UpdateDepth(bestMove);
        
        inBook = bestMove.bookMove;
        ply++;
        return node;
    }

    private void UpdateDepth(Search.SearchResult last)
    {
        if (last.bookMove) return;

        int increase = Thresholds[depthFloor, 0];
        int decrease = board.IsEndgame() ? Thresholds[depthFloor, 2] : Thresholds[depthFloor, 1];

        if (last.time < increase) // the move took a short time, increase depth
            depth++;
        else if (last.time > decrease) // the move took a long time, decrease depth
            depth--;
        depth = Math.Clamp(depth, depthFloor, depthCeiling);
    }

    public string NotateLastMove()
    {
        if (game.Count == 0)
            return string.Empty;
        if (game.Count == 1)
            return game[0].move.Notate(new Board(Presets.StartingBoard));
        return game[^1].move.Notate(game[^2].board);
    }

    public static List<PGNNode> RandomGame(int depth)
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
        {1500, 30000, 20000}, // 7
        {20000, 300000, 150000}, // 8
    };
}