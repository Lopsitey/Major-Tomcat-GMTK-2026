#region

using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    /// <summary>
    ///     Cockpit maintenance: the food seesaw keeps tipping in zero-G. Tap the raised pan to
    ///     shove it back down and hold the beam level long enough for the kibble to settle.
    ///     From the GMTK Miro board ("food seesaw").
    /// </summary>
    public sealed class FoodSeesaw : TaskBase
    {
        [Header("Seesaw Settings")] [Tooltip("Degrees of tilt that still counts as balanced.")] [SerializeField]
        private float m_BalanceTolerance = 5f;

        [Tooltip("Seconds the beam must stay level before the kibble settles.")] [SerializeField]
        private float m_RequiredHoldSeconds = 2f;

        [Tooltip("How hard the beam runs away from level. Higher = twitchier.")] [SerializeField]
        private float m_Instability = 3.2f;

        [Tooltip("Degrees per second removed from the tilt speed when a pan is tapped.")] [SerializeField]
        private float m_TapImpulse = 26f;

        private const float MaxTilt = 32f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private VisualElement m_Beam;
        private VisualElement m_LeftPan;
        private VisualElement m_RightPan;
        private VisualElement m_BalanceFill;
        private Label m_Status;

        // Positive tilt means the right-hand pan is dipping
        private float m_Tilt;
        private float m_TiltSpeed;
        private float m_HeldSeconds;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("FoodSeesaw: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            // Drop stale VisualElement refs — the document tree is gone after disable
            m_Beam = null;
            m_LeftPan = null;
            m_RightPan = null;
            m_BalanceFill = null;
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_HeldSeconds = 0f;
            m_Tilt = 0f;
            m_TiltSpeed = 0f;
        }

        private void BindUI(VisualElement root)
        {
            m_Beam = root.Q<VisualElement>("seesaw-beam");
            m_LeftPan = root.Q<VisualElement>("pan-left");
            m_RightPan = root.Q<VisualElement>("pan-right");
            m_BalanceFill = root.Q<VisualElement>("balance-fill");
            m_Status = root.Q<Label>("seesaw-status");

            if (m_Beam == null)
            {
                Debug.LogError("FoodSeesaw: 'seesaw-beam' missing from UXML.");
                return;
            }

            m_HeldSeconds = 0f;

            // Start already tipping so the player has something to correct
            m_Tilt = Random.value < 0.5f ? -14f : 14f;
            m_TiltSpeed = 0f;

            if (m_LeftPan != null)
            {
                m_LeftPan.pickingMode = PickingMode.Position;
                m_LeftPan.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    NudgePan(-1f);
                });
            }

            if (m_RightPan != null)
            {
                m_RightPan.pickingMode = PickingMode.Position;
                m_RightPan.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    NudgePan(1f);
                });
            }

            ApplyVisuals();
        }

        /// <summary>
        ///     Tapping a pan pushes that side down, which drags the tilt towards that pan.
        /// </summary>
        private void NudgePan(float direction)
        {
            if (IsCompleting)
                return;

            m_TiltSpeed += direction * m_TapImpulse;
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Beam == null)
                return;

            var deltaTime = Time.deltaTime;

            // Unstable equilibrium: the further from level, the faster it runs away
            m_TiltSpeed += m_Tilt * m_Instability * deltaTime;

            // Light damping so taps do not stack into an unrecoverable spin
            m_TiltSpeed = Mathf.Lerp(m_TiltSpeed, 0f, 1.6f * deltaTime);

            m_Tilt = Mathf.Clamp(m_Tilt + m_TiltSpeed * deltaTime, -MaxTilt, MaxTilt);

            // Beam bottoms out against the frame rather than spinning past it
            if (Mathf.Abs(m_Tilt) >= MaxTilt)
                m_TiltSpeed = 0f;

            if (Mathf.Abs(m_Tilt) <= m_BalanceTolerance)
                m_HeldSeconds += deltaTime;
            else
                m_HeldSeconds = Mathf.Max(0f, m_HeldSeconds - deltaTime * 0.75f);

            ApplyVisuals();

            if (m_HeldSeconds >= m_RequiredHoldSeconds)
                HandleWinSequence();
        }

        private void ApplyVisuals()
        {
            if (m_Beam == null)
                return;

            m_Beam.style.rotate = new StyleRotate(new Rotate(new Angle(m_Tilt, AngleUnit.Degree)));

            var progress = Mathf.Clamp01(m_HeldSeconds / m_RequiredHoldSeconds);
            if (m_BalanceFill != null)
                m_BalanceFill.style.width = new StyleLength(Length.Percent(progress * 100f));

            if (m_Status == null)
                return;

            m_Status.text = Mathf.Abs(m_Tilt) <= m_BalanceTolerance ? "Steady..." : "Tap the raised pan!";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[FoodSeesaw] KIBBLE BALANCED! Delaying close...</color>");

            if (m_LeftPan != null)
                m_LeftPan.pickingMode = PickingMode.Ignore;

            if (m_RightPan != null)
                m_RightPan.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Balanced!";

            ScheduleCompletion();
        }
    }
}