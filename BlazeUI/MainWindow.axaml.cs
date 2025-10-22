using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Threading;
using BlazeUI.Blaze;

namespace BlazeUI;

public partial class MainWindow : Window
{
    private readonly GridBoard? _pieceBoard;
    private DispatcherTimer? _timer;
    
    public MainWindow()
    {
        InitializeComponent();

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
        _pieceBoard = new GridBoard(this.FindControl<Grid>("pieces")!, this.FindControl<Grid>("highlight")!);
        //PieceBoard.SetMatch(new(new (Presets.StartingBoard), 6), Side.White);
        _pieceBoard.SetMatch(null, Side.White);
        //PieceBoard.HighLight(new Board(Presets.StartingBoard).GetBitboard(0), Side.White);
    }
    
    private void StartNewGame(object sender, RoutedEventArgs e)
    {
        UninitializedWarning.Foreground = Brushes.White;
        UninitializedWarning.Text = "Initializing Blaze Engine...";
        
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        Bitboards.StartInit();
        _timer.Tick += Poll;
        _timer.Start();
    }

    private void Poll(object? sender, EventArgs e)
    {
        if (Bitboards.Poll())
        {
            _timer!.Stop();
            TopRow.Children.Remove(UninitializedWarning);
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