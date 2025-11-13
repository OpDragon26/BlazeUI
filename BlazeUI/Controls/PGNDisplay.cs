using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze.API;
using BlazeUI.Board_Interface;

namespace BlazeUI.Controls;

public class PGNDisplay : StackPanel
{
    public BoardUI? DisplayBoard;
    private readonly List<MoveItem> Moves = new();
    public readonly List<DisplayButton> Buttons = new();
    private int Rows;

    public void AddNode(GameNode node)
    {
        Moves.Add(new(node));
        if (Rows == 0)
        {
            Children.Add(new DisplayRow { Index = ++Rows , Base = this , DisplayBoard = DisplayBoard! });
            DisplayRow newRow = (Children[^1] as DisplayRow)!;
            newRow.Init();
            
            newRow.Add(Moves[^1]);
            return;
        }
        
        DisplayRow lastRow = (Children[^1] as DisplayRow)!;
        if (!lastRow.IsFilled())
            lastRow.Add(Moves[^1]);
        else
        {
            lastRow.Finish();
            Children.Add(new DisplayRow { Index = ++Rows , Base = this , DisplayBoard = DisplayBoard! });
            lastRow = (Children[^1] as DisplayRow)!;
            lastRow.Init();
            lastRow.Add(Moves[^1]);
        }
    }

    public void ClearSelected()
    {
        Buttons.ForEach(button => button.Classes.Remove("SelectedEntry"));
    }
    
    public class MoveItem(GameNode node)
    {
        public readonly GameNode Node = node;
        public readonly string Move = node.Notate();
    }
}