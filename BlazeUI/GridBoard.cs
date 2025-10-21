using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace BlazeUI;

public class GridBoard(Grid grid)
{
    public readonly Grid InnerGrid = grid;
    readonly List<PieceItem> _pieces = new();

    public void MovePiece((int x, int y) from, (int x, int y) to)
    {
        int index = FindIndexOfPiece(from);
        if (index == -1)
            return;
        
        Grid.SetColumn(_pieces[index].piece, from.x);
        Grid.SetRow(_pieces[index].piece, from.y);
        _pieces[index].pos = to;
    }

    public void AddPiece(MoveablePiece piece, (int x, int y) at)
    {
        InnerGrid.Children.Add(piece);
        Grid.SetColumn(piece, at.x);
        Grid.SetRow(piece, at.y);
        _pieces.Add(new PieceItem(piece, at));
    }

    private void RemovePiece((int x, int y) at)
    {
        int index = FindIndexOfPiece(at);
        if (index == -1)
            return;

        InnerGrid.Children.Remove(_pieces[index].piece);
        _pieces.RemoveAt(index);
    }

    public void Clear()
    {
        _pieces.ForEach(item => RemovePiece(item.pos));
        _pieces.Clear();
    }

    private int FindIndexOfPiece((int x, int y) at)
    {

        for (int i = 0; i < _pieces.Count; i++)
            if (_pieces[i].pos == at)
                return i;
        return -1;
    }
    
    private class PieceItem(MoveablePiece piece, (int x, int y) pos)
    {
        public readonly MoveablePiece piece = piece;
        public (int x, int y) pos = pos;
    }
}