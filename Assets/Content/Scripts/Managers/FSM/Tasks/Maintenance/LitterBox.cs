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

        //UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_TrayElements = new();
        private VisualElement m_PooElement;
        private int m_PooTrayIndex;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnUIEnabled()
        {
            // Query UI elements now that UIDocument is guaranteed to exist
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("LitterBox: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
            ApplyHiddenPooState();
        }

        protected override void OnUIClosed()
        {
            // Drop stale VisualElement refs — the document tree is gone after disable
            m_TrayElements.Clear();
            m_PooElement = null;
        }

        protected override void ResetTask()
        {
            // Pick new random tray for next open; visual reset happens in OnUIEnabled
            m_PooTrayIndex = 0;
        }

        private void BindUI(VisualElement root)
        {
            m_TrayElements.Clear();
            root.Query<VisualElement>(className: "litter-tray").ToList(m_TrayElements);
            Debug.Log($"LitterBox: Found {m_TrayElements.Count} trays");

            if (m_TrayElements.Count == 0)
            {
                Debug.LogError("LitterBox: No litter trays found in UXML.");
                return;
            }

            // Prefer a tray count that matches the UXML; fall back to serialized hint
            var trayCount = m_TrayElements.Count > 0 ? m_TrayElements.Count : m_TrayCount;
            m_PooTrayIndex = Random.Range(0, trayCount);

            // One shared poo element lives in UXML; reparent it under the chosen tray
            m_PooElement = root.Q<VisualElement>("litter-poo");
            Debug.Log($"LitterBox: Poo element found: {m_PooElement != null}, under tray {m_PooTrayIndex}");

            if (m_PooElement != null)
            {
                var hostTray = m_TrayElements[m_PooTrayIndex];
                hostTray.Add(m_PooElement);
                m_PooElement.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    Debug.Log("LitterBox: Poo clicked!");
                    RemovePoo();
                });
            }

            // Register click handlers on this tree instance
            for (var i = 0; i < m_TrayElements.Count; i++)
            {
                var index = i;
                m_TrayElements[i].RegisterCallback<ClickEvent>(_ =>
                {
                    Debug.Log($"LitterBox: Tray {index} clicked!");
                    SelectTray(index);
                });
            }
        }

        private void ApplyHiddenPooState()
        {
            foreach (var tray in m_TrayElements)
            {
                tray.style.opacity = 1f;
                tray.pickingMode = PickingMode.Position;
            }

            if (m_PooElement == null)
                return;

            // Hidden until the correct tray is selected — Ignore so it can't steal tray clicks
            m_PooElement.style.opacity = 0f;
            m_PooElement.pickingMode = PickingMode.Ignore;
            m_PooElement.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        ///     Called when a tray is clicked - reveals poo if correct, otherwise dims the wrong tray
        /// </summary>
        private void SelectTray(int index)
        {
            if (IsCompleting || index < 0 || index >= m_TrayElements.Count)
                return;

            if (index == m_PooTrayIndex)
            {
                // Correct tray! Reveal the poo
                RevealPoo();
            }
            else
            {
                // Wrong tray - hide it (comment previously mentioned re-randomizing; kept simple for clarity)
                m_TrayElements[index].style.opacity = 0.3f;
                m_TrayElements[index].pickingMode = PickingMode.Ignore;
            }
        }

        /// <summary>
        ///     Reveals the poo so it can be clicked to remove
        /// </summary>
        private void RevealPoo()
        {
            if (m_PooElement == null)
                return;

            m_PooElement.style.opacity = 1f;
            m_PooElement.pickingMode = PickingMode.Position;
        }

        /// <summary>
        ///     Called when poo is clicked - removes it and completes the task
        /// </summary>
        private void RemovePoo()
        {
            if (IsCompleting)
                return;

            HandleWinSequence();
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[LitterBox] POOP SCOOPED! Delaying close...</color>");

            // Disable all interactions
            foreach (var tray in m_TrayElements)
                tray.pickingMode = PickingMode.Ignore;

            if (m_PooElement != null)
                m_PooElement.pickingMode = PickingMode.Ignore;

            ScheduleCompletion();
        }
    }
}
