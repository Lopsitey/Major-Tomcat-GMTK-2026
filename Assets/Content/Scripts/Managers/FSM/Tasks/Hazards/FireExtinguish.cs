#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Engine hazard: the drive bay is alight. Every click knocks a flame down a stage, but
    ///     left alone the fires keep growing back.
    /// </summary>
    public sealed class FireExtinguish : TaskBase
    {
        [Header("Fire Settings")] [Tooltip("Stages a flame passes through before it is out.")] [SerializeField]
        private int m_MaxStage = 3;

        [Tooltip("Seconds between a random surviving flame flaring up a stage.")] [SerializeField]
        private float m_GrowInterval = 2f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Flames = new();
        private readonly List<int> m_Stages = new();

        private float m_GrowTimer;
        private int m_Extinguished;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("FireExtinguish: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Flames.Clear();
            m_Stages.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_Extinguished = 0;
            m_GrowTimer = 0f;
        }

        private void BindUI(VisualElement root)
        {
            m_Flames.Clear();
            m_Stages.Clear();
            m_Extinguished = 0;
            m_GrowTimer = m_GrowInterval;

            m_Status = root.Q<Label>("fire-status");
            root.Query<VisualElement>(className: "flame").ToList(m_Flames);

            if (m_Flames.Count == 0)
            {
                Debug.LogError("FireExtinguish: No '.flame' elements found in UXML.");
                return;
            }

            for (var i = 0; i < m_Flames.Count; i++)
            {
                var index = i;
                var flame = m_Flames[i];

                m_Stages.Add(Random.Range(1, m_MaxStage + 1));

                flame.style.left = new StyleLength(Length.Percent(Random.Range(6f, 76f)));
                flame.style.top = new StyleLength(Length.Percent(Random.Range(10f, 62f)));
                flame.style.display = DisplayStyle.Flex;
                flame.pickingMode = PickingMode.Position;
                flame.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    DouseFlame(index);
                });

                ApplyFlameVisual(i);
            }

            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Flames.Count == 0)
                return;

            m_GrowTimer -= Time.deltaTime;
            if (m_GrowTimer > 0f)
                return;

            m_GrowTimer = m_GrowInterval;
            GrowRandomFlame();
        }

        /// <summary>
        ///     Flares one surviving flame back up so ignoring the panel loses ground.
        /// </summary>
        private void GrowRandomFlame()
        {
            var candidates = new List<int>();
            for (var i = 0; i < m_Flames.Count; i++)
                if (m_Stages[i] > 0 && m_Stages[i] < m_MaxStage)
                    candidates.Add(i);

            if (candidates.Count == 0)
                return;

            var index = candidates[Random.Range(0, candidates.Count)];
            m_Stages[index]++;
            ApplyFlameVisual(index);
        }

        private void DouseFlame(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Flames.Count || m_Stages[index] <= 0)
                return;

            m_Stages[index]--;
            ApplyFlameVisual(index);

            if (m_Stages[index] <= 0)
            {
                m_Extinguished++;
                UpdateStatus();

                if (m_Extinguished >= m_Flames.Count)
                    HandleWinSequence();
            }
        }

        private void ApplyFlameVisual(int index)
        {
            var flame = m_Flames[index];
            var stage = m_Stages[index];

            if (stage <= 0)
            {
                flame.style.opacity = 0f;
                flame.pickingMode = PickingMode.Ignore;
                return;
            }

            var normalised = (float)stage / m_MaxStage;
            flame.style.opacity = 1f;
            flame.style.scale = new StyleScale(new Scale(Vector2.one * Mathf.Lerp(0.55f, 1.25f, normalised)));

            // Small fires burn yellow, big ones go deep orange
            flame.style.backgroundColor =
                new StyleColor(Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(0.95f, 0.32f, 0.12f), normalised));
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Fires burning: {m_Flames.Count - m_Extinguished}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[FireExtinguish] BAY IS OUT! Delaying close...</color>");

            foreach (var flame in m_Flames)
                flame.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Fire out!";

            ScheduleCompletion();
        }
    }
}