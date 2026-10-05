using System;
using System.Collections.Generic;
using System.Text;

namespace Pokiwar.Domain
{
    [Serializable]
    public struct Cell
    {
        public GemType Type;
        public int Multiplier;
        public int Id;

        public Cell(GemType type, int multiplier, int id)
        {
            Type = type;
            Multiplier = multiplier < 1 ? 1 : multiplier;
            Id = id;
        }

        public bool IsEmpty => Type == GemType.None;

        public static readonly Cell Empty = new Cell { Type = GemType.None, Multiplier = 0, Id = 0 };

        public override string ToString() => IsEmpty ? "." : Type + (Multiplier > 1 ? "x" + Multiplier : "") + "#" + Id;
    }

    public readonly struct Pos : IEquatable<Pos>
    {
        public readonly int X;
        public readonly int Y;

        public Pos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Pos o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Pos p && Equals(p);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(Pos a, Pos b) => a.Equals(b);
        public static bool operator !=(Pos a, Pos b) => !a.Equals(b);
        public override string ToString() => "(" + X + "," + Y + ")";

        public bool IsAdjacent(Pos o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y) == 1;
    }

    /// <summary>Shared 8x8 board. Y = 0 is the bottom row; gems fall toward Y = 0.</summary>
    public sealed class BoardState
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Cell[] cells;
        private int nextId;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new Cell[width * height];
            for (int i = 0; i < cells.Length; i++) cells[i] = Cell.Empty;
        }

        public Cell this[int x, int y]
        {
            get => cells[y * Width + x];
            set => cells[y * Width + x] = value;
        }

        public Cell this[Pos p]
        {
            get => this[p.X, p.Y];
            set => this[p.X, p.Y] = value;
        }

        public int CellCount => cells.Length;
        public int LastId => nextId;

        public bool InBounds(Pos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public int AllocateId() => ++nextId;

        public Cell NewCell(GemType type, int multiplier = 1) => new Cell(type, multiplier, AllocateId());

        public BoardState Clone()
        {
            var b = new BoardState(Width, Height) { nextId = nextId };
            Array.Copy(cells, b.cells, cells.Length);
            return b;
        }

        public void CopyFrom(BoardState other)
        {
            Array.Copy(other.cells, cells, cells.Length);
            nextId = other.nextId;
        }

        public int CountType(GemType t)
        {
            int n = 0;
            for (int i = 0; i < cells.Length; i++) if (cells[i].Type == t) n++;
            return n;
        }

        public bool SameLayout(BoardState other)
        {
            if (other.Width != Width || other.Height != Height) return false;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Type != other.cells[i].Type || cells[i].Multiplier != other.cells[i].Multiplier || cells[i].Id != other.cells[i].Id) return false;
            }
            return true;
        }

        /// <summary>Rows are written top row first. Letters: S Sword, L Lightning, F Fire, H Heart, D Shield, Y YinYang, '.' empty. A digit after a letter sets the multiplier.</summary>
        public static BoardState Parse(params string[] rowsTopFirst)
        {
            int h = rowsTopFirst.Length;
            var parsed = new List<List<(GemType, int)>>();
            int w = -1;
            foreach (var row in rowsTopFirst)
            {
                var list = new List<(GemType, int)>();
                for (int i = 0; i < row.Length; i++)
                {
                    char c = row[i];
                    if (c == ' ') continue;
                    GemType t = FromChar(c);
                    int mult = 1;
                    if (i + 1 < row.Length && char.IsDigit(row[i + 1]))
                    {
                        mult = row[i + 1] - '0';
                        i++;
                    }
                    list.Add((t, mult));
                }
                if (w < 0) w = list.Count;
                else if (w != list.Count) throw new ArgumentException("Board rows have different widths");
                parsed.Add(list);
            }
            var b = new BoardState(w, h);
            for (int r = 0; r < h; r++)
            {
                int y = h - 1 - r;
                for (int x = 0; x < w; x++)
                {
                    var (t, m) = parsed[r][x];
                    b[x, y] = t == GemType.None ? Cell.Empty : b.NewCell(t, m);
                }
            }
            return b;
        }

        public static GemType FromChar(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'S': return GemType.Sword;
                case 'L': return GemType.Lightning;
                case 'F': return GemType.Fire;
                case 'H': return GemType.Heart;
                case 'D': return GemType.Shield;
                case 'Y': return GemType.YinYang;
                default: return GemType.None;
            }
        }

        public static char ToChar(GemType t)
        {
            switch (t)
            {
                case GemType.Sword: return 'S';
                case GemType.Lightning: return 'L';
                case GemType.Fire: return 'F';
                case GemType.Heart: return 'H';
                case GemType.Shield: return 'D';
                case GemType.YinYang: return 'Y';
                default: return '.';
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    var c = this[x, y];
                    sb.Append(ToChar(c.Type));
                    if (c.Multiplier > 1) sb.Append(c.Multiplier);
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
