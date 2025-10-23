using System;
using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze;

namespace BlazeUI;

public class PGNDisplay(StackPanel panel)
{
    private readonly List<string> _game = new();
    private Grid? _last;
    
    public void Add(string move)
    {
        if (_game.Count % 2 == 0)
        {
            // new row added
            _last = new Grid {ColumnDefinitions = new ColumnDefinitions("25,*,*") , Classes = { "GameEntry" }};
            _last.Children.Add(new TextBlock() { Text = Convert.ToString(_game.Count / 2 + 1) + '.' , Classes = { "Indexer" }});
            Button moveText = new Button { Content = move , Classes = { "GameEntryRow" }};
            _last.Children.Add(moveText);
            Grid.SetColumn(moveText, 1);
            panel.Children.Add(_last);
        }
        else
        {
            // no new row added
            Button moveText = new Button { Content = move , Classes = { "GameEntryRow" }};
            _last!.Children.Add(moveText);
            Grid.SetColumn(moveText, 2);
        }
        _game.Add(move);
    }

    public void Clear()
    {
        _game.Clear();
        panel.Children.Clear();
    }
}