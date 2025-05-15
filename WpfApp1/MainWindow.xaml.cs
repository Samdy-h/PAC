using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace PixelArtEditor
{
    public partial class MainWindow : Window
    {
        private int currentGridSize = 8;
        private double cellWidth;
        private double cellHeight;
        private Color currentColor = Colors.Black;
        private int canvasWidth = 512;
        private int canvasHeight = 512;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGrid();

            GridSizeComboBox.SelectionChanged += GridSizeComboBox_SelectionChanged;
            DrawingCanvas.MouseLeftButtonDown += DrawingCanvas_MouseLeftButtonDown;
            DrawingCanvas.MouseMove += DrawingCanvas_MouseMove;
        }

        private void InitializeGrid()
        {
            UpdateCanvasSize();
        }

        private void UpdateCanvasSize()
        {
            if (int.TryParse(CanvasWidthTextBox.Text, out int width) &&
                int.TryParse(CanvasHeightTextBox.Text, out int height) &&
                width > 0 && height > 0)
            {
                canvasWidth = width;
                canvasHeight = height;

                DrawingCanvas.Width = canvasWidth;
                DrawingCanvas.Height = canvasHeight;
                CanvasBorder.Width = canvasWidth;
                CanvasBorder.Height = canvasHeight;

                RedrawGrid();
            }
        }

        private void GridSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridSizeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentGridSize = int.Parse(selectedItem.Tag.ToString());
                RedrawGrid();
            }
        }

        private void RedrawGrid()
        {
            DrawingCanvas.Children.Clear();

            // Calcular tamaño de celda para mantenerlas cuadradas
            cellWidth = (double)canvasWidth / currentGridSize;
            cellHeight = (double)canvasHeight / currentGridSize;

            // Dibujar líneas de la cuadrícula
            for (int i = 0; i <= currentGridSize; i++)
            {
                // Líneas verticales
                Line verticalLine = new Line
                {
                    X1 = i * cellWidth,
                    Y1 = 0,
                    X2 = i * cellWidth,
                    Y2 = canvasHeight,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                DrawingCanvas.Children.Add(verticalLine);

                // Líneas horizontales
                Line horizontalLine = new Line
                {
                    X1 = 0,
                    Y1 = i * cellHeight,
                    X2 = canvasWidth,
                    Y2 = i * cellHeight,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                DrawingCanvas.Children.Add(horizontalLine);
            }
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawPixel(e.GetPosition(DrawingCanvas));
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DrawPixel(e.GetPosition(DrawingCanvas));
        }

        private void DrawPixel(Point position)
        {
            int column = (int)(position.X / cellWidth);
            int row = (int)(position.Y / cellHeight);

            if (column < 0 || row < 0 || column >= currentGridSize || row >= currentGridSize)
                return;

            // Eliminar píxel existente en esta posición
            for (int i = DrawingCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (DrawingCanvas.Children[i] is Rectangle existingPixel &&
                    Canvas.GetLeft(existingPixel) == column * cellWidth &&
                    Canvas.GetTop(existingPixel) == row * cellHeight)
                {
                    DrawingCanvas.Children.RemoveAt(i);
                    break;
                }
            }

            // Crear nuevo píxel
            Rectangle pixel = new Rectangle
            {
                Width = cellWidth,
                Height = cellHeight,
                Fill = new SolidColorBrush(currentColor),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.2
            };

            Canvas.SetLeft(pixel, column * cellWidth);
            Canvas.SetTop(pixel, row * cellHeight);
            DrawingCanvas.Children.Add(pixel);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
            RedrawGrid();
        }

        private void ApplySizeButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCanvasSize();
        }
    }
}