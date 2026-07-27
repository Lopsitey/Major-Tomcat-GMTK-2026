#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Electrical hazard: cat hair has clogged the fan cogs. Each cog spins past a brush at the
    ///     top — click the cog only while a grime clump is inside that brush zone.
    ///     From the GMTK Miro board ("cog cleaning").
    /// </summary>
    public sealed class CogCleaning : TaskBase
    {
        [Header("Cog Settings")] [Tooltip("Half-width of the brush zone in degrees.")] [SerializeField]
        private float m_BrushTolerance = 26f;

        [Tooltip("Extra degrees per second added to a cog after a mistimed click.")] [SerializeField]
        private float m_MissPenaltySpeed = 20f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Cogs = new();
        private readonly List<List<VisualElement>> m_Grime = new();
        private readonly List<List<bool>> m_GrimeCleaned = new();
        private readonly List<float> m_Angles = new();
        private readonly List<float> m_Speeds = new();

        private int m_RemainingGrime;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("CogCleaning: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Cogs.Clear();
            m_Grime.Clear();
            m_GrimeCleaned.Clear();
            m_Angles.Clear();
            m_Speeds.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_RemainingGrime = 0;
        }

        private void BindUI(VisualElement root)
        {
            m_Cogs.Clear();
            m_Grime.Clear();
            m_GrimeCleaned.Clear();
            m_Angles.Clear();
            m_Speeds.Clear();
            m_RemainingGrime = 0;

            m_Status = root.Q<Label>("cog-status");
            root.Query<VisualElement>(className: "cog").ToList(m_Cogs);

            if (m_Cogs.Count == 0)
            {
                Debug.LogError("CogCleaning: No '.cog' elements found in UXML.");
                return;
            }

            for (var i = 0; i < m_Cogs.Count; i++)
            {
                var cogIndex = i;
                var cog = m_Cogs[i];

                var clumps = new List<VisualElement>();
                cog.Query<VisualElement>(className: "cog-grime").ToList(clumps);
                m_Grime.Add(clumps);

                var cleaned = new List<bool>();
                foreach (var clump in clumps)
                {
                    cleaned.Add(false);
                    clump.style.opacity = 1f;
                    // Grime rides the cog, so it must never eat the cog's own clicks
                    clump.pickingMode = PickingMode.Ignore;
                }

                m_GrimeCleaned.Add(cleaned);
                m_RemainingGrime += clumps.Count;

                m_Angles.Add(Random.Range(0f, 360f));
                m_Speeds.Add(Random.Range(55f, 95f) * (i % 2 == 0 ? 1f : -1f));

                cog.pickingMode = PickingMode.Position;
                cog.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    ScrubCog(cogIndex);
                });
            }

            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Cogs.Count == 0)
                return;

            for (var i = 0; i < m_Cogs.Count; i++)
            {
                m_Angles[i] = Mathf.Repeat(m_Angles[i] + m_Speeds[i] * Time.deltaTime, 360f);
                m_Cogs[i].style.rotate =
                    new StyleRotate(new Rotate(new Angle(m_Angles[i], AngleUnit.Degree)));
            }
        }

        /// <summary>
        ///     Clicking a cog only scrubs if one of its remaining clumps is under the brush at the top.
        /// </summary>
        private void ScrubCog(int cogIndex)
        {
            if (IsCompleting || cogIndex < 0 || cogIndex >= m_Cogs.Count)
                return;

            var clumps = m_Grime[cogIndex];
            var cleaned = m_GrimeCleaned[cogIndex];
            var spacing = clumps.Count > 0 ? 360f / clumps.Count : 360f;

            for (var i = 0; i < clumps.Count; i++)
            {
                if (cleaned[i])
                    continue;

                // Clump i sits at i*spacing on the cog, so its world angle includes the cog spin
                var worldAngle = Mathf.Repeat(m_Angles[cogIndex] + i * spacing, 360f);
                var offsetFromBrush = Mathf.Abs(Mathf.DeltaAngle(worldAngle, 0f));

                if (offsetFromBrush > m_BrushTolerance)
                    continue;

                cleaned[i] = true;
                clumps[i].style.opacity = 0f;
                m_RemainingGrime--;

                UpdateStatus();

                if (m_RemainingGrime <= 0)
                    HandleWinSequence();

                return;
            }

            // Nothing under the brush — the cog kicks and spins faster
            m_Speeds[cogIndex] += Mathf.Sign(m_Speeds[cogIndex]) * m_MissPenaltySpeed;
            m_Speeds[cogIndex] = Mathf.Clamp(m_Speeds[cogIndex], -220f, 220f);

            if (m_Status != null)
                m_Status.text = "Wait for the grime to reach the brush!";
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Grime left: {m_RemainingGrime}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[CogCleaning] COGS SPOTLESS! Delaying close...</color>");

            foreach (var cog in m_Cogs)
                cog.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Cogs spotless!";

            ScheduleCompletion();
        }
    }
}