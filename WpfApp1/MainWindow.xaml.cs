using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace PixelArtEditor
{
    public class PixelArtData
    {
        public int GridSize { get; set; }
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }
        public List<PixelInfo> Pixels { get; set; } = new List<PixelInfo>();
    }

    public class PixelInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte[] ColorBytes { get; set; }
    }

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

            cellWidth = (double)canvasWidth / currentGridSize;
            cellHeight = (double)canvasHeight / currentGridSize;

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

            // Eliminar píxel existente
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Pixel Art Files (*.pw)|*.pw",
                DefaultExt = ".pw"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PixelArtData data = new PixelArtData
                    {
                        GridSize = currentGridSize,
                        CanvasWidth = canvasWidth,
                        CanvasHeight = canvasHeight
                    };

                    foreach (var child in DrawingCanvas.Children)
                    {
                        if (child is Rectangle pixel && pixel.Fill is SolidColorBrush brush)
                        {
                            double left = Canvas.GetLeft(pixel);
                            double top = Canvas.GetTop(pixel);

                            data.Pixels.Add(new PixelInfo
                            {
                                X = (int)(left / cellWidth),
                                Y = (int)(top / cellHeight),
                                ColorBytes = new byte[] { brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A }
                            });
                        }
                    }

                    string json = JsonSerializer.Serialize(data);
                    File.WriteAllText(saveFileDialog.FileName, json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Pixel Art Files (*.pw)|*.pw"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    PixelArtData data = JsonSerializer.Deserialize<PixelArtData>(json);

                    // Actualizar UI
                    currentGridSize = data.GridSize;
                    canvasWidth = data.CanvasWidth;
                    canvasHeight = data.CanvasHeight;

                    CanvasWidthTextBox.Text = data.CanvasWidth.ToString();
                    CanvasHeightTextBox.Text = data.CanvasHeight.ToString();

                    // Redibujar grid
                    RedrawGrid();

                    // Dibujar píxeles
                    foreach (var pixelInfo in data.Pixels)
                    {
                        Rectangle pixel = new Rectangle
                        {
                            Width = cellWidth,
                            Height = cellHeight,
                            Fill = new SolidColorBrush(Color.FromArgb(
                                pixelInfo.ColorBytes[3],
                                pixelInfo.ColorBytes[0],
                                pixelInfo.ColorBytes[1],
                                pixelInfo.ColorBytes[2])),
                            Stroke = Brushes.Gray,
                            StrokeThickness = 0.2
                        };

                        Canvas.SetLeft(pixel, pixelInfo.X * cellWidth);
                        Canvas.SetTop(pixel, pixelInfo.Y * cellHeight);
                        DrawingCanvas.Children.Add(pixel);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}