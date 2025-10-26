using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia;

namespace RussianCheckers;

// partial нужен, тк XAML генерирует часть класса автоматически
public partial class MainWindow : Window
{
    const int size = 8; 
    Button[,] buttons = new Button[size, size]; 
    Board board = new(); 
    (int row, int col)? selectedCell; 
    List<(int row, int col)> possibleMoves = new(); 

    public MainWindow()
    {
        InitializeComponent();
        BuildGrid(); 
        Draw();      
    }

    void BuildGrid()
    {
        // очищаем доску
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Children.Clear();

        // делаем доску квадратной
        for (int i = 0; i < size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                var button = new Button
                {
                    Background = ((row + col) % 2 == 0) ? Brushes.GhostWhite : Brushes.Teal,

                    BorderBrush = Brushes.LightCyan,
                    BorderThickness = new Avalonia.Thickness(0.5),

                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,

                    MinWidth = 50,
                    MinHeight = 50,

                    Padding = new Avalonia.Thickness(0)
                };

                /// <summary> 
                /// запоминаем столбец и строку для лямбы(тк при вызове она будет смотреть на текущее значение)
                /// при клике вызывается событие с sender(кто вызвал событие(наш button)) и RoutedEventHandler(аргументы события), 
                /// мы должны их обработать через лямбду и вызвать OnClick 
                /// </summary>
                int remRow = row, remCol = col; 
                button.Click += (_, _) => OnClick(remRow, remCol);

                // добавляем клетку в сетку
                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);

                buttons[row, col] = button;
                BoardGrid.Children.Add(button);
            }

        // делаем доску квадратной при изменении окна
        this.SizeChanged += (_, _) =>
        {
            double size = Math.Min(this.Bounds.Width, this.Bounds.Height);
            BoardGrid.Width = size;
            BoardGrid.Height = size;
        };
    }

    void OnClick(int row, int col)
    {
        // если среди possibleMoves есть (row, col)
        if (selectedCell.HasValue && possibleMoves.Any(cell => cell == (row, col)))
        {
            var (srcRow, srcCol) = selectedCell.Value;

            if (board.TryMove(srcRow, srcCol, row, col))
            {
                selectedCell = null;
                possibleMoves.Clear();
                Draw();

                if (board.CheckWin(out var winner))
                    ShowMessage(winner == Board.Player.White ? "White won!" : "Black won!");
            }
            else
            {
                selectedCell = null;
                possibleMoves.Clear();
                Draw();
            }
            return;
        }

        var piece = board[row, col];
        if (piece != null && piece.Owner == board.CurrentPlayer)
        {
            selectedCell = (row, col);
            possibleMoves = board.GetMovesForPiece(row, col);
            Draw();
        }
        else
        {
            selectedCell = null;
            possibleMoves.Clear();
            Draw();
        }
    }

    async void ShowMessage(string text)
    {
        var dialog = new Window
        {
            Width = 200,
            Height = 100,
            Content = new TextBlock
            {
                Text = text,

                FontSize = 20,

                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };
        // нужен чтобы программа не "замораживалась" при открытии сообщения(для await и нужен async)
        await dialog.ShowDialog(this); 
    }

    void Draw()
    {
        double cellsize = BoardGrid.Bounds.Width / size;

        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
            {
                var button = buttons[row, col];
                
                button.Background = ((row + col) % 2 == 0) ? Brushes.GhostWhite : Brushes.Teal;
                button.Content = null;

                if (selectedCell == (row, col))
                    button.Background = Brushes.MediumAquamarine;

                if (possibleMoves.Any(cell => cell == (row, col)))
                {
                    button.Content = new Ellipse
                    {
                        // полупрозрачный серый круг
                        Fill = Brushes.LightGray,

                        Width = cellsize / 4,
                        Height = cellsize / 4,

                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                }

                var checker = board[row, col];
                if (checker != null)
                {
                    var ellipse = new Ellipse
                    {
                        Fill = checker.Owner == Board.Player.White ? Brushes.White : Brushes.Black,
                        Stroke = checker.IsKing ? Brushes.Gold : Brushes.Gray,

                        StrokeThickness = 2,
                        Width = cellsize * 0.8,
                        Height = cellsize * 0.8,

                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    
                    button.Content = ellipse;
                }
            }
    }
}
