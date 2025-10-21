using System;
using System.Threading;

namespace BlazeUI.Blaze;

public class EmbeddedMatch(Board board, int depth, bool dynamicDepth = true, bool useBook = true) : Match(board, depth, dynamicDepth, useBook)
{
    private bool complete = true;
    PGNNode last = new PGNNode {board = board};

    private void StartSearch()
    {
        Thread t = new Thread(() =>
        {
            complete = false;
            last = BotMove();
            complete = true;
        });
        t.Start();
    }
    
    public void WaitStartSearch()
    {
        if (!complete)
            WaitMove();
        StartSearch();
    }

    public bool TryStartSearch()
    {
        if (complete)
            StartSearch();
        return complete;
    }

    public bool Poll(out PGNNode result)
    {
        result = last;
        return complete;
    }

    public PGNNode WaitMove()
    {
        while (!complete)
            Thread.Sleep(10);
        return last;
    }

    public new bool TryMake(Move move, out PGNNode node, long time = -1)
    {
        node = new PGNNode();
        if (!complete)
            return false;
        
        return base.TryMake(move, out node, time);
    }
    
    public new bool TryMake(Move move, long time = -1)
    {
        if (!complete)
            return false;
        
        return base.TryMake(move, time);
    }
}