using Avalonia.Threading;
using BlazeUI.Board_Interface;

namespace BlazeUI.Utils;

public static class BotAPI
{
    public static void PlayBotMove(this BoardUI ui)
    {
        if (ui.Match is null)
            return;
        
        ui.GridBoard.locked.Lock();
        
        DispatcherTimer timer = new DispatcherTimer();
        ui.Match.WaitStartSearch();
        timer.Tick += (_, _) =>
        {
            if (ui.Match.Poll(out var node))
            {
                ui.HandleBotMove(node.move!);
                timer.Stop();
            }
        };
        timer.Start();
    }
}