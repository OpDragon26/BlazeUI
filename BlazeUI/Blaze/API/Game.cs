using System;
using System.Collections;
using System.Collections.Generic;

namespace BlazeUI.Blaze.API;
using Board_Representation;
using Move_Generation;

public class Game(Board board) : IEnumerable<GameNode>
{
    private readonly List<GameNode> nodes = [new(board, null)];
    private Outcome outcome = Outcome.Ongoing;
    public int Count => nodes.Count - 1;
    public int Length => nodes.Count / 2;
    public GameNode? LastMove => Count == 0 ? null : nodes[^2];
    
    public GameNode this[Index i] => nodes[i];
    
    public void AddNode(Move move, long time = -1)
    {
        nodes[^1].move = move;
        nodes[^1].time = time;
        
        Board nextBoard = new Board(nodes[^1].board);
        nextBoard.MakeMove(move);
        outcome = nextBoard.GetOutcome();
        
        nodes.Add(new(nextBoard, null));
    }

    public void AddNodeUCI(string UCI, long time = -1)
    {
        AddNode(new(UCI, nodes[^1].board), time);
    }

    public string GetPGN()
    {
        string pgn = "";

        for (int i = 0; i < Count; i++)
        {
            // add number if necessary
            if (i % 2 == 0)
                pgn += $"{i / 2 + 1}. ";

            pgn += nodes[i].Notate() + " ";
        }

        return pgn;
    }

    public Game Join(Game other)
    {
        nodes.AddRange(other);
        return this;
    }
    
    IEnumerator<GameNode> IEnumerable<GameNode>.GetEnumerator()
    {
        return nodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return nodes.GetEnumerator();
    }
}

public class GameNode(Board board, Move? move, long time = -1)
{
    public bool final => move == null;
    public readonly Board board = board; // board before move
    public Move? move = move;
    public long time = time;

    public string Notate()
    {
        return move is null ? "..." : move.Notate(board);
    }
}