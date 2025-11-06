using System;

namespace BlazeUI.Blaze;

public class Timer
{
    private long StartTime = 0;

    public void Start()
    {
        StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public long Stop()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartTime;
    }
}