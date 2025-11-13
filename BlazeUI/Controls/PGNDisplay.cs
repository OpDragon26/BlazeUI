using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Utils;
using BlazeUI.Board_Interface;

namespace BlazeUI.Controls;

public class PGNDisplay : StackPanel
{
    public BoardUI? DisplayBoard;
    private readonly List<MoveItem> Moves = new();
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
    
    private class MoveItem(GameNode node)
    {
        public readonly GameNode Node = node;
        public readonly string Move = node.Notate();
    }

    private class DisplayRow : Grid
    {
        public required int Index;
        public required PGNDisplay Base;
        public required BoardUI DisplayBoard;
        private string Label => $"{Index}. ";

        public void Init()
        {
            ColumnDefinitions = new ColumnDefinitions("25,*,*");
            Classes.Add("GameEntry");
            Children.Add(new TextBlock()
            {
                Text = Label,
                Classes = { "Indexer" }
            });
        }
        
        public void Add(MoveItem item)
        {
            Children.Add(new DisplayButton { Item = item , DisplayBoard = DisplayBoard , Base = Base });
            if (IsFilled())
            {
                DisplayButton lastChild = (Children[1] as DisplayButton)!;
                lastChild.IsLast = false;
            }
        }

        public void Finish()
        {
            DisplayButton lastChild = (Children[2] as DisplayButton)!;
            lastChild.IsLast = false;
        }

        public bool IsFilled()
        {
            return Children.Count > 2;
        }
    }

    private class DisplayButton : Button
    {
        public required MoveItem Item;
        public required BoardUI DisplayBoard;
        public required PGNDisplay Base;
        public bool IsLast = true;

        public void Init()
        {
            Content = Item.Move;
            Classes.Add("GameEntryRow");
            
            Click += (_,_) =>
            {
                DisplayBoard.GridBoard.LoadBoard(BoardUtils.General.AfterMove(Item.Node.board, Item.Node.move!), DisplayBoard.PlayerSide, false, Item.Node.move);
                DisplayBoard.GridBoard.locked.Lock();
                if (IsLast)
                    DisplayBoard.GridBoard.locked.Unlock(DisplayBoard.PlayerSide);
            };
        }
    }
}