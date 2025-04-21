using System;
using System.Collections.Generic;
using System.Linq;

namespace cli_life
{
    public enum FigureType
    {
        StillLife,
        Oscillator,
        Spaceship,
        Unknown
    }

    public class KnownFigure
    {
        public int[,] Pattern { get; }
        public FigureType Type { get; }
        public string Name { get; }

        public KnownFigure(int[,] pattern, FigureType type, string name)
        {
            Pattern = pattern;
            Type = type;
            Name = name;
        }
    }

    public static class FigureRecognizer
    {
        public static List<KnownFigure> Figures = new List<KnownFigure>()
        {
            new KnownFigure(new int[,] {
                { 1, 1 },
                { 1, 1 }
            }, FigureType.StillLife, "Block"),

            new KnownFigure(new int[,] {
                { 0, 1, 1, 0 },
                { 1, 0, 0, 1 },
                { 0, 1, 1, 0 }
            }, FigureType.StillLife, "Beehive"),

            new KnownFigure(new int[,] {
                { 1, 1, 1 }
            }, FigureType.Oscillator, "Blinker"),

            new KnownFigure(new int[,] {
                { 0, 1, 0 },
                { 0, 0, 1 },
                { 1, 1, 1 }
            }, FigureType.Spaceship, "Glider")
        };

        public static int[,] NormalizeCluster(List<(int x, int y)> cluster)
        {
            int minX = cluster.Min(c => c.x);
            int minY = cluster.Min(c => c.y);
            int maxX = cluster.Max(c => c.x);
            int maxY = cluster.Max(c => c.y);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            int[,] result = new int[height, width];

            foreach (var (x, y) in cluster)
            {
                result[y - minY, x - minX] = 1;
            }

            return result;
        }

        public static KnownFigure Match(int[,] cluster)
        {
            foreach (var fig in Figures)
            {
                if (IsMatch(fig.Pattern, cluster))
                    return fig;
            }

            return null;
        }

        private static bool IsMatch(int[,] pattern, int[,] cluster)
        {
            if (pattern.GetLength(0) != cluster.GetLength(0) ||
                pattern.GetLength(1) != cluster.GetLength(1))
                return false;

            for (int y = 0; y < pattern.GetLength(0); y++)
            {
                for (int x = 0; x < pattern.GetLength(1); x++)
                {
                    if (pattern[y, x] != cluster[y, x])
                        return false;
                }
            }

            return true;
        }
    }
}