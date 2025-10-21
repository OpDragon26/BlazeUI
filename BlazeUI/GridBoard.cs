using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

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
        
        PieceItem piece = _pieces[index];
        
        RemovePiece(to, other => other.pos != from);
        
        Grid.SetColumn(piece.piece, to.x);
        Grid.SetRow(piece.piece, to.y);
        piece.pos = to;
    }

    private void AddPiece(MoveablePiece piece, (int x, int y) at)
    {
        InnerGrid.Children.Add(piece);
        Grid.SetColumn(piece, at.x);
        Grid.SetRow(piece, at.y);
        _pieces.Add(new PieceItem(piece, at));
    }

    public void LoadBoard(Blaze.Board board, Blaze.Side perspective)
    {
        Clear();

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                if (board.GetPiece(file, rank) == Blaze.Pieces.Empty)
                    continue;
                
                (int x, int y) objectivePos = PerspectiveConverter.ToObjective((file, rank), perspective);

                MoveablePiece piece = new MoveablePiece { PieceGrid = this , Source = GetPieceBitmap(board.GetPiece(file, rank))};
                AddPiece(piece, objectivePos);
            }
        }
    }

    private void RemovePiece((int x, int y) at)
    {
        int index = FindIndexOfPiece(at);
        if (index == -1)
            return;

        InnerGrid.Children.Remove(_pieces[index].piece);
        _pieces.RemoveAt(index);
    }

    private bool RemovePiece((int x, int y) at, Func<PieceItem, bool> condition)
    {
        int index = FindIndexOfPiece(at);
        if (index == -1)
            return false;

        if (condition(_pieces[index]))
        {
            RemovePiece(at);
            return true;
        }
        return false;
    }

    private void Clear()
    {
        _pieces.ForEach(item => RemovePiece(item.pos));
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

    private static Bitmap GetPieceBitmap(uint piece)
    {
        return new Bitmap(AbsolutePath + $"{((piece & Blaze.Pieces.ColorMask) == 0 ? "white" : "black")}_{PieceName[piece & Blaze.Pieces.TypeMask]}.png");
    }
    
    private static readonly string AbsolutePath = "/home/opdragon25/Documents/CSharp/AvaloniaChessUI/BlazeUI/assets/pieces/";
    private static readonly Dictionary<uint, string> PieceName = new()
    {
        { 0b000 , "pawn" },
        { 0b001 , "rook" },
        { 0b010 , "knight" },
        { 0b011 , "bishop" },
        { 0b100 , "queen" },
        { 0b101 , "king"}
    };
}

static class PerspectiveConverter
{
    public static (int file, int rank) FromObjective((int x, int y) objective, Blaze.Side sideTo)
    {
        if (sideTo == Blaze.Side.White)
        {
            int file = 7 - objective.x;
            int rank = objective.y;

            return (file, rank);
        }
        else
        {
            int file = objective.x;
            int rank = 7 - objective.y;

            return (file, rank);
        }
    }

    public static (int x, int y) ToObjective((int file, int rank) relative, Blaze.Side sideFrom)
    {
        if (sideFrom == Blaze.Side.White)
        {
            int x = relative.file;
            int y = 7 - relative.rank;

            return (x, y);
        }
        else
        {
            int x = 7 - relative.file;
            int y = relative.rank;

            return (x, y);
        }
    }

}