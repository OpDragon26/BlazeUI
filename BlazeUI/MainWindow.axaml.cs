using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using BlazeUI.Debug_Utils;

namespace BlazeUI;
using Blaze;
using Blaze.Board_Representation;
using Blaze.API;
using BotAPI;

public partial class MainWindow : Window
{
    private readonly PromotionHandler Promotion;
    private Side LastPlayed = Side.White;
    private readonly int Depth = 7;
    
    public MainWindow()
    {
        InitializeComponent();

        //DebugInterface.Execute();
        
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
        
        //Sound.Init();
        PopupHandlerGrid.Initialize();
        Promotion = new PromotionHandler(PromotionGrid);
        Promotion.InitImages(Side.White);
        PGNPanel.DisplayBoard = PieceBoard;
        PieceBoard.Initialize(Promotion, PGNPanel, this);
        
        InitProgress.Init(InitProgressBar);
        InitProgress.SetCompletion(0);
        
        StartNewGame();
    }
    
    private void NewGameOpenDropdown(object sender, RoutedEventArgs e)
    {
        PopupHandlerGrid.Toggle("NewGameDropdownPopup");
    }
    
    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        StartNewGame();
        PopupHandlerGrid.ClearActive();
    }
    
    private void StartNewGame()
    {
        PopupHandlerGrid.ClearActive();
        if (Init.init == Init.InitStatus.Complete)
            PieceBoard.SetMatch(new(new(Presets.StartingBoard), Depth, delayBook: true), LastPlayed);
        if (Init.init == Init.InitStatus.Waiting)
            return;
        
        PopupHandlerGrid.SetActive("InitPopup");
        InitBot.Initialize(() => {
            InitProgress.SetCompletion(Init.Progress.Percentage);
            InitStatus.Text = Init.Progress.Message;
        }, () => {
            PopupHandlerGrid.ClearActive();
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
        Promotion.Selected = name switch
        {
            "QueenPromotionButton" => 0b100,
            "RookPromotionButton" => 0b001,
            "KnightPromotionButton" => 0b010,
            "BishopPromotionButton" => 0b011,
            _ => throw new ArgumentOutOfRangeException()
        };
        Promotion.Cancel();
    }
    
    private void OnKeyDown(TopLevel t, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Promotion.Selected = 0b111;
                Promotion.Cancel();
                break;
            case Key.Right:
                PGNPanel.Slide(1);
                break;
            case Key.Left:
                PGNPanel.Slide(-1);
                break;
            case Key.Down:
                PGNPanel.Slide(2);
                break;
            case Key.Up:
                PGNPanel.Slide(-2);
                break;
        }
        
        base.OnKeyDown(e);
    }

    public void GameOverSplash(Outcome outcome, int moves)
    {
        PopupHandlerGrid.SetActive("GameOverPopup");
        GameOverTitle.Text = outcome switch
        {
            Outcome.Draw => "Game is a draw.",
            Outcome.WhiteWin => "White won!",
            Outcome.BlackWin => "Black won!",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        //Sound.PlaySound(General.SideWon(PieceBoard.PlayerSide, outcome) ? "game-won" : "game-lost");
        GameOverMoves.Text = $"moves: {moves}";
    }

    private void ClosePopup(object? sender, RoutedEventArgs e)
    {
        PopupHandlerGrid.ClearActive();
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