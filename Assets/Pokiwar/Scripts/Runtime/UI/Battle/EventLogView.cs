using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Debug panel listing every CombatEvent with before/after values and modifiers.</summary>
    public sealed class EventLogView : MonoBehaviour
    {
        public GameObject Panel;
        public Text Content;
        public ScrollRect Scroll;
        public Button Toggle;
        public int MaxLines = 80;

        private readonly Queue<string> lines = new Queue<string>();

        private void Awake()
        {
            Toggle.onClick.AddListener(() =>
            {
                Panel.SetActive(!Panel.activeSelf);
                if (Panel.activeSelf) Refresh();
            });
            Panel.SetActive(false);
        }

        public void Clear()
        {
            lines.Clear();
            Content.text = "";
        }

        public void Add(string line)
        {
            lines.Enqueue(line);
            while (lines.Count > MaxLines) lines.Dequeue();
            if (!Panel.activeSelf) return;
            Refresh();
        }

        public void Refresh()
        {
            var sb = new StringBuilder();
            foreach (var l in lines) sb.AppendLine(l);
            Content.text = sb.ToString();
            Canvas.ForceUpdateCanvases();
            if (Scroll != null) Scroll.verticalNormalizedPosition = 0f;
        }
    }
}
