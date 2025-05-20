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
using System.Text.RegularExpressions;

namespace PixelArtEditor
{
    public partial class MainWindow : Window
    {
        private int currentGridSize = 16;
        private double cellWidth;
        private double cellHeight;
        private const int CanvasSize = 512;

        // Clases para serialización
        public class SavedPixel
        {
            public int X { get; set; }
            public int Y { get; set; }
            public string Color { get; set; }
            public bool IsCursor { get; set; }
        }

        public class ProjectData
        {
            public int GridSize { get; set; }
            public int CanvasSize { get; set; }
            public List<SavedPixel> Pixels { get; set; }
            public string ConsoleContent { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeGrid();
            TextInput.Focus();
        }

        private void InitializeGrid()
        {
            DrawingCanvas.Width = CanvasSize;
            DrawingCanvas.Height = CanvasSize;
            RedrawGrid();
            AddToConsole("Sistema de comandos listo");
            AddToConsole("Comandos disponibles:");
            AddToConsole("Spawn(x,y) - Posiciona cursor");
            AddToConsole("Dibujar(x,y,color) - Ej: Dibujar(5,5,red)");
        }

        private void AddToConsole(string message)
        {
            TextDisplay.Text = $"{message}\n{TextDisplay.Text}";
            ConsoleScrollViewer.ScrollToTop();
        }

