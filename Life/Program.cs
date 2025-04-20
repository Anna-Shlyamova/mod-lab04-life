using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using ScottPlot;

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
        static private void Reset(Config config)
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
        static Config LoadConfig(string path = "settings.json")
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json);
        }
        static void SaveState(string path)
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

        static void LoadState(string path)
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
        static void Main(string[] args)
        {

            var config = LoadConfig();
            Reset(config);
            
            if (File.Exists("state.txt"))
            {
                Console.WriteLine("Загрузить предыдущее состояние? (y/n)");
                if (Console.ReadKey(true).Key == ConsoleKey.Y)
                {
                    LoadState("state.txt");
                }
            }

            while (true)
            {
                Render();
                board.Advance();
                Thread.Sleep(200);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.S)
                    {
                        SaveState("state.txt");
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
            
        }
    }
}