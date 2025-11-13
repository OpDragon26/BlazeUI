using System;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI.BotAPI;

public static class InitBot
{
    public static void Initialize(Action tick, Action finished)
    {
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        Init.StartInit();
        
        timer.Tick += (_, _) =>
        {
            tick();
            if (Init.init == Init.InitStatus.Complete)
            {
                finished();
                timer.Stop();
            }
        };
        
        timer.Start();
    }
}