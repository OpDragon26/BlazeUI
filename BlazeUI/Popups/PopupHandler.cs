using System.Collections.Generic;
using Avalonia.Controls;

namespace BlazeUI.Popups;

public class PopupHandler : Grid
{
    private readonly Dictionary<string, Control> Popups = new();
    private string? Active;
    
    public void Initialize()
    {
        ZIndex = -5;

        foreach (Control child in Children)
            if (child.Name is not null)
                Popups.Add(child.Name, child);
    }

    public void SetActive(string name)
    {
        ClearActive();
        Active = name;
        Children.Add(Popups[name]);
    }

    public void ClearActive()
    {
        Children.Clear();
        Active = null;
    }

    public void Toggle(string name)
    {
        if (Active == name)
            ClearActive();
        else
            SetActive(name);
    }
}