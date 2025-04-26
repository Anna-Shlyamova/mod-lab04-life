using cli_life;

namespace LifeTests;

public class LifeUnitTests
{
    public class FigureRecognizerTests
    {
        [Fact]
        public void NormalizeCluster_FormsCorrectMatrix()
        {
            var cluster = new List<(int, int)> { (2, 3), (2, 4), (3, 3), (3, 4) };
            var result = FigureRecognizer.NormalizeCluster(cluster);

            int[,] expected = { {1, 1}, {1, 1} };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Match_KnownBlockFigure()
        {
            int[,] block = { {1, 1}, {1, 1} };
            var result = FigureRecognizer.Match(block);
            Assert.NotNull(result);
            Assert.Equal("Block", result.Name);
        }

        [Fact]
        public void Match_KnownBeehiveFigure()
        {
            int[,] beehive = {
                {0,1,1,0},
                {1,0,0,1},
                {0,1,1,0}
            };
            var result = FigureRecognizer.Match(beehive);
            Assert.NotNull(result);
            Assert.Equal("Beehive", result.Name);
        }

        [Fact]
        public void Match_KnownBlinkerFigure()
        {
            int[,] blinker = { {1, 1, 1} };
            var result = FigureRecognizer.Match(blinker);
            Assert.NotNull(result);
            Assert.Equal("Blinker", result.Name);
        }

        [Fact]
        public void Match_KnownGliderFigure()
        {
            int[,] glider = {
                {0,1,0},
                {0,0,1},
                {1,1,1}
            };
            var result = FigureRecognizer.Match(glider);
            Assert.NotNull(result);
            Assert.Equal("Glider", result.Name);
        }

        [Fact]
        public void Match_UnknownFigureReturnsNull()
        {
            int[,] unknown = { {1,0},{0,1} };
            var result = FigureRecognizer.Match(unknown);
            Assert.Null(result);
        }
    }
    
    public class BoardTests
    {
        [Fact]
        public void Board_CorrectDimensions()
        {
            var board = new Board(100, 100, 10);
            Assert.Equal(10, board.Columns);
            Assert.Equal(10, board.Rows);
        }

        [Fact]
        public void Board_AllCellsAlive_WhenDensityOne()
        {
            var board = new Board(100, 100, 10, 1.0);
            foreach (var cell in board.Cells)
                Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Board_NoCellsAlive_WhenDensityZero()
        {
            var board = new Board(100, 100, 10, 0.0);
            foreach (var cell in board.Cells)
                Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Board_CellsHaveCorrectNeighborsCount()
        {
            var board = new Board(100, 100, 10);
            foreach (var cell in board.Cells)
                Assert.Equal(8, cell.neighbors.Count);
        }
    }
    
    public class CellTests
    {
        [Fact]
        public void Cell_BornWithThreeLiveNeighbors()
        {
            var cell = new Cell();
            cell.neighbors.AddRange(new[] { new Cell { IsAlive = true }, new Cell { IsAlive = true }, new Cell { IsAlive = true } });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Cell_DiesWithMoreThanThreeNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_SurvivesWithTwoOrThreeNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(new[] { new Cell { IsAlive = true }, new Cell { IsAlive = true } });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }
        
        [Fact]
        public void Cell_StaysDeadWithNoLiveNeighbors()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(new[] { new Cell(), new Cell(), new Cell() });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_DiesWithLessThanTwoNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }
    }
    
    public class ProgramTests
    {
        [Fact]
        public void LoadConfig_ReturnsNonNullConfig()
        {
            var config = Program.LoadConfig();
            Assert.NotNull(config);
        }

        [Fact]
        public void AnalyzeBoard_DoesNotThrow()
        {
            var config = Program.LoadConfig();
            Program.Reset(config);
            var ex = Record.Exception(() => Program.AnalyzeBoard());
            Assert.Null(ex);
        }

        [Fact]
        public void RunDensityExperiment_ReturnsValidDensity()
        {
            var plotPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "density_plot.png"));
            var dataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "density_data.txt"));
    
            if (File.Exists(plotPath)) File.Delete(plotPath);
            if (File.Exists(dataPath)) File.Delete(dataPath);

            Program.RunDensityExperiment();

            Assert.True(File.Exists(plotPath), "Файл графика density_plot.png должен быть создан.");
            Assert.True(File.Exists(dataPath), "Файл данных density_data.txt должен быть создан.");

            var plotInfo = new FileInfo(plotPath);
            var dataInfo = new FileInfo(dataPath);

            Assert.True(plotInfo.Length > 0, "Файл графика не должен быть пустым.");
            Assert.True(dataInfo.Length > 0, "Файл данных не должен быть пустым.");

            if (File.Exists(plotPath)) File.Delete(plotPath);
            if (File.Exists(dataPath)) File.Delete(dataPath);
        }
    }
}