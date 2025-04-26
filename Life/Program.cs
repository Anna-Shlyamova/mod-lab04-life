using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using ScottPlot;
using ScottPlot.Plottables;

namespace cli_life
{
    public class Config
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
    }
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
    }
    class Program
    {
        static Board board;
        internal static void Reset(Config config)
        {
            board = new Board(
                width: config.Width,
                height: config.Height,
                cellSize: config.CellSize,
                liveDensity: config.LiveDensity);
        }
        
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        
        internal static Config LoadConfig(string path = "settings.json")
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json);
        }
        
        internal static void SaveState(string path)
        {
            using StreamWriter writer = new StreamWriter(path);
            for (int y = 0; y < board.Rows; y++)
            {
                for (int x = 0; x < board.Columns; x++)
                { 
                    writer.Write(board.Cells[x, y].IsAlive ? '1' : '0');
                }
                writer.WriteLine();
            }
        }
        

        internal static void LoadState(string path)
        {
            var lines = File.ReadAllLines(path);
            for (int y = 0; y < lines.Length && y < board.Rows; y++)
            {
                for (int x = 0; x < lines[y].Length && x < board.Columns; x++)
                {
                    board.Cells[x, y].IsAlive = lines[y][x] == '1';
                }
            }
        }

        static List<(int x, int y)> GetAliveCluster(bool[,] visited, int startX, int startY)
        {
            var cluster = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                cluster.Add((x, y));

                for (int i = 0; i < 8; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx >= 0 && nx < board.Columns && ny >= 0 && ny < board.Rows)
                    {
                        if (!visited[nx, ny] && board.Cells[nx, ny].IsAlive)
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            return cluster;
        }

        internal static void AnalyzeBoard()
        {
            bool[,] visited = new bool[board.Columns, board.Rows];
            var figureCounts = new Dictionary<string, int>();

            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    if (board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = GetAliveCluster(visited, x, y);
                        var normalized = FigureRecognizer.NormalizeCluster(cluster);
                        var matched = FigureRecognizer.Match(normalized);

                        if (matched != null)
                        {
                            if (!figureCounts.ContainsKey(matched.Name))
                                figureCounts[matched.Name] = 0;

                            figureCounts[matched.Name]++;
                        }
                    }
                }
            }

            Console.WriteLine("\n--- Обнаруженные фигуры ---");
            foreach (var kv in figureCounts.OrderBy(k => k.Key))
            {
                Console.WriteLine($"{kv.Key}: {kv.Value}");
            }
            Console.WriteLine("\n");
        }

        internal static void RunDensityExperiment()
        {
            Console.WriteLine("Запуск эксперимента. Построение графика");
            var config = LoadConfig();
            int width = config.Width;
            int height = config.Height;
            int cellSize = config.CellSize;
            int generations = 200;

            double[] densities = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };
            var plot = new Plot();
            var sb = new StringBuilder();

            foreach (var density in densities)
            {
                var board = new Board(width, height, cellSize, density);
                List<double> aliveCounts = new List<double>();

                sb.AppendLine($"Density: {density}");
                for (int gen = 0; gen < generations; gen++)
                {
                    int alive = 0;
                    foreach (var cell in board.Cells)
                        if (cell.IsAlive)
                            alive++;

                    aliveCounts.Add(alive);
                    sb.AppendLine($"{gen} {alive}");
                    board.Advance();
                }
                double[] xs = Enumerable.Range(0, generations).Select(i => (double)i).ToArray();
                double[] ys = aliveCounts.ToArray();

                var scatter = plot.Add.Scatter(xs, ys);
                scatter.Label = $"Density {density}";
            }

            plot.ShowLegend();
            plot.Legend.Location = Alignment.UpperRight;
            plot.Title("Живые клетки по поколениям");
            plot.XLabel("Поколение");
            plot.YLabel("Живые клетки");

            string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            string plotPath = Path.Combine(projectDir, "density_plot.png");
            string dataPath = Path.Combine(projectDir, "density_data.txt");
            
            plot.SavePng(plotPath, 800, 600);
            Console.WriteLine("График сохранен как density_plot.png");
            
            File.WriteAllText(dataPath, sb.ToString());
            Console.WriteLine("Данные сохранены в density_data.txt");
        }
        
        static void Main(string[] args)
        {
            string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            string statePath = Path.Combine(projectDir, "state.txt");
            var config = LoadConfig();
            Reset(config);
            
            if (File.Exists(statePath))
            {
                Console.WriteLine("Загрузить предыдущее состояние? (y/n)");
                if (Console.ReadKey(true).Key == ConsoleKey.Y)
                {
                    LoadState(statePath);
                }
            }

            while (true)
            {
                Render();
                AnalyzeBoard();
                board.Advance();
                Thread.Sleep(200);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.S)
                    {
                        SaveState(statePath);
                        Console.WriteLine("Состояние сохранено.");
                        Console.WriteLine("Продолжить? (y/n)");

                        if (Console.ReadKey(true).Key == ConsoleKey.N)
                        {
                            Console.WriteLine("Выход...");
                            break;
                        }
                    }
                    else if (key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Выход...");
                        break;
                    }
                }
            }
            RunDensityExperiment();

        }
    }
}