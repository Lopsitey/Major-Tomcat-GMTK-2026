#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Cockpit hazard: the treat jar shattered across the console. Click two shards to swap
    ///     them until the numbers run left to right. From the GMTK Miro board ("treat jar jigsaw").
    /// </summary>
    public sealed class TreatJarJigsaw : TaskBase
    {
        private static readonly Color[] ShardColours =
        {
            new(0.94f, 0.62f, 0.35f),
            new(0.86f, 0.45f, 0.55f),
            new(0.55f, 0.72f, 0.95f),
            new(0.62f, 0.86f, 0.55f),
            new(0.90f, 0.84f, 0.45f)
        };

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<Label> m_Shards = new();
        private readonly List<int> m_ShardValues = new();

        private int m_SelectedSlot = -1;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("TreatJarJigsaw: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Shards.Clear();
            m_ShardValues.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_SelectedSlot = -1;
        }

        private void BindUI(VisualElement root)
        {
            m_Shards.Clear();
            m_ShardValues.Clear();
            m_SelectedSlot = -1;

            m_Status = root.Q<Label>("jigsaw-status");
            root.Query<Label>(className: "jigsaw-shard").ToList(m_Shards);

            if (m_Shards.Count < 2)
            {
                Debug.LogError("TreatJarJigsaw: Need at least two '.jigsaw-shard' elements in UXML.");
                return;
            }

            for (var i = 0; i < m_Shards.Count; i++)
                m_ShardValues.Add(i);

            Scramble();

            for (var i = 0; i < m_Shards.Count; i++)
            {
                var slot = i;
                m_Shards[i].pickingMode = PickingMode.Position;
                m_Shards[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    SelectSlot(slot);
                });

                ApplyShardVisual(i);
            }

            UpdateStatus();
        }

        /// <summary>
        ///     Shuffles the shards, re-rolling on the off chance the scramble solved the puzzle.
        /// </summary>
        private void Scramble()
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                for (var i = m_ShardValues.Count - 1; i > 0; i--)
                {
                    var swap = Random.Range(0, i + 1);
                    (m_ShardValues[i], m_ShardValues[swap]) = (m_ShardValues[swap], m_ShardValues[i]);
                }

                if (!IsSolved())
                    return;
            }
        }

        private void SelectSlot(int slot)
        {
            if (IsCompleting || slot < 0 || slot >= m_Shards.Count)
                return;

            if (m_SelectedSlot < 0)
            {
                m_SelectedSlot = slot;
                m_Shards[slot].AddToClassList("jigsaw-selected");
                UpdateStatus();
                return;
            }

            m_Shards[m_SelectedSlot].RemoveFromClassList("jigsaw-selected");

            // Clicking the same shard twice just cancels the selection
            if (m_SelectedSlot == slot)
            {
                m_SelectedSlot = -1;
                UpdateStatus();
                return;
            }

            (m_ShardValues[m_SelectedSlot], m_ShardValues[slot]) =
                (m_ShardValues[slot], m_ShardValues[m_SelectedSlot]);

            ApplyShardVisual(m_SelectedSlot);
            ApplyShardVisual(slot);

            m_SelectedSlot = -1;
            UpdateStatus();

            if (IsSolved())
                HandleWinSequence();
        }

        private void ApplyShardVisual(int slot)
        {
            var value = m_ShardValues[slot];
            var shard = m_Shards[slot];

            shard.text = (value + 1).ToString();
            shard.style.backgroundColor = new StyleColor(ShardColours[value % ShardColours.Length]);
        }

        private bool IsSolved()
        {
            for (var i = 0; i < m_ShardValues.Count; i++)
                if (m_ShardValues[i] != i)
                    return false;

            return m_ShardValues.Count > 0;
        }

        private void UpdateStatus()
        {
            if (m_Status == null)
                return;

            m_Status.text = m_SelectedSlot >= 0 ? "Pick a shard to swap with" : "Click two shards to swap them";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[TreatJarJigsaw] JAR REBUILT! Delaying close...</color>");

            foreach (var shard in m_Shards)
                shard.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Treats saved!";

            ScheduleCompletion();
        }
    }
}