using System.Threading;

namespace BlazeUI.Blaze.Interface;
using Board_Representation;
using Move_Generation;

public class EmbeddedMatch(Board board, int depth, bool dynamicDepth = true, bool useBook = true, bool delayBook = false) : Match(board, depth, dynamicDepth, useBook, delayBook)
{
    private bool complete = true;

    private void StartSearch()
    {
        Thread t = new Thread(() =>
        {
            complete = false;
            BotMove();
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

    public bool Poll(out GameNode result)
    {
        result = game.LastMove!;
        return complete;
    }

    private GameNode WaitMove()
    {
        while (!complete)
            Thread.Sleep(10);
        return game.LastMove!;
    }

    public new bool TryMake(Move move, out GameNode node, long time = -1)
    {
        node = game.LastMove!;
        if (!complete)
            return false;
        
        return base.TryMake(move, out node!, time);
    }
    
    public new bool TryMake(Move move, long time = -1)
    {
        if (!complete)
            return false;
        
        return base.TryMake(move, time);
    }
}