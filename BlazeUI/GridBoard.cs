using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;

public class GridBoard(Grid grid, Grid highlightGrid)
{
    public readonly Grid InnerGrid = grid;
    private readonly List<PieceItem> _pieces = new();
    private EmbeddedMatch? _match;
    private Side _side;
    
    private DispatcherTimer? _timer;

    private void StartPolling()
    {
        if (_match == null)
            return;
        
        LockAll(true);
        
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _match.WaitStartSearch();
        _timer.Tick += Poll;
        _timer.Start();
    }

    private void Poll(object? sender, EventArgs e)
    {
        if (_match!.Poll(out var node))
        {
            _timer!.Stop();
            LoadBoard(node.board, _side);
            LockAll(false);
            LockPieces((Side)(1 - (int)_side), true);
        }
    }
    
    public void MovePiece((int x, int y) from, (int x, int y) to)
    {
        int index = FindIndexOfPiece(from);
        if (index == -1)
            return;

        if (_match == null)
        {
            PieceItem piece = _pieces[index];
        
            RemovePiece(to, other => other.pos != from);
        
            Grid.SetColumn(piece.piece, to.x);
            Grid.SetRow(piece.piece, to.y);
            piece.pos = to;
            return;
        }
        
        if (_match.board.side != (int)_side)
            return;

        Move move = new Move(Move.GetSquare(PerspectiveConverter.Objective(from, _side)) + Move.GetSquare(PerspectiveConverter.Objective(to, _side)), _match.board);
        //Console.WriteLine(move.GetUCI());
        if (!_match.TryMake(move))
            return;
        LoadBoard(_match.board, _side);
        StartPolling();

    }

    private void AddPiece(MoveablePiece piece, (int x, int y) at)
    {
        InnerGrid.Children.Add(piece);
        Grid.SetColumn(piece, at.x);
        Grid.SetRow(piece, at.y);
        _pieces.Add(new PieceItem(piece, at));
    }

    private void LoadBoard(Board board, Side perspective)
    {
        Clear();

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                uint piece = board.GetPiece(file, rank);
                
                if (piece == Pieces.Empty)
                    continue;
                (int x, int y) objectivePos = PerspectiveConverter.Objective((file, rank), perspective);
                Side side = (piece & Pieces.ColorMask) == 0 ? Side.White : Side.Black;
                
                MoveablePiece pieceObject = new MoveablePiece { PieceGrid = this , Source = GetPieceBitmap(board.GetPiece(file, rank)) , Side = side };
                AddPiece(pieceObject, objectivePos);
            }
        }
    }

    public void SetMatch(EmbeddedMatch? match, Side perspective)
    {
        _side = perspective;
        _match = match;
        
        LoadBoard(match == null ? new(Presets.StartingBoard) : match.board, perspective);
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
        InnerGrid.Children.Clear();
        _pieces.Clear();
    }

    private void ClearHighlight()
    {
        highlightGrid.Children.Clear();
    }

    public void HighLight(ulong bitboard, Side perspective)
    {
        ClearHighlight();
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // objective coordinates on the bitboard
                (int x, int y) pos = (x, y);
                (int file, int rank) subjective = PerspectiveConverter.Objective(pos, perspective);

                if ((bitboard & BitboardUtils.GetSquare(pos)) != 0)
                {
                    // if the given square is highlighted
                    HighlightSingle(subjective);
                }
            }
        }
    }

    private void LockPieces(Side side, bool locked)
    {
        foreach (PieceItem piece in _pieces)
        {
            if (piece.piece.Side == side)
            {
                if (locked) piece.piece.Lock();
                else piece.piece.Unlock();
            }
        }
    }

    private void LockAll(bool locked)
    {
        foreach (PieceItem piece in _pieces)
        {
            if (locked) piece.piece.Lock();
            else piece.piece.Unlock();
        }
    }

    private void HighlightSingle((int file, int rank) pos)
    {
        Rectangle highlight = new Rectangle { [Shape.FillProperty] = Colors.HighLight };
        highlightGrid.Children.Add(highlight);
        Grid.SetColumn(highlight, pos.file);
        Grid.SetRow(highlight, pos.rank);
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
        string pieceFile = $"{((piece & Pieces.ColorMask) == 0 ? "white" : "black")}_{PieceName[piece & Pieces.TypeMask]}.png";
        //Console.WriteLine($"{Convert.ToString(piece, toBase:2).PadLeft(4, '0')} -> {pieceFile}");
        return new Bitmap(AbsolutePath + pieceFile);
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
    public static (int x, int y) Objective((int x, int y) objective, Side side)
    {
        return side != Side.White ? (7 - objective.x, objective.y) : (objective.x, 7 - objective.y);
    }
}