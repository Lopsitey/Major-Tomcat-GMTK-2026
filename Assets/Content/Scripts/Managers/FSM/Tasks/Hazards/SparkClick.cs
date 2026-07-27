#region

using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Electrical hazard: arcs are jumping around the breaker panel. Ground each one before it
    ///     fizzles out somewhere else — every hit makes the next arc shorter lived.
    /// </summary>
    public sealed class SparkClick : TaskBase
    {
        [Header("Arc Settings")] [Tooltip("Arcs that must be grounded to clear the hazard.")] [SerializeField]
        private int m_RequiredHits = 6;

        [Tooltip("Seconds the first arc stays put.")] [SerializeField]
        private float m_StartLifetime = 1.25f;

        [Tooltip("Shortest an arc will ever linger.")] [SerializeField]
        private float m_MinLifetime = 0.5f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private VisualElement m_Spark;
        private Label m_Status;

        private float m_Lifetime;
        private float m_ArcTimer;
        private int m_Hits;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("SparkClick: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Spark = null;
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_Hits = 0;
            m_Lifetime = m_StartLifetime;
        }

        private void BindUI(VisualElement root)
        {
            m_Spark = root.Q<VisualElement>("spark");
            m_Status = root.Q<Label>("spark-status");

            if (m_Spark == null)
            {
                Debug.LogError("SparkClick: 'spark' element missing from UXML.");
                return;
            }

            m_Hits = 0;
            m_Lifetime = m_StartLifetime;

            m_Spark.pickingMode = PickingMode.Position;
            m_Spark.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                GroundArc();
            });

            MoveArc();
            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Spark == null)
                return;

            m_ArcTimer -= Time.deltaTime;
            if (m_ArcTimer <= 0f)
                MoveArc();
        }

        /// <summary>
        ///     Jumps the arc somewhere new and restarts its countdown.
        /// </summary>
        private void MoveArc()
        {
            m_ArcTimer = m_Lifetime;

            m_Spark.style.left = new StyleLength(Length.Percent(Random.Range(4f, 80f)));
            m_Spark.style.top = new StyleLength(Length.Percent(Random.Range(6f, 68f)));
            m_Spark.style.rotate = new StyleRotate(new Rotate(new Angle(Random.Range(0f, 360f), AngleUnit.Degree)));
        }

        private void GroundArc()
        {
            if (IsCompleting || m_Spark == null)
                return;

            m_Hits++;

            if (m_Hits >= m_RequiredHits)
            {
                HandleWinSequence();
                return;
            }

            // Each grounded arc makes the panel angrier
            m_Lifetime = Mathf.Max(m_MinLifetime, m_Lifetime - 0.12f);
            MoveArc();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Arcs grounded: {m_Hits}/{m_RequiredHits}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[SparkClick] PANEL GROUNDED! Delaying close...</color>");

            if (m_Spark != null)
            {
                m_Spark.pickingMode = PickingMode.Ignore;
                m_Spark.style.display = DisplayStyle.None;
            }

            if (m_Status != null)
                m_Status.text = "No more arcs!";

            ScheduleCompletion();
        }
    }
}