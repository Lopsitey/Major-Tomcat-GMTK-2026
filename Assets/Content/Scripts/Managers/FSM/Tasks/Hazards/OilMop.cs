#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Living Quarters hazard: a tipped oil can keeps creeping across the deck. Clicking a
    ///     slick removes it, but a fresh one seeps in every couple of seconds until you out-mop it.
    ///     From the GMTK Miro board ("mop oil/vomit/wee").
    /// </summary>
    public sealed class OilMop : TaskBase
    {
        [Header("Slick Settings")]
        [Tooltip("Total slicks that must be mopped up to clear the hazard.")]
        [SerializeField]
        private int m_RequiredCleans = 6;

        [Tooltip("Slicks showing when the hazard opens.")] [SerializeField]
        private int m_StartingSlicks = 3;

        [Tooltip("Seconds between new slicks seeping in.")] [SerializeField]
        private float m_SpreadInterval = 2.2f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Slicks = new();
        private readonly List<bool> m_SlickVisible = new();

        private float m_SpreadTimer;
        private int m_Cleaned;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("OilMop: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Slicks.Clear();
            m_SlickVisible.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_Cleaned = 0;
            m_SpreadTimer = 0f;
        }

        private void BindUI(VisualElement root)
        {
            m_Slicks.Clear();
            m_SlickVisible.Clear();
            m_Cleaned = 0;
            m_SpreadTimer = m_SpreadInterval;

            m_Status = root.Q<Label>("oil-status");
            root.Query<VisualElement>(className: "oil-slick").ToList(m_Slicks);

            if (m_Slicks.Count == 0)
            {
                Debug.LogError("OilMop: No '.oil-slick' elements found in UXML.");
                return;
            }

            for (var i = 0; i < m_Slicks.Count; i++)
            {
                var index = i;
                m_SlickVisible.Add(false);

                var slick = m_Slicks[i];
                slick.style.display = DisplayStyle.None;
                slick.pickingMode = PickingMode.Position;
                slick.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    MopSlick(index);
                });
            }

            var starting = Mathf.Clamp(m_StartingSlicks, 1, m_Slicks.Count);
            for (var i = 0; i < starting; i++)
                SpawnSlick();

            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Slicks.Count == 0)
                return;

            m_SpreadTimer -= Time.deltaTime;
            if (m_SpreadTimer > 0f)
                return;

            m_SpreadTimer = m_SpreadInterval;
            SpawnSlick();
            UpdateStatus();
        }

        /// <summary>
        ///     Reveals one hidden slick at a random spot. Cleaned elements are recycled, so the
        ///     spread keeps pressure on without ever running out of pooled elements.
        /// </summary>
        private void SpawnSlick()
        {
            var hidden = new List<int>();
            for (var i = 0; i < m_Slicks.Count; i++)
                if (!m_SlickVisible[i])
                    hidden.Add(i);

            if (hidden.Count == 0)
                return;

            var index = hidden[Random.Range(0, hidden.Count)];
            var slick = m_Slicks[index];

            slick.style.left = new StyleLength(Length.Percent(Random.Range(4f, 78f)));
            slick.style.top = new StyleLength(Length.Percent(Random.Range(6f, 68f)));
            slick.style.scale = new StyleScale(new Scale(Vector2.one * Random.Range(0.75f, 1.25f)));
            slick.style.display = DisplayStyle.Flex;
            slick.pickingMode = PickingMode.Position;

            m_SlickVisible[index] = true;
        }

        private void MopSlick(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Slicks.Count || !m_SlickVisible[index])
                return;

            m_SlickVisible[index] = false;
            m_Slicks[index].style.display = DisplayStyle.None;
            m_Cleaned++;

            UpdateStatus();

            if (m_Cleaned >= m_RequiredCleans)
                HandleWinSequence();
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Oil mopped: {m_Cleaned}/{m_RequiredCleans}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[OilMop] DECK DEGREASED! Delaying close...</color>");

            for (var i = 0; i < m_Slicks.Count; i++)
            {
                m_Slicks[i].pickingMode = PickingMode.Ignore;
                m_Slicks[i].style.display = DisplayStyle.None;
                m_SlickVisible[i] = false;
            }

            if (m_Status != null)
                m_Status.text = "Deck clean!";

            ScheduleCompletion();
        }
    }
}