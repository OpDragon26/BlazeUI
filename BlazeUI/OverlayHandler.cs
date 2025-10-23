using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace BlazeUI;

public class OverlayHandler(Grid overlayGrid)
{
    private readonly Dictionary<string, Grid> Overlays = new();
    public string Active = String.Empty;
    
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
        {
            Active = name;
            overlayGrid.Children.Add(overlay);
        }
        else
            overlayGrid.ZIndex = -5;
    }

    public void Toggle(string name)
    {
        if (Active == name)
            RemoveActive();
        else
            SetActive(name);
    }

    public void RemoveActive()
    {
        Active = String.Empty;
        overlayGrid.ZIndex = -5;
        overlayGrid.Children.Clear();
    }
}