using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace BlazeUI;

public class OverlayHandler(Grid overlayGrid)
{
    private readonly Dictionary<string, Grid> Overlays = new();
    
    public void AddOverlay(Grid overlay, string name)
    {
        Overlays.TryAdd(name, overlay);
    }

    public void Init()
    {
        overlayGrid.Children.Clear();
    }

    public void SetActive(string name)
    {
        RemoveActive();
        overlayGrid.ZIndex = 5;
        if (Overlays.TryGetValue(name, out var overlay))
            overlayGrid.Children.Add(overlay);
        else
            overlayGrid.ZIndex = -5;
    }

    public void RemoveActive()
    {
        overlayGrid.ZIndex = -5;
        overlayGrid.Children.Clear();
    }
}