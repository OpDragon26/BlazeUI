using System;
using System.Collections.Generic;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Interface;
using BlazeUI.Blaze.Move_Generation;

namespace BlazeUI.Blaze.Book;

public static class Book
{
    private static readonly List<Layer> book = new();
    private static bool init;
    private static readonly Random random = new();

    public static void Init(string[] origin)
    {
        if (init) return;
        init = true;
        
        foreach (string line in origin)
        {
            //Console.WriteLine(line);
            AddLine(ParseUCI(line));
        }
    }
    
    public static bool TryRetrieve(Board board, int depth, out Move? move)
    {
        move = null;
        if (depth >= book.Count)
            return false;

        return book[depth].TryRetrieve(board, out move);
    }
    
    private static void AddLine(Game line)
    {
        for (int depth = 0; depth < line.Count - 1; depth++)
        {
            AddToLayer(line[depth], depth);
        }
    }

    private static void AddToLayer(GameNode node, int depth)
    {
        if (depth == book.Count)
            book.Add(new());
        book[depth].AddEntry(node.board, node.move!);
    }

    private class Layer
    {
        readonly List<Entry> entries = new();

        public void AddEntry(Board board, Move move)
        {
            Entry? entry = entries.Find(e => e.board.Equals(board));
            if (entry == null)
                entries.Add(new Entry(board, move));
            else
                entry.moves.Add(move);
        }

        public bool TryRetrieve(Board board, out Move? move)
        {
            Entry? entry = entries.Find(e => e.board.Equals(board));
            move = entry?.moves[random.Next(entry.moves.Count)];
            return entry != null;
        }
    }
    
    private class Entry(Board board, Move move)
    {
        public readonly Board board = board;
        public readonly List<Move> moves = [move];
    }

    private static Game ParseUCI(string UCI)
    {
        Game line = new Game(new(Presets.StartingBoard));
        
        string[] uciGame = UCI.Split(' ');

        foreach (string move in uciGame)
            line.AddNodeUCI(move);
        
        return line;
    }
}