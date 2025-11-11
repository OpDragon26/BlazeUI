using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BlazeUI.Blaze;
using Path = System.IO.Path;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Utils;
using BlazeUI.Blaze.Move_Generation;

namespace BlazeUI;

public class GridBoard(Grid grid, Grid highlightGrid, PromotionHandler promotionHandler, PGNDisplay pgnDisplay, TextBlock depthDisplay, TextBlock botMaterial, TextBlock? playerMaterial, MainWindow window)
{
    public readonly Grid InnerGrid = grid;
    private readonly List<PieceItem> _pieces = new();
    private EmbeddedMatch? _match;
    public Side side;
    private Outcome _outcome;
    
    private DispatcherTimer? _timer;

    private ((int file, int rank) from, (int file, int rank) to) _promotionSquare;
    private bool _expectPromotion;
    
    public void MovePiece((int x, int y) from, (int x, int y) to)
    {
        ClearHighlight("possible-moves");
        
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
        
        if (_match.board.side != (int)side)
            return;

        (int x, int y) invertedFrom = PerspectiveConverter.Invert(from, side);
        (int x, int y) invertedTo = PerspectiveConverter.Invert(to, side);
        
        // white attempted promotion
        if ((_match.board.GetPiece(invertedFrom) == Pieces.WhitePawn && invertedTo.y == 7) ||
            (_match.board.GetPiece(invertedFrom) == Pieces.BlackPawn && invertedTo.y == 0))
        {
            _promotionSquare = (invertedFrom, invertedTo);
            RequestPromotion();
            _expectPromotion = true;
            return;
        }
        
        TryMakeMove(MoveUtils.GetSquare(invertedFrom) + MoveUtils.GetSquare(invertedTo));
    }

    public void PieceRaised((int x, int y) pos)
    {
        HighLight(BitboardUtils.GetMoveBitboard(MoveGenerator.SearchBoard(_match!.board, false).ToArray().Where(move => move.Source == PerspectiveConverter.Invert(pos, side)).ToArray()), side, "possible-moves");
    }

    public void PieceReleased()
    {
        ClearHighlight("possible-moves");
    }

    private void TryMakeMove(string moveString)
    {
        if (IsGameOver())
            return;
        
        Move move = new Move(moveString, _match!.board);
        if (_match.TryMake(move))
        {
            ClearHighlight("last-move");
            HighlightMove(move, Colors.HighLightMove);
            pgnDisplay.Add(_match.game.LastMove!.Notate(), _match.game.LastMove.move!, new Board(_match!.board));
            LoadBoard(_match.board, side, true);
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
            Sound.PlaySound(General.SideWon(side, _outcome) ? "game-won" : "game-lost");
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
            HandleBotMove(node);
    }

    private void HandleBotMove(GameNode node)
    {
        depthDisplay.Text = $"depth {_match!.depth}";
        ClearHighlight("last-move");
        HighlightMove(node.move!, Colors.HighLightMove);
        pgnDisplay.Add(_match.game.LastMove!.Notate(), _match.game.LastMove.move!, new Board(_match!.board));
        _timer!.Stop();
        LoadBoard(_match.board, side, true);
        LockAll(true);
        LockPieces(side, false);
        IsGameOver();
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
            TryMakeMove(MoveUtils.GetSquare(_promotionSquare.from) + MoveUtils.GetSquare(_promotionSquare.to) + MoveUtils.PromotionStr[piece]);
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

    public void LoadBoard(Board board, Side perspective, bool playSound)
    {
        LoadBoard(board, perspective, playSound, _ => 0UL);
    }

    private void LoadBoard(Board board, Side perspective, bool playSound, Func<Board, ulong> highlighter)
    {
        Clear();
        UpdateMaterial(board, perspective);
        ClearHighlight("check");
        if (_match != null && MoveGenerator.Attacked(board.KingPositions[board.side], board, 1 - board.side))
            HighLight(BitboardUtils.GetSquare(board.KingPositions[board.side]), perspective, "check", Colors.HighLightCheck);
        
        ClearHighlight("specified");
        HighLight(highlighter(board), perspective, "specified", Colors.HighLight);
        
        if (playSound)
            Sound.PlaySound("move");
        
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                uint piece = board.GetPiece(file, rank);
                
                if (piece == Pieces.Empty)
                    continue;
                (int x, int y) objectivePos = PerspectiveConverter.Invert((file, rank), perspective);
                Side pieceSide = (piece & Pieces.ColorMask) == 0 ? Side.White : Side.Black;
                
                MoveablePiece pieceObject = new MoveablePiece { PieceGrid = this , Source = GetPieceBitmap(board.GetPiece(file, rank)) , Side = pieceSide };
                AddPiece(pieceObject, objectivePos);
            }
        }
    }

