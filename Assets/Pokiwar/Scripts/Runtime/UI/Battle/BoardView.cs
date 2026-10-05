using System;
using System.Collections;
using System.Collections.Generic;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Replays board steps produced by the domain. It keeps its own id grid so it can animate from the old layout.</summary>
    public sealed class BoardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform GemRoot;
        public GemView GemPrefab;
        public RectTransform Selection;
        public float CellSize = 92f;
        public SpriteLibrary Sprites;

        public event Action<Pos, Pos> SwapRequested;
        public bool InputEnabled;

        private readonly Dictionary<int, GemView> byId = new Dictionary<int, GemView>();
        private readonly Stack<GemView> pool = new Stack<GemView>();
        private int[,] grid;
        private int width;
        private int height;
        private Pos? pressed;
        private Pos? selected;
        private bool dragFired;
        private Vector2 pressLocal;

        public void Rebuild(BoardState b)
        {
            foreach (var v in byId.Values) Release(v);
            byId.Clear();
            width = b.Width;
            height = b.Height;
            grid = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var c = b[x, y];
                    if (c.IsEmpty) continue;
                    var v = Acquire(c);
                    v.Rect.anchoredPosition = CellPos(x, y);
                    grid[x, y] = c.Id;
                }
            }
            ClearSelection();
        }

        public bool Matches(BoardState b)
        {
            if (grid == null || b.Width != width || b.Height != height) return false;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (grid[x, y] != b[x, y].Id) return false;
            return true;
        }

        public Vector2 CellPos(int x, float y) => new Vector2((x - (width - 1) * 0.5f) * CellSize, (y - (height - 1) * 0.5f) * CellSize);

        public RectTransform CellAnchorFor(Pos p) => byId.TryGetValue(grid[p.X, p.Y], out var v) ? v.Rect : GemRoot;

        public IEnumerator AnimateSwap(Pos a, Pos b, bool keep)
        {
            ClearSelection();
            byId.TryGetValue(grid[a.X, a.Y], out var va);
            byId.TryGetValue(grid[b.X, b.Y], out var vb);
            Vector2 pa = CellPos(a.X, a.Y), pb = CellPos(b.X, b.Y);
            yield return Tween.Run(0.16f, t =>
            {
                float e = Tween.EaseOut(t);
                if (va != null) va.Rect.anchoredPosition = Vector2.Lerp(pa, pb, e);
                if (vb != null) vb.Rect.anchoredPosition = Vector2.Lerp(pb, pa, e);
            });
            if (keep)
            {
                int t0 = grid[a.X, a.Y];
                grid[a.X, a.Y] = grid[b.X, b.Y];
                grid[b.X, b.Y] = t0;
                yield break;
            }
            yield return Tween.Run(0.16f, t =>
            {
                float e = Tween.EaseOut(t);
                if (va != null) va.Rect.anchoredPosition = Vector2.Lerp(pb, pa, e);
                if (vb != null) vb.Rect.anchoredPosition = Vector2.Lerp(pa, pb, e);
            });
        }

        public IEnumerator AnimateStep(CascadeStep step)
        {
            var dying = new List<GemView>();
            foreach (var cc in step.Cleared)
            {
                if (byId.TryGetValue(cc.Cell.Id, out var v)) dying.Add(v);
                byId.Remove(cc.Cell.Id);
                grid[cc.At.X, cc.At.Y] = 0;
            }
            yield return Tween.Run(0.18f, t =>
            {
                float s = 1f + 0.25f * Mathf.Sin(t * Mathf.PI) - t;
                foreach (var v in dying) v.transform.localScale = Vector3.one * Mathf.Max(0f, s);
            });
            foreach (var v in dying) Release(v);

            var moves = new List<(GemView view, Vector2 from, Vector2 to)>();
            foreach (var f in step.Falls)
            {
                if (!byId.TryGetValue(f.Id, out var v)) continue;
                moves.Add((v, CellPos(f.From.X, f.From.Y), CellPos(f.To.X, f.To.Y)));
            }
            foreach (var f in step.Falls)
            {
                if (grid[f.From.X, f.From.Y] == f.Id) grid[f.From.X, f.From.Y] = 0;
            }
            foreach (var f in step.Falls) grid[f.To.X, f.To.Y] = f.Id;
            foreach (var s in step.Spawns)
            {
                var v = Acquire(s.Cell);
                var from = CellPos(s.At.X, s.StartY);
                v.Rect.anchoredPosition = from;
                moves.Add((v, from, CellPos(s.At.X, s.At.Y)));
                grid[s.At.X, s.At.Y] = s.Cell.Id;
            }
            yield return Tween.Run(0.24f, t =>
            {
                float e = Tween.EaseIn(t);
                foreach (var m in moves) m.view.Rect.anchoredPosition = Vector2.LerpUnclamped(m.from, m.to, e);
            });
        }

        public IEnumerator AnimateResolution(SwapResolution res, BoardState finalBoard)
        {
            yield return AnimateSwap(res.Move.A, res.Move.B, res.Valid);
            if (!res.Valid) yield break;
            foreach (var step in res.Steps) yield return AnimateStep(step);
            if (res.Reshuffled || !Matches(finalBoard))
            {
                yield return Tween.Run(0.2f, t => GemRoot.localScale = Vector3.one * (1f - 0.1f * t));
                Rebuild(finalBoard);
                yield return Tween.Run(0.2f, t => GemRoot.localScale = Vector3.one * (0.9f + 0.1f * t));
            }
        }

        private GemView Acquire(Cell c)
        {
            var v = pool.Count > 0 ? pool.Pop() : Instantiate(GemPrefab, GemRoot);
            v.Rect.sizeDelta = new Vector2(CellSize - 6f, CellSize - 6f);
            v.Bind(c, Sprites != null ? Sprites.Gem(c.Type) : null);
            byId[c.Id] = v;
            return v;
        }

        private void Release(GemView v)
        {
            v.gameObject.SetActive(false);
            pool.Push(v);
        }

        private bool TryCell(PointerEventData e, out Pos p, out Vector2 local)
        {
            p = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(GemRoot, e.position, e.pressEventCamera, out local)) return false;
            int x = Mathf.FloorToInt(local.x / CellSize + width * 0.5f);
            int y = Mathf.FloorToInt(local.y / CellSize + height * 0.5f);
            p = new Pos(x, y);
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        public void OnPointerDown(PointerEventData e)
        {
            dragFired = false;
            pressed = null;
            if (!InputEnabled || grid == null) return;
            if (TryCell(e, out var p, out var local))
            {
                pressed = p;
                pressLocal = local;
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (!InputEnabled || dragFired || !pressed.HasValue) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(GemRoot, e.position, e.pressEventCamera, out var local)) return;
            var d = local - pressLocal;
            if (d.magnitude < CellSize * 0.4f) return;
            var a = pressed.Value;
            var b = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Pos(a.X + (d.x > 0 ? 1 : -1), a.Y) : new Pos(a.X, a.Y + (d.y > 0 ? 1 : -1));
            dragFired = true;
            if (b.X < 0 || b.Y < 0 || b.X >= width || b.Y >= height) return;
            ClearSelection();
            SwapRequested?.Invoke(a, b);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!InputEnabled || dragFired || !pressed.HasValue) return;
            var p = pressed.Value;
            pressed = null;
            if (selected.HasValue)
            {
                var s = selected.Value;
                if (s.IsAdjacent(p))
                {
                    ClearSelection();
                    SwapRequested?.Invoke(s, p);
                    return;
                }
                if (s == p)
                {
                    ClearSelection();
                    return;
                }
            }
            Select(p);
        }

        private void Select(Pos p)
        {
            selected = p;
            if (Selection == null) return;
            Selection.gameObject.SetActive(true);
            Selection.anchoredPosition = CellPos(p.X, p.Y);
            Selection.sizeDelta = new Vector2(CellSize, CellSize);
            Selection.SetAsLastSibling();
        }

        public void ClearSelection()
        {
            selected = null;
            if (Selection != null) Selection.gameObject.SetActive(false);
        }

        public void Hint(SwapMove m)
        {
            Select(m.A);
        }
    }
}
