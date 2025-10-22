using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;

public partial class MainWindow : Window
{
    private readonly GridBoard? _pieceBoard;
    private readonly OverlayHandler? _overlay;
    private DispatcherTimer? _timer;
    
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
        
        // load match
        _pieceBoard = new GridBoard(this.FindControl<Grid>("pieces")!, this.FindControl<Grid>("highlight")!, this);
        _pieceBoard.SetMatch(null, Side.White);
        StartNewGame();
    }

    private void InitOverlays()
    {
        _overlay!.AddOverlay(InitOverlay, "init");
        _overlay!.Init();
    }

    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }
    
    private void StartNewGame()
    {
        if (Bitboards.init)
            _pieceBoard!.SetMatch(new(new(Presets.StartingBoard), 6), Side.White);
        if (Bitboards.begunInit)
            return;
        _overlay!.SetActive("init");
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
            _overlay!.RemoveActive();
            _pieceBoard!.SetMatch(new(new(Presets.StartingBoard), 6), Side.White);
        }
    }
}

public static class Colors
{
    public static readonly SolidColorBrush LightSquare = new(new Color(255, 238, 238, 210));
    public static readonly SolidColorBrush DarkSquare = new(new Color(255, 118, 150, 86));
    public static readonly SolidColorBrush HighLight =  new(new Color(128, 199, 24, 24));
}