using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BlazeUI;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush LightSquare = new(new Color(255, 238, 238, 210));
    private static readonly SolidColorBrush DarkSquare = new(new Color(255, 118, 150, 86));

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
                    { [Shape.FillProperty] = (file + rank) % 2 == 0 ? LightSquare : DarkSquare });
            }
        }

        // place pieces
        var Board = this.FindControl<Grid>("pieces");
        MoveableImage pawn = new MoveableImage { PieceGrid = Board, Source = new Bitmap("/home/opdragon25/Documents/CSharp/BlazeUI/BlazeUI/assets/pieces/white_pawn.png")};
        Grid.SetColumn(pawn, 3);
        Board!.Children.Add(pawn);
    }
}