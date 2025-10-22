using System;
using Avalonia.Controls;

namespace BlazeUI;

public class ProgressBar : Border
{
    private Border? _completed;

    public void Init(Border completed)
    {
        _completed = completed;
    }
    
    public void SetCompletion(int percentage)
    {
        percentage = Math.Clamp(percentage, 0, 100);
        double fraction = Width * (percentage / 100d);
        _completed!.Width = fraction;
    }
}