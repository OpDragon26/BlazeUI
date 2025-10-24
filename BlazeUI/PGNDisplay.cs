using System;
using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze;

namespace BlazeUI;

public class PGNDisplay(StackPanel panel)
{
    private readonly List<(string move, Board board)> _game = new();
    private Grid? _last;
    private GridBoard? _board;
    
    public void Add(string move, Board board)
    {
        if (_game.Count % 2 == 0)
        {
            // new row added
            _last = new Grid { 
                ColumnDefinitions = new ColumnDefinitions("25,*,*"), 
                Classes = { "GameEntry" } 
            };
            _last.Children.Add(new TextBlock() {
                Text = Convert.ToString(_game.Count / 2 + 1) + '.', 
                Classes = { "Indexer" }
            });
            
            panel.Children.Add(_last);
        }
        
        Button moveText = new Button {
            Content = move, 
            Classes = { "GameEntryRow" }, 
            Name = Convert.ToString(_game.Count)
        };
        
        moveText.Click += (sender, _) =>
        {
            _board!.LoadBoard(board, _board.side);
            string buttonName = (sender as Button)!.Name!;
            if (!buttonName.Equals(Convert.ToString(_game.Count - 1)))
                _board!.LockAll(true);
        };
        
        _last!.Children.Add(moveText);
        Grid.SetColumn(moveText, _game.Count % 2 + 1);
        
        _game.Add((move,  board));
    }

    public void Clear()
    {
        _game.Clear();
        panel.Children.Clear();
    }

    public void Init(GridBoard grid)
    {
        _board = grid;
    }
}