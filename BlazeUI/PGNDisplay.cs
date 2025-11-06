using System;
using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze;
using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI;

public class PGNDisplay(StackPanel panel)
{
    private readonly List<(string moveString, Move move, Board board)> _game = new();
    private Grid? _last;
    private GridBoard? _board;
    private int _lastViewed;
    private readonly List<Button> _buttons = new();
    
    public void Add(string moveString, Move move, Board board)
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
            Content = moveString, 
            Classes = { "GameEntryRow" }, 
            Name = Convert.ToString(_game.Count)
        };
        _buttons.Add(moveText);
        
        moveText.Click += (sender, _) =>
        {
            Button button = (sender as Button)!;
            JumpTo(int.Parse(button.Name!));
        };
        
        _last!.Children.Add(moveText);
        Grid.SetColumn(moveText, _game.Count % 2 + 1);
        
        _lastViewed = _game.Count;
        ClearSelected();
        moveText.Classes.Add("SelectedEntry");
        _game.Add((moveString, move, board));
    }

    private void ClearSelected()
    {
        foreach (Button button in _buttons)
        {
            if (button.Classes.Contains("SelectedEntry"))
                button.Classes.Remove("SelectedEntry");
        }
    }
    
    public void Slide(int by)
    {
        JumpTo(_lastViewed + by);
    }

    private void JumpTo(int to)
    {
        to = Math.Clamp(to, 0, _game.Count - 1);
        _lastViewed = to;
        _board!.LoadBoard(_game[_lastViewed].board, _board.side, true);
        if (_lastViewed != _game.Count - 1)
            _board!.LockAll(true);
        
        _board!.ClearHighlight("last-move");
        if (_game[_lastViewed].move.Source.file != 8)
            _board!.HighlightMove(_game[_lastViewed].move, Colors.HighLightMove);
        
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