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
        private int currentGridSize = 16;
        private double cellWidth;
        private double cellHeight;
        private Color currentColor = Colors.Black;
        private const int CanvasSize = 512; // Tamaño fijo del canvas

        public MainWindow()
        {
            InitializeComponent();
            InitializeGrid();
            DrawingCanvas.MouseLeftButtonDown += DrawingCanvas_MouseLeftButtonDown;
            DrawingCanvas.MouseMove += DrawingCanvas_MouseMove;
        }

        private void InitializeGrid()
        {
            DrawingCanvas.Width = CanvasSize;
            DrawingCanvas.Height = CanvasSize;
            RedrawGrid();
        }

        private void OpenSizeDialog_Click(object sender, RoutedEventArgs e)
        {
            // Crear ventana de diálogo
            var dialog = new Window
            {
                Title = "Cambiar tamaño de cuadrícula",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            // Crear contenido del diálogo
            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var textBlock = new TextBlock
            {
                Text = "Tamaño de la cuadrícula (1-256):",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox
            {
                Text = currentGridSize.ToString(),
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxLength = 3
            };

            var button = new Button
            {
                Content = "Aplicar",
                Width = 80,
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(5)
            };

            button.Click += (s, args) =>
            {
                if (int.TryParse(textBox.Text, out int size) && size > 0 && size <= 256)
                {
                    currentGridSize = size;
                    RedrawGrid();
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Por favor ingrese un número válido entre 1 y 256",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };

            // Manejar Enter en el TextBox
            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(button);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
            textBox.Focus();
            textBox.SelectAll();
        }

        private void RedrawGrid()
        {
            DrawingCanvas.Children.Clear();
            cellWidth = (double)CanvasSize / currentGridSize;
            cellHeight = (double)CanvasSize / currentGridSize;

            // Dibujar líneas de la cuadrícula
            for (int i = 0; i <= currentGridSize; i++)
            {
                // Líneas verticales
                var verticalLine = new Line
                {
                    X1 = i * cellWidth,
                    Y1 = 0,
                    X2 = i * cellWidth,
                    Y2 = CanvasSize,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                DrawingCanvas.Children.Add(verticalLine);

                // Líneas horizontales
                var horizontalLine = new Line
                {
                    X1 = 0,
                    Y1 = i * cellHeight,
                    X2 = CanvasSize,
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
            {
                DrawPixel(e.GetPosition(DrawingCanvas));
            }
        }

        private void DrawPixel(Point position)
        {
            int column = (int)(position.X / cellWidth);
            int row = (int)(position.Y / cellHeight);

            if (column < 0 || row < 0 || column >= currentGridSize || row >= currentGridSize)
                return;

            // Eliminar píxel existente si existe
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
            var pixel = new Rectangle
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
                        GridSize = currentGridSize
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