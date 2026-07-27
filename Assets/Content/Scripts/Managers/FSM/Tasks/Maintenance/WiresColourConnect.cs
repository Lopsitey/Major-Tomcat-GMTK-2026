#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    /// <summary>
    ///     Electrical maintenance: a cat chewed the loom apart. Click a port on the left, then the
    ///     port on the right sharing its colour. From the GMTK Miro board ("wires colour connect").
    /// </summary>
    public sealed class WiresColourConnect : TaskBase
    {
        // Palette index doubles as the pairing key, so the colours must stay distinct
        private static readonly Color[] WireColours =
        {
            new(0.92f, 0.29f, 0.29f), // red
            new(0.35f, 0.72f, 1f), // blue
            new(0.45f, 0.85f, 0.42f), // green
            new(0.98f, 0.80f, 0.28f) // yellow
        };

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_LeftPorts = new();
        private readonly List<VisualElement> m_RightPorts = new();
        private readonly List<int> m_LeftColourIds = new();
        private readonly List<int> m_RightColourIds = new();
        private readonly HashSet<int> m_ConnectedColourIds = new();

        private int m_SelectedLeftIndex = -1;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("WiresColourConnect: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_LeftPorts.Clear();
            m_RightPorts.Clear();
            m_LeftColourIds.Clear();
            m_RightColourIds.Clear();
            m_ConnectedColourIds.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_SelectedLeftIndex = -1;
        }

        private void BindUI(VisualElement root)
        {
            m_LeftPorts.Clear();
            m_RightPorts.Clear();
            m_LeftColourIds.Clear();
            m_RightColourIds.Clear();
            m_ConnectedColourIds.Clear();
            m_SelectedLeftIndex = -1;

            m_Status = root.Q<Label>("wire-status");
            root.Query<VisualElement>(className: "wire-port-left").ToList(m_LeftPorts);
            root.Query<VisualElement>(className: "wire-port-right").ToList(m_RightPorts);

            if (m_LeftPorts.Count == 0 || m_LeftPorts.Count != m_RightPorts.Count)
            {
                Debug.LogError(
                    $"WiresColourConnect: Port counts must match and be non-zero (left {m_LeftPorts.Count}, right {m_RightPorts.Count}).");
                return;
            }

            var pairCount = Mathf.Min(m_LeftPorts.Count, WireColours.Length);

            // Left column keeps palette order; the right column is shuffled every open
            var shuffled = new List<int>();
            for (var i = 0; i < pairCount; i++)
            {
                m_LeftColourIds.Add(i);
                shuffled.Add(i);
            }

            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var swap = Random.Range(0, i + 1);
                (shuffled[i], shuffled[swap]) = (shuffled[swap], shuffled[i]);
            }

            m_RightColourIds.AddRange(shuffled);

            for (var i = 0; i < pairCount; i++)
            {
                var leftIndex = i;
                var rightIndex = i;

                StylePort(m_LeftPorts[i], WireColours[m_LeftColourIds[i]]);
                StylePort(m_RightPorts[i], WireColours[m_RightColourIds[i]]);

                m_LeftPorts[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    SelectLeftPort(leftIndex);
                });

                m_RightPorts[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    TryConnect(rightIndex);
                });
            }

            UpdateStatus();
        }

        private static void StylePort(VisualElement port, Color colour)
        {
            port.style.backgroundColor = new StyleColor(colour);
            port.style.opacity = 1f;
            port.pickingMode = PickingMode.Position;
            port.RemoveFromClassList("wire-selected");
            port.RemoveFromClassList("wire-connected");
        }

        private void SelectLeftPort(int index)
        {
            if (IsCompleting || index < 0 || index >= m_LeftPorts.Count)
                return;

            // Already wired up — nothing to re-select
            if (m_ConnectedColourIds.Contains(m_LeftColourIds[index]))
                return;

            if (m_SelectedLeftIndex >= 0 && m_SelectedLeftIndex < m_LeftPorts.Count)
                m_LeftPorts[m_SelectedLeftIndex].RemoveFromClassList("wire-selected");

            m_SelectedLeftIndex = index;
            m_LeftPorts[index].AddToClassList("wire-selected");
            UpdateStatus();
        }

        private void TryConnect(int rightIndex)
        {
            if (IsCompleting || rightIndex < 0 || rightIndex >= m_RightPorts.Count)
                return;

            if (m_SelectedLeftIndex < 0)
                return;

            var wantedColourId = m_LeftColourIds[m_SelectedLeftIndex];

            if (m_ConnectedColourIds.Contains(m_RightColourIds[rightIndex]))
                return;

            if (m_RightColourIds[rightIndex] != wantedColourId)
            {
                // Wrong colour — drop the selection so the player starts the pair again
                m_LeftPorts[m_SelectedLeftIndex].RemoveFromClassList("wire-selected");
                m_SelectedLeftIndex = -1;
                UpdateStatus();
                return;
            }

            var leftPort = m_LeftPorts[m_SelectedLeftIndex];
            leftPort.RemoveFromClassList("wire-selected");
            leftPort.AddToClassList("wire-connected");
            leftPort.pickingMode = PickingMode.Ignore;

            var rightPort = m_RightPorts[rightIndex];
            rightPort.AddToClassList("wire-connected");
            rightPort.pickingMode = PickingMode.Ignore;

            m_ConnectedColourIds.Add(wantedColourId);
            m_SelectedLeftIndex = -1;
            UpdateStatus();

            if (m_ConnectedColourIds.Count >= m_LeftColourIds.Count)
                HandleWinSequence();
        }

        private void UpdateStatus()
        {
            if (m_Status == null)
                return;

            if (m_SelectedLeftIndex >= 0)
                m_Status.text = "Now pick the matching colour on the right";
            else
                m_Status.text = $"Wires left: {m_LeftColourIds.Count - m_ConnectedColourIds.Count}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[WiresColourConnect] LOOM REPAIRED! Delaying close...</color>");

            foreach (var port in m_LeftPorts)
                port.pickingMode = PickingMode.Ignore;

            foreach (var port in m_RightPorts)
                port.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Power restored!";

            ScheduleCompletion();
        }
    }
}