using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;

public class GridBoard(Grid grid, Grid highlightGrid, PromotionHandler promotionHandler, PGNDisplay pgnDisplay, MainWindow window)
{
    public readonly Grid InnerGrid = grid;
    private readonly List<PieceItem> _pieces = new();
    private EmbeddedMatch? _match;
    private Side _side;
    private Outcome _outcome;
    
    private DispatcherTimer? _timer;

    private ((int file, int rank) from, (int file, int rank) to) _promotionSquare;
    private bool _expectPromotion;
    
    public void MovePiece((int x, int y) from, (int x, int y) to)
    {
        HighLight(0, _side);
        
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

        (int x, int y) invertedFrom = PerspectiveConverter.Invert(from, _side);
        (int x, int y) invertedTo = PerspectiveConverter.Invert(to, _side);
        
        // white attempted promotion
        if ((_match.board.GetPiece(invertedFrom) == Pieces.WhitePawn && invertedTo.y == 7) ||
            (_match.board.GetPiece(invertedFrom) == Pieces.BlackPawn && invertedTo.y == 0))
        {
            _promotionSquare = (invertedFrom, invertedTo);
            RequestPromotion();
            _expectPromotion = true;
            return;
        }
        
        TryMakeMove(Move.GetSquare(invertedFrom) + Move.GetSquare(invertedTo));
    }

    public void PieceRaised((int x, int y) pos)
    {
        HighLight(BitboardUtils.GetMoveBitboard(Search.SearchBoard(_match!.board, false).ToArray().Where(move => move.Source == PerspectiveConverter.Invert(pos, _side)).ToArray()), _side);
    }

    private void TryMakeMove(string moveString)
    {
        if (IsGameOver())
            return;
        
        Move move = new Move(moveString, _match!.board);
        if (_match.TryMake(move))
        {
            pgnDisplay.Add(_match.NotateLastMove());
            LoadBoard(_match.board, _side);
            // Console.WriteLine("Made move " + moveString);
            StartPolling();
        }
    }

    private bool IsGameOver()
    {
        _outcome = _match!.GetOutcome();

        if (_outcome != Outcome.Ongoing)
        {
            window.GameOverSplash(_outcome, _match.game.Count / 2);
            LockAll(true);
            return true;
        }
        return false;
    }
    
    private void StartPolling()
    {
        if (_match == null || IsGameOver())
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
            pgnDisplay.Add(_match.NotateLastMove());
            _timer!.Stop();
            LoadBoard(node.board, _side);
            LockAll(true);
            LockPieces(_side, false);
            IsGameOver();
        }
    }

    private void RequestPromotion()
    {
        promotionHandler.RequestPromotion(_promotionSquare.to.file);
        
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += PollPromotion;
        _timer.Start();
    }

    private void PollPromotion(object? sender, EventArgs e)
    {
        if (promotionHandler._selected != 0b111 || !_expectPromotion)
        {
            //Console.WriteLine(promotionHandler._selected);
            _expectPromotion = false;
            _timer!.Stop();
            uint piece = promotionHandler._selected;
            promotionHandler.SendBack();
            TryMakeMove(Move.GetSquare(_promotionSquare.from) + Move.GetSquare(_promotionSquare.to) + Move.PromotionStr[piece]);
        }
    }

    public void CancelPromotion()
    {
        _expectPromotion = false;
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
                (int x, int y) objectivePos = PerspectiveConverter.Invert((file, rank), perspective);
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
        
        LockAll(true);
        LockPieces(_side, false);

        if (match != null)
        {
            pgnDisplay.Clear();
            if (match.board.side == 1)
                pgnDisplay.Add("...");
            if (perspective == Side.Black)
                StartPolling();
            LoadBoard(match.board, perspective);
            IsGameOver();
        }
        else
            LoadBoard(new(Presets.StartingBoard), perspective);
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

    private void HighLight(ulong bitboard, Side perspective)
    {
        ClearHighlight();
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // objective coordinates on the bitboard
                (int x, int y) pos = (x, y);
                (int file, int rank) subjective = PerspectiveConverter.Invert(pos, perspective);

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

    public static Bitmap GetPieceBitmap(uint piece)
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
    public static (int x, int y) Invert((int x, int y) objective, Side side)
    {
        return side != Side.White ? (7 - objective.x, objective.y) : (objective.x, 7 - objective.y);
    }
}