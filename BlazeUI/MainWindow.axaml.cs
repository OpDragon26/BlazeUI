using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BlazeUI;
using Blaze;
using Blaze.Board_Representation;
using Blaze.API;
using BotAPI;

public partial class MainWindow : Window
{
    private readonly PromotionHandler _promotionHandler;
    private readonly OverlayHandler OverlayHandler;
    private readonly PGNDisplay PgnDisplay;
    private Side LastPlayed = Side.White;
    private readonly int Depth = 3;
    
    public MainWindow()
    {
        InitializeComponent();

        Sound.Init();
        
        // init overlay
        OverlayHandler = new OverlayHandler(OverlayGrid);
        InitOverlays();
        
        InitProgress.Init(InitProgressBar);
        InitProgress.SetCompletion(0);
        
        // set up promotion handler
        _promotionHandler = new PromotionHandler(PromotionGrid);
        _promotionHandler.InitImages(Side.White);
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        
        PieceBoard.Initialize(_promotionHandler);
        
        // load a new game from starting position
        PgnDisplay = new PGNDisplay(PGNPanel, PieceBoard.GridBoard);
        
        //DebugInterface.Execute();
        
        StartNewGame();
    }

    private void InitOverlays()
    {
        OverlayHandler.AddOverlay(InitOverlay, "init");
        OverlayHandler.AddOverlay(GameOverOverlay, "game-over");
        OverlayHandler.AddOverlay(NewGameDropdownOverlay, "new-game");
        OverlayHandler.Init();
    }
    
    private void NewGameOpenDropdown(object sender, RoutedEventArgs e)
    {
        OverlayHandler.Toggle("new-game");
    }
    
    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        StartNewGame();
        OverlayHandler.RemoveActive();
    }
    
    private void StartNewGame()
    {
        OverlayHandler.RemoveActive();
        if (Init.init == Init.InitStatus.Complete)
            PieceBoard.SetMatch(new(new(Presets.StartingBoard), Depth, delayBook: true), LastPlayed);
        if (Init.init == Init.InitStatus.Waiting)
            return;
        
        OverlayHandler.SetActive("init");
        InitBot.Initialize(() => {
            InitProgress.SetCompletion(Init.Progress.Percentage);
            InitStatus.Text = Init.Progress.Message;
        }, () => {
            OverlayHandler.RemoveActive();
            PieceBoard.SetMatch(new(new(Presets.StartingBoard), Depth, delayBook: true), Side.White);
        });
    }
    
    private void StartNewAsWhite(object sender, RoutedEventArgs e)
    {
        LastPlayed = Side.White;
        StartNewGame();
    }
    private void StartAsNewBlack(object sender, RoutedEventArgs e)
    {
        LastPlayed = Side.Black;
        StartNewGame();
    }

    private void PromotionSelected(object? sender, RoutedEventArgs e)
    {
        string name = (sender as Button)!.Name!;
        _promotionHandler.Selected = name switch
        {
            "QueenPromotionButton" => 0b100,
            "RookPromotionButton" => 0b001,
            "KnightPromotionButton" => 0b010,
            "BishopPromotionButton" => 0b011,
            _ => throw new ArgumentOutOfRangeException()
        };
        _promotionHandler.Cancel();
    }
    
    private void OnKeyDown(TopLevel t, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _promotionHandler.Selected = 0b111;
                _promotionHandler.Cancel();
                break;
            case Key.Right:
                PgnDisplay.Slide(1);
                break;
            case Key.Left:
                PgnDisplay.Slide(-1);
                break;
            case Key.Down:
                PgnDisplay.Slide(2);
                break;
            case Key.Up:
                PgnDisplay.Slide(-2);
                break;
        }
        
        base.OnKeyDown(e);
    }

    public void GameOverSplash(Outcome outcome, int moves)
    {
        OverlayHandler.SetActive("game-over");
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
        OverlayHandler.RemoveActive();
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