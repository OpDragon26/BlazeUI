using System.Collections.Generic;
using Avalonia.Controls;
using BlazeUI.Blaze;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using BlazeUI.Utils;
using static BlazeUI.Utils.BoardUIUtils;

namespace BlazeUI.Board_Interface;
public class PieceGrid : Grid
{
    public BoardUI? Base { get; set; }
    public readonly List<PieceItem> PieceList = new();
    public readonly LockState locked = new();

    public void LoadBoard(Board board, Side perspective, bool playSound, Move? lastMove = null)
    {
        if (playSound)
            Sound.PlaySound("move");
        Clear();
        
        if (lastMove is not null)
            Base!.Highlights.HighlightMove(lastMove);
        if (Init.init == Init.InitStatus.Complete)
            Base!.Highlights.HighlightCheck(board, perspective);
        
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
        
        locked.Lock();
    }

    public void LoadBoard(Move? lastMove = null)
    {
        LoadBoard(Base!.Match!.board, Base.PlayerSide, true, lastMove);
    }

    private void Clear()
    {
        Children.Clear();
        PieceList.Clear();
    }
    
    public class PieceItem(MoveablePiece piece, Side side, (int x, int y) pos)
    {
        public readonly MoveablePiece piece = piece;
        public readonly (int x, int y) pos = pos;
        public readonly Side side = side;
    }

    private void AddPiece(MoveablePiece piece, Side side, (int x, int y) at)
    {
        Children.Add(piece);
        SetColumn(piece, at.x);
        SetRow(piece, at.y);
        PieceList.Add(new PieceItem(piece, side, at));
    }
}