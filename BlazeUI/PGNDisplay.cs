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
    private int _lastViewed;
    private readonly List<Button> _buttons = new();
    
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
        _buttons.Add(moveText);
        
        moveText.Click += (sender, _) =>
        {
            _board!.LoadBoard(board, _board.side);
            Button button = (sender as Button)!;
            if (!button.Name!.Equals(Convert.ToString(_game.Count - 1)))
                _board!.LockAll(true);
            
            _lastViewed = Convert.ToInt32(button.Name);
            ClearSelected();
            button.Classes.Add("SelectedEntry");
        };
        
        _last!.Children.Add(moveText);
        Grid.SetColumn(moveText, _game.Count % 2 + 1);
        
        _lastViewed = _game.Count;
        ClearSelected();
        moveText.Classes.Add("SelectedEntry");
        _game.Add((move, board));
    }

    private void ClearSelected()
    {
        foreach (Button button in _buttons)
        {
            if (button.Classes.Contains("SelectedEntry"))
                button.Classes.Remove("SelectedEntry");
        }
    }

    public void GoBackOne()
    {
        _lastViewed = Math.Max(_lastViewed - 1, 0);
        _board!.LoadBoard(_game[_lastViewed].board, _board.side);
        _board!.LockAll(true);
        
        ClearSelected();
        _buttons[_lastViewed].Classes.Add("SelectedEntry");
    }

    public void GoForwardOne()
    {
        _lastViewed = Math.Min(_lastViewed + 1, _game.Count - 1);
        _board!.LoadBoard(_game[_lastViewed].board, _board.side);
        if (_lastViewed != _game.Count - 1)
            _board!.LockAll(true);
        
        ClearSelected();
        _buttons[_lastViewed].Classes.Add("SelectedEntry");
    }

    public void Clear()
    {
        _game.Clear();
        panel.Children.Clear();
        _buttons.Clear();
        _last = null;
    }

    public void Init(GridBoard grid)
    {
        _board = grid;
    }
}