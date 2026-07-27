#region

using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Living Quarters hazard: the water line the cat chewed is now over-pressured. Hit the
    ///     release valve while the needle is inside the safe band. The band shrinks each time.
    ///     Escalation of the water bowl pipe work from the GMTK Miro board ("water pipe").
    /// </summary>
    public sealed class PipeBurst : TaskBase
    {
        [Header("Valve Settings")] [Tooltip("Successful releases needed to bleed the line.")] [SerializeField]
        private int m_RequiredSeals = 3;

        [Tooltip("Needle sweep speed in percent of the gauge per second.")] [SerializeField]
        private float m_NeedleSpeed = 62f;

        [Tooltip("Width of the safe band as a percent of the gauge on the first attempt.")] [SerializeField]
        private float m_StartZoneWidth = 26f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private VisualElement m_Needle;
        private VisualElement m_SafeZone;
        private Button m_ReleaseButton;
        private Label m_Status;

        private float m_NeedlePosition;
        private float m_NeedleDirection = 1f;
        private float m_ZoneStart;
        private float m_ZoneWidth;

        // Sweep speed ramps up during a run, so the serialized value is the baseline to restore
        private float m_CurrentNeedleSpeed;
        private int m_Seals;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("PipeBurst: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Needle = null;
            m_SafeZone = null;
            m_ReleaseButton = null;
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_Seals = 0;
            m_NeedlePosition = 0f;
            m_NeedleDirection = 1f;
        }

        private void BindUI(VisualElement root)
        {
            m_Needle = root.Q<VisualElement>("gauge-needle");
            m_SafeZone = root.Q<VisualElement>("gauge-zone");
            m_ReleaseButton = root.Q<Button>("release-button");
            m_Status = root.Q<Label>("pipe-status");

            if (m_Needle == null || m_SafeZone == null)
            {
                Debug.LogError("PipeBurst: 'gauge-needle' or 'gauge-zone' missing from UXML.");
                return;
            }

            m_Seals = 0;
            m_NeedlePosition = Random.Range(0f, 100f);
            m_NeedleDirection = Random.value < 0.5f ? -1f : 1f;
            m_ZoneWidth = m_StartZoneWidth;
            m_CurrentNeedleSpeed = m_NeedleSpeed;

            RollSafeZone();

            if (m_ReleaseButton != null)
            {
                m_ReleaseButton.SetEnabled(true);
                m_ReleaseButton.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    AttemptRelease();
                });
            }

            ApplyNeedleVisual();
            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Needle == null)
                return;

            m_NeedlePosition += m_NeedleDirection * m_CurrentNeedleSpeed * Time.deltaTime;

            // Bounce off both ends of the gauge
            if (m_NeedlePosition >= 100f)
            {
                m_NeedlePosition = 100f;
                m_NeedleDirection = -1f;
            }
            else if (m_NeedlePosition <= 0f)
            {
                m_NeedlePosition = 0f;
                m_NeedleDirection = 1f;
            }

            ApplyNeedleVisual();
        }

        private void AttemptRelease()
        {
            if (IsCompleting || m_Needle == null)
                return;

            var inZone = m_NeedlePosition >= m_ZoneStart && m_NeedlePosition <= m_ZoneStart + m_ZoneWidth;

            if (!inZone)
            {
                // Miss: the line spikes and the needle whips faster
                m_CurrentNeedleSpeed = Mathf.Min(m_CurrentNeedleSpeed + 10f, 160f);
                if (m_Status != null)
                    m_Status.text = "Missed! Pressure spiking...";
                return;
            }

            m_Seals++;

            if (m_Seals >= m_RequiredSeals)
            {
                HandleWinSequence();
                return;
            }

            // Each success tightens the window and speeds the sweep up
            m_ZoneWidth = Mathf.Max(9f, m_ZoneWidth - 5f);
            m_CurrentNeedleSpeed = Mathf.Min(m_CurrentNeedleSpeed + 14f, 160f);
            RollSafeZone();
            UpdateStatus();
        }

        private void RollSafeZone()
        {
            m_ZoneStart = Random.Range(0f, 100f - m_ZoneWidth);

            if (m_SafeZone == null)
                return;

            m_SafeZone.style.left = new StyleLength(Length.Percent(m_ZoneStart));
            m_SafeZone.style.width = new StyleLength(Length.Percent(m_ZoneWidth));
        }

        private void ApplyNeedleVisual()
        {
            m_Needle.style.left = new StyleLength(Length.Percent(m_NeedlePosition));
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Releases left: {m_RequiredSeals - m_Seals}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[PipeBurst] LINE BLED! Delaying close...</color>");

            if (m_ReleaseButton != null)
            {
                m_ReleaseButton.SetEnabled(false);
                m_ReleaseButton.pickingMode = PickingMode.Ignore;
            }

            if (m_Status != null)
                m_Status.text = "Pressure normal!";

            ScheduleCompletion();
        }
    }
}