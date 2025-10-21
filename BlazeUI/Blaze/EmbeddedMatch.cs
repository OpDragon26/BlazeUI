using System.Threading;

namespace BlazeUI.Blaze;

public class EmbeddedMatch(Board board, int depth, bool dynamicDepth = true, bool useBook = true) : Match(board, depth, dynamicDepth, useBook)
{
    private bool complete = true;
    PGNNode last = new PGNNode {board = board};

    public void StartSearch()
    {
        if (!complete)
            WaitMove();
        Thread t = new Thread(() =>
        {
            complete = false;
            last = BotMove();
            complete = true;
        });
        t.Start();
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
}