    private void UpdateMaterial(Board board, Side perspective)
    {
        General.MaterialComparison comparison = General.CompareMaterial(board);

        if (perspective == Side.White)
        {
            playerMaterial!.Text = comparison.GetWhiteString();
            botMaterial.Text = comparison.GetBlackString();
        }
        else
        {
            playerMaterial!.Text = comparison.GetBlackString();
            botMaterial.Text = comparison.GetWhiteString();
        }
    }

    public void LoadLatest()
    {
        LoadBoard(_match!.board, side, false);
    }

    public void SetMatch(EmbeddedMatch? match, Side perspective)
    {
        ClearHighlight();
        _timer?.Stop();
        pgnDisplay.Init(this);
        
        side = perspective;
        _match = match;
        
        LockAll(true);
        LockPieces(side, false);

        if (match != null)
        {
            depthDisplay.Text = $"depth {_match!.depth}";
            pgnDisplay.Clear();
            if (match.board.side == 1)
                pgnDisplay.Add("...", new Move((8,8),(8,8)), new Board("8/8/8/8/8/8/8/8 w - - 0 1"));
            if (perspective == Side.Black)
                StartPolling();
            LoadBoard(match.board, perspective, true);
            IsGameOver();
        }
        else
            LoadBoard(new(Presets.StartingBoard), perspective, false);
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
    
    public void ClearHighlight(string id)
    {
        highlightGrid.Children.RemoveAll(highlightGrid.Children.Where(item => (item as HighlightRectangle)!.ID.Equals(id)));
    }

    private void HighLight(ulong bitboard, Side perspective, string id)
    {
        HighLight(bitboard, perspective, id, Colors.HighLight);
    }
    
    private void HighLight(ulong bitboard, Side perspective, string id, SolidColorBrush color)
    {
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
                    HighlightSingle(subjective, id, color);
                }
            }
        }
    }

    public void HighlightMove(Move move, SolidColorBrush color)
    {
        ulong bitboard = BitboardUtils.GetSquare(move.Source) | BitboardUtils.GetSquare(move.Destination);
        HighLight(bitboard, side, "last-move", color);
    }

    private void LockPieces(Side sideToLock, bool locked)
    {
        foreach (PieceItem piece in _pieces)
        {
            if (piece.piece.Side == sideToLock)
            {
                if (locked) piece.piece.Lock();
                else piece.piece.Unlock();
            }
        }
    }

    public void LockAll(bool locked)
    {
        foreach (PieceItem piece in _pieces)
        {
            if (locked) piece.piece.Lock();
            else piece.piece.Unlock();
        }
    }

    private void HighlightSingle((int file, int rank) pos, string id, SolidColorBrush color)
    {
        Rectangle highlight = new HighlightRectangle { [Shape.FillProperty] = color , ID = id};
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
        return new Bitmap(Path.Combine("assets", "pieces", pieceFile));
    }
    
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

public class HighlightRectangle : Rectangle
{
    public required string ID;
}

static class PerspectiveConverter
{
    public static (int x, int y) Invert((int x, int y) objective, Side side)
    {
        return side != Side.White ? (7 - objective.x, objective.y) : (objective.x, 7 - objective.y);
    }
}