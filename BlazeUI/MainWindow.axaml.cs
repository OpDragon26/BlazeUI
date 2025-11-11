using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;
using Blaze.Board_Representation;
using Blaze.API;
using Blaze.Magic_Lookup;
public partial class MainWindow : Window
{
    private readonly PromotionHandler _promotionHandler;
    private readonly OverlayHandler _overlay;
    private readonly PGNDisplay _pgnDisplay;
    private DispatcherTimer? _timer;
    private Side _lastPlayed = Side.White;
    private readonly int _depth = 7;
    
    public MainWindow()
    {
        InitializeComponent();

        Sound.Init();
        
        // init overlay
        _overlay = new OverlayHandler(OverlayGrid);
        InitOverlays();
        
        InitProgress.Init(InitProgressBar);
        InitProgress.SetCompletion(0);
        
        // set up promotion handler
        _promotionHandler = new PromotionHandler(PromotionGrid);
        _promotionHandler.InitImages(Side.White);
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        
        PieceBoard.Initialize(_promotionHandler);
        
        // load a new game from starting position
        _pgnDisplay = new PGNDisplay(PGNPanel, PieceBoard.GridBoard);
        
        //DebugInterface.Execute();
        
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
            PieceBoard.SetMatch(new(new(Presets.StartingBoard), _depth), _lastPlayed);
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
        InitProgress.SetCompletion(Init.Progress.Percentage);
        InitStatus.Text = Init.Progress.Message;
        
        if (Bitboards.Poll())
        {
            _timer!.Stop();
            _overlay.RemoveActive();
            PieceBoard.SetMatch(new EmbeddedMatch(new Board(Presets.StartingBoard), _depth, delayBook: true), Side.White);
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
        switch (e.Key)
        {
            case Key.Escape:
                //PieceBoard.CancelPromotion();
                //PieceBoard.LoadLatest();
                break;
            case Key.Right:
                _pgnDisplay.Slide(1);
                break;
            case Key.Left:
                _pgnDisplay.Slide(-1);
                break;
            case Key.Down:
                _pgnDisplay.Slide(2);
                break;
            case Key.Up:
                _pgnDisplay.Slide(-2);
                break;
        }
        
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
    public static readonly SolidColorBrush HighLight =  new(new Color(192, 199, 24, 24));
    public static readonly SolidColorBrush HighLightMove =  new(new Color(192, 222, 237, 59));
    public static readonly SolidColorBrush HighLightCheck =  new(new Color(255, 216, 0, 0));
}