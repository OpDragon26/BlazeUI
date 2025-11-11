using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Utils;
using static BlazeUI.Utils.BoardUIUtils;

namespace BlazeUI.Board_Interface;
public class PieceGrid : Grid
{
    public BoardUI? Base { get; set; }
    private readonly List<PieceItem> PieceList = new();
    public readonly LockState locked = new();

    public void LoadBoard(Board board, Side perspective, bool playSound)
    {
        if (playSound)
            Sound.PlaySound("move");
        Clear();
        
        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
        {
            uint piece = board.GetPiece(file, rank);
                
            if (piece == Pieces.Empty)
                continue;
            (int x, int y) objectivePos = Invert.Switch((file, rank), perspective);
            Side pieceSide = (piece & Pieces.ColorMask) == 0 ? Side.White : Side.Black;
            
            MoveablePiece pieceObject = new MoveablePiece { Base = Base! , Source = GetPieceBitmap(board.GetPiece(file, rank)) };
            AddPiece(pieceObject, pieceSide, objectivePos);
        }
    }

    private void Clear()
    {
        Children.Clear();
        PieceList.Clear();
    }
    
    private class PieceItem(MoveablePiece piece, Side side, (int x, int y) pos)
    {
        public readonly MoveablePiece piece = piece;
        public (int x, int y) pos = pos;
        public Side side = side;
    }

    private void AddPiece(MoveablePiece piece, Side side, (int x, int y) at)
    {
        Children.Add(piece);
        SetColumn(piece, at.x);
        SetRow(piece, at.y);
        PieceList.Add(new PieceItem(piece, side, at));
    }

    public class LockState
    {
        private bool White;
        private bool Black;

        public void Lock()
        {
            White = true;
            Black = true;
        }

        public void Unlock()
        {
            White = false;
            Black = false;
        }

        public void Lock(Side side)
        {
            if (side == Side.White)
                White = true;
            else 
                Black = true;
        }

        public void Unlock(Side side)
        {
            if (side == Side.White)
                White = false;
            else
                Black = false;
        }

        public bool IsLocked(Side side)
        {
            return side == Side.White ? White : Black;
        }
    }
}