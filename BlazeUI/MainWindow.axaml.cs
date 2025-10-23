using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;

public partial class MainWindow : Window
{
    private readonly PromotionHandler _promotionHandler;
    private readonly GridBoard? _pieceBoard;
    private readonly OverlayHandler _overlay;
    private DispatcherTimer? _timer;
    private Side _lastPlayed = Side.White;
    
    public MainWindow()
    {
        InitializeComponent();

        // init overlay
        _overlay = new OverlayHandler(OverlayGrid);
        InitOverlays();
        
        InitProgress.Init(InitProgressBar);
        InitProgress.SetCompletion(0);
        
        // init board
        var BoardBackground = this.FindControl<UniformGrid>("board");
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                BoardBackground!.Children.Add(new Rectangle
                    { [Shape.FillProperty] = (file + rank) % 2 == 0 ? Colors.LightSquare : Colors.DarkSquare });
            }
        }
        
        // set up promotion handler
        _promotionHandler = new PromotionHandler(PromotionGrid);
        _promotionHandler.InitImages(Side.White);
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        
        // load a new game from starting position
        _pieceBoard = new GridBoard(this.FindControl<Grid>("pieces")!, this.FindControl<Grid>("highlight")!, _promotionHandler, new PGNDisplay(PGNPanel), this);
        _pieceBoard.SetMatch(null, Side.White);
        StartNewGame();
    }

    private void InitOverlays()
    {
        _overlay.AddOverlay(InitOverlay, "init");
        _overlay.AddOverlay(GameOverOverlay, "game-over");
        _overlay.AddOverlay(NewGameDropdownOverlay, "new-game");
        _overlay.Init();
    }
    
    private void NewGameOpenDropdown(object sender, RoutedEventArgs e)
    {
        _overlay.Toggle("new-game");
    }
    
    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        StartNewGame();
        _overlay.RemoveActive();
    }
    
    private void StartNewGame()
    {
        if (Bitboards.init)
            _pieceBoard!.SetMatch(new(new(Presets.StartingBoard), 6), _lastPlayed);
        if (Bitboards.begunInit)
            return;
        _overlay.SetActive("init");
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        Bitboards.StartInit();
        _timer.Tick += Poll;
        _timer.Start();
    }

    private void Poll(object? sender, EventArgs e)
    {
        InitProgress.SetCompletion(Bitboards.progress.percentage);
        InitStatus.Text = Bitboards.progress.message;
        
        if (Bitboards.Poll())
        {
            _timer!.Stop();
            _overlay.RemoveActive();
            _pieceBoard!.SetMatch(new(new(Presets.StartingBoard), 6), Side.White);
            //_pieceBoard!.SetMatch(new(new("8/7P/8/5K1k/8/8/8/8 w - - 0 1"), 6), Side.White);
        }
    }
    
    private void StartNewAsWhite(object sender, RoutedEventArgs e)
    {
        _overlay.RemoveActive();
        _lastPlayed = Side.White;
        StartNewGame();
    }
    private void StartAsNewBlack(object sender, RoutedEventArgs e)
    {
        _overlay.RemoveActive();
        _lastPlayed = Side.Black;
        StartNewGame();
    }

    private void PromotionSelected(object? sender, RoutedEventArgs e)
    {
        string name = (sender as Button)!.Name!;
        _promotionHandler._selected = name switch
        {
            "QueenPromotionButton" => 0b100,
            "RookPromotionButton" => 0b001,
            "KnightPromotionButton" => 0b010,
            "BishopPromotionButton" => 0b011,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private void OnKeyDown(TopLevel t, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            _pieceBoard!.CancelPromotion();
        
        base.OnKeyDown(e);
    }

    public void GameOverSplash(Outcome outcome, int moves)
    {
        _overlay.SetActive("game-over");
        GameOverTitle.Text = outcome switch
        {
            Outcome.Draw => "Game is a draw.",
            Outcome.WhiteWin => "White won!",
            Outcome.BlackWin => "Black won!",
            _ => throw new ArgumentOutOfRangeException()
        };
        GameOverMoves.Text = $"moves: {moves}";
    }

    private void ClosePopup(object? sender, RoutedEventArgs e)
    {
        _overlay.RemoveActive();
    }
}

public static class Colors
{
    public static readonly SolidColorBrush LightSquare = new(new Color(255, 238, 238, 210));
    public static readonly SolidColorBrush DarkSquare = new(new Color(255, 118, 150, 86));
    public static readonly SolidColorBrush HighLight =  new(new Color(128, 199, 24, 24));
}