        private void TextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                ExecuteCommand();
            }
        }

        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            string command = TextInput.Text.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                AddToConsole($"> {command}");
                ProcessCommand(command);
                TextInput.Clear();
                TextInput.Focus();
            }
        }

        private void ProcessCommand(string command)
        {
            try
            {
                if (command.StartsWith("Spawn", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessSpawnCommand(command);
                }
                else if (command.StartsWith("Dibujar", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessDrawCommand(command);
                }
                else
                {
                    AddToConsole("Error: Comando no reconocido");
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"Error: {ex.Message}");
            }
        }

        private void ProcessSpawnCommand(string command)
        {
            var match = Regex.Match(command, @"Spawn\(\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);

                if (IsValidCoordinate(x, y))
                {
                    DrawCursor(x, y);
                    AddToConsole($"Cursor posicionado en ({x}, {y})");
                }
                else
                {
                    AddToConsole($"Error: Coordenadas deben estar entre 0 y {currentGridSize - 1}");
                }
            }
            else
            {
                AddToConsole("Error: Formato incorrecto. Use Spawn(x,y)");
            }
        }

        private void ProcessDrawCommand(string command)
        {
            var match = Regex.Match(command, @"Dibujar\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\w+)\s*\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                string colorName = match.Groups[3].Value.ToLower();

                Color color = colorName switch
                {
                    "red" => Colors.Red,
                    "blue" => Colors.Blue,
                    "green" => Colors.Green,
                    "black" => Colors.Black,
                    "white" => Colors.White,
                    _ => throw new Exception("Color no válido. Use: red, blue, green, black, white")
                };

                if (IsValidCoordinate(x, y))
                {
                    DrawPixel(x, y, color);
                    AddToConsole($"Pixel dibujado en ({x}, {y}) con color {colorName}");
                }
                else
                {
                    AddToConsole($"Error: Coordenadas deben estar entre 0 y {currentGridSize - 1}");
                }
            }
            else
            {
                AddToConsole("Error: Formato incorrecto. Use Dibujar(x,y,color)");
            }
        }

        private bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < currentGridSize && y >= 0 && y < currentGridSize;
        }

        private void DrawCursor(int x, int y)
        {
            RemoveElementAtPosition(x, y, "cursor");

            var cursor = new Rectangle
            {
                Width = cellWidth,
                Height = cellHeight,
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1,
                Tag = "cursor"
            };

            Canvas.SetLeft(cursor, x * cellWidth);
            Canvas.SetTop(cursor, y * cellHeight);
            DrawingCanvas.Children.Add(cursor);
        }

        private void DrawPixel(int x, int y, Color color)
        {
            RemoveElementAtPosition(x, y);

            var pixel = new Rectangle
            {
                Width = cellWidth,
                Height = cellHeight,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.2
            };

            Canvas.SetLeft(pixel, x * cellWidth);
            Canvas.SetTop(pixel, y * cellHeight);
            DrawingCanvas.Children.Add(pixel);
        }

        private void RemoveElementAtPosition(int x, int y, string tagFilter = null)
        {
            for (int i = DrawingCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (DrawingCanvas.Children[i] is FrameworkElement element &&
                    Canvas.GetLeft(element) == x * cellWidth &&
                    Canvas.GetTop(element) == y * cellHeight &&
                    (tagFilter == null || element.Tag?.ToString() == tagFilter))
                {
                    DrawingCanvas.Children.RemoveAt(i);
                }
            }
        }

        private void RedrawGrid()
        {
            DrawingCanvas.Children.Clear();
            cellWidth = (double)CanvasSize / currentGridSize;
            cellHeight = (double)CanvasSize / currentGridSize;

            for (int i = 0; i <= currentGridSize; i++)
            {
                DrawingCanvas.Children.Add(new Line
                {
                    X1 = i * cellWidth,
                    Y1 = 0,
                    X2 = i * cellWidth,
                    Y2 = CanvasSize,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });

                DrawingCanvas.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = i * cellHeight,
                    X2 = CanvasSize,
                    Y2 = i * cellHeight,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });
            }
        }

        private void OpenSizeDialog_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var dialog = new Window
            {
                Title = "Cambiar tamaño de cuadrícula",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var sizeTextBox = new TextBox
            {
                Text = currentGridSize.ToString(),
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var button = new Button
            {
                Content = "Aplicar",
                Width = 80,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            button.Click += (s, args) =>
            {
                if (int.TryParse(sizeTextBox.Text, out int size) && size > 0 && size <= 256)
                {
                    currentGridSize = size;
                    RedrawGrid();
                    AddToConsole($"Tamaño de cuadrícula cambiado a {size}x{size}");
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Ingrese un número entre 1 y 256", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock { Text = "Tamaño de cuadrícula (1-256):" },
                    sizeTextBox,
                    button
                }
            };

            dialog.ShowDialog();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DrawingCanvas.Children.Clear();
            RedrawGrid();
            AddToConsole("Canvas limpiado");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var saveDialog = new SaveFileDialog
            {
                Filter = "Pixel Art Files (*.pw)|*.pw",
                DefaultExt = ".pw"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var data = new ProjectData
                    {
                        GridSize = currentGridSize,
                        CanvasSize = CanvasSize,
                        Pixels = GetPixelsData(),
                        ConsoleContent = TextDisplay.Text
                    };

                    File.WriteAllText(saveDialog.FileName, JsonSerializer.Serialize(data));
                    AddToConsole($"Proyecto guardado: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    AddToConsole($"Error al guardar: {ex.Message}");
                }
            }
        }

        private List<SavedPixel> GetPixelsData()
        {
            var pixels = new List<SavedPixel>();
            foreach (var child in DrawingCanvas.Children)
            {
                if (child is Rectangle pixel && pixel.Fill is SolidColorBrush brush)
                {
                    pixels.Add(new SavedPixel
                    {
                        X = (int)(Canvas.GetLeft(pixel) / cellWidth),
                        Y = (int)(Canvas.GetTop(pixel) / cellHeight),
                        Color = brush.Color.ToString(),
                        IsCursor = pixel.Tag?.ToString() == "cursor"
                    });
                }
            }
            return pixels;
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var openDialog = new OpenFileDialog
            {
                Filter = "Pixel Art Files (*.pw)|*.pw"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var data = JsonSerializer.Deserialize<ProjectData>(json);

                    currentGridSize = data.GridSize;
                    TextDisplay.Text = data.ConsoleContent;
                    RedrawGrid();

                    foreach (var pixel in data.Pixels)
                    {
                        Color color = (Color)ColorConverter.ConvertFromString(pixel.Color);
                        if (pixel.IsCursor)
                        {
                            DrawCursor(pixel.X, pixel.Y);
                        }
                        else
                        {
                            DrawPixel(pixel.X, pixel.Y, color);
                        }
                    }

                    AddToConsole($"Proyecto cargado: {openDialog.FileName}");
                }
                catch (Exception ex)
                {
                    AddToConsole($"Error al cargar: {ex.Message}");
                }
            }
        }
    }
}