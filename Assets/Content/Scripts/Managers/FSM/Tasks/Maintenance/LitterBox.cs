#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    public sealed class LitterBox : TaskBase
    {
        [Header("Litter Box Settings")] [Tooltip("Number of trays to hide poo under.")] [SerializeField]
        private int m_TrayCount = 4;

        //UI refs
        private readonly List<VisualElement> m_TrayElements = new();
        private VisualElement m_PooElement;
        private int m_PooTrayIndex;
        private int m_RemovedPooCount;

        protected override void Awake()
        {
            base.Awake();

            // Don't query UI elements yet - wait until OnUIEnabled
            m_RemovedPooCount = 0;
        }

        protected override void OnUIEnabled()
        {
            // Query UI elements now that UIDocument is guaranteed to exist
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("LitterBox: UIDocument not found!");
                return;
            }

            var root = m_MiniGameUI.rootVisualElement;

            // Only query trays once
            if (m_TrayElements.Count == 0)
            {
                root.Query<VisualElement>(className: "litter-tray").ToList(m_TrayElements);
                Debug.Log($"LitterBox: Found {m_TrayElements.Count} trays");

                // Query the poo element
                m_PooElement = root.Q<VisualElement>("litter-poo");
                Debug.Log($"LitterBox: Poo element found: {m_PooElement != null}");

                // Randomly select which tray hides the poo
                m_PooTrayIndex = Random.Range(0, m_TrayElements.Count);

                // Position the poo under the selected tray
                UpdatePooPosition();

                // Register click handlers ONCE - they persist
                for (var i = 0; i < m_TrayElements.Count; i++)
                {
                    var index = i;
                    m_TrayElements[i].RegisterCallback<ClickEvent>(_ =>
                    {
                        Debug.Log($"LitterBox: Tray {index} clicked!");
                        SelectTray(index);
                    });
                }

                if (m_PooElement != null)
                    m_PooElement.RegisterCallback<ClickEvent>(_ =>
                    {
                        Debug.Log("LitterBox: Poo clicked!");
                        RemovePoo();
                    });
            }

            // Ensure all trays have proper picking mode when UI is shown
            for (var i = 0; i < m_TrayElements.Count; i++) m_TrayElements[i].pickingMode = PickingMode.Position;

            if (m_PooElement != null) m_PooElement.pickingMode = PickingMode.Position;
        }

        protected override void ResetTask()
        {
            // Reset all trays to visible and re-enable interactions
            for (var i = 0; i < m_TrayElements.Count; i++)
            {
                var tray = m_TrayElements[i];
                tray.style.opacity = 1f;
                tray.pickingMode = PickingMode.Position;
            }

            // Reset poo
            if (m_PooElement != null)
            {
                m_PooElement.style.opacity = 0f;
                m_PooElement.pickingMode = PickingMode.Ignore;
            }

            // Pick new random tray
            m_PooTrayIndex = Random.Range(0, m_TrayElements.Count);
            m_RemovedPooCount = 0;
        }

        /// <summary>
        ///     Updates the visual position of the poo to be under the current tray
        /// </summary>
        private void UpdatePooPosition()
        {
            if (m_PooElement == null) return;

            // Make poo invisible until uncovered (it will be positioned absolutely over the tray)
            m_PooElement.style.opacity = 0f;
            m_PooElement.pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        ///     Called when a tray is clicked - reveals poo if correct, otherwise hides another random tray
        /// </summary>
        private void SelectTray(int index)
        {
            if (index == m_PooTrayIndex)
            {
                // Correct tray! Reveal the poo
                RevealPoo();
            }
            else
            {
                // Wrong tray - hide it and pick a new random tray
                m_TrayElements[index].style.opacity = 0.3f;
                m_TrayElements[index].pickingMode = PickingMode.Ignore;
            }
        }

        /// <summary>
        ///     Reveals the poo so it can be clicked to remove
        /// </summary>
        private void RevealPoo()
        {
            if (m_PooElement == null) return;

            m_PooElement.style.opacity = 1f;
            m_PooElement.pickingMode = PickingMode.Position;
        }

        /// <summary>
        ///     Called when poo is clicked - removes it and completes the task
        /// </summary>
        private void RemovePoo()
        {
            m_RemovedPooCount++;

            if (m_RemovedPooCount >= 1) // All poo removed
                HandleWinSequence();
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[LitterBox] POOP SCOOPED! Delaying close...</color>");

            // Disable all interactions
            foreach (var tray in m_TrayElements) tray.pickingMode = PickingMode.Ignore;

            if (m_PooElement != null) m_PooElement.pickingMode = PickingMode.Ignore;

            // Use UI Toolkit's native scheduler to wait 1 second before running CompleteTask()
            m_MiniGameUI.rootVisualElement.schedule.Execute(CompleteTask).StartingIn(1500);
        }
    }
}