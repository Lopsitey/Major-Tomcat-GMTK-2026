#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    public sealed class Vomit : TaskBase
    {
        [Header("Vomit Settings")] [Tooltip("Number of vomit spots to clean.")] [SerializeField]
        private int m_VomitSpotCount = 5;

        private readonly List<VisualElement> m_VomitSpots = new();
        private int m_CleanedSpots;

        protected override void Awake()
        {
            base.Awake();

            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null) return;
            var root = m_MiniGameUI.rootVisualElement;

            // Query all vomit spot elements
            root.Query<VisualElement>(className: "vomit-spot").ToList(m_VomitSpots);

            // Register click handlers for all vomit spots
            for (var i = 0; i < m_VomitSpots.Count; i++)
            {
                var index = i;
                m_VomitSpots[i].RegisterCallback<ClickEvent>(_ => CleanSpot(index));
            }

            m_CleanedSpots = 0;
        }

        /// <summary>
        ///     Called when a vomit spot is clicked - removes it visually and checks for completion
        /// </summary>
        private void CleanSpot(int index)
        {
            if (index >= m_VomitSpots.Count) return;

            var spot = m_VomitSpots[index];

            // Disable this spot's interactions
            spot.pickingMode = PickingMode.Ignore;
            spot.style.opacity = 0f;

            m_CleanedSpots++;

            // Check if all spots are cleaned
            if (m_CleanedSpots >= m_VomitSpots.Count) HandleWinSequence();
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[Vomit] ALL CLEANED UP! Delaying close...</color>");

            // Disable all interactions
            foreach (var spot in m_VomitSpots) spot.pickingMode = PickingMode.Ignore;

            // Use UI Toolkit's native scheduler to wait 1 second before running CompleteTask()
            m_MiniGameUI.rootVisualElement.schedule.Execute(CompleteTask).StartingIn(1500);
        }
    }
}