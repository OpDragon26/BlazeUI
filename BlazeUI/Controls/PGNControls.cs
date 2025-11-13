using Avalonia.Controls;
using BlazeUI.Blaze.Utils;
using BlazeUI.Board_Interface;

namespace BlazeUI.Controls;

    public class DisplayRow : Grid
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
        
        public void Add(PGNDisplay.MoveItem item)
        {
            DisplayButton button = new DisplayButton { 
                Item = item, 
                DisplayBoard = DisplayBoard, 
                Base = Base,
            };
            button.Init();
            Children.Add(button);
            Base.Buttons.Add(button);
            SetColumn(button, 1);
            
            if (IsFilled())
            {
                SetColumn(button, 2);
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

    public class DisplayButton : Button
    {
        public required PGNDisplay.MoveItem Item;
        public required BoardUI DisplayBoard;
        public required PGNDisplay Base;
        public bool IsLast = true;

        public void Init()
        {
            Content = Item.Move;
            Classes.Add("GameEntryButton");
            
            Click += (_,_) =>
            {
                DisplayBoard.GridBoard.LoadBoard(BoardUtils.General.AfterMove(Item.Node.board, Item.Node.move!), DisplayBoard.PlayerSide, false, Item.Node.move);
                DisplayBoard.GridBoard.locked.Lock();
                if (IsLast)
                    DisplayBoard.GridBoard.locked.Unlock(DisplayBoard.PlayerSide);
                
                Base.ClearSelected();
                Classes.Add("SelectedEntry");
            };
        }
    }