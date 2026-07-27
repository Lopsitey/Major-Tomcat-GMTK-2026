#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Cockpit hazard: the console alarm bank is screaming. Only the currently flashing lamp
    ///     can be silenced, and the alarm keeps hopping to a different lamp.
    /// </summary>
    public sealed class AlarmSilence : TaskBase
    {
        [Header("Alarm Settings")] [Tooltip("Seconds before the alarm jumps to a different lamp.")] [SerializeField]
        private float m_HopInterval = 0.85f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Lamps = new();
        private readonly List<bool> m_Silenced = new();

        private int m_LitIndex = -1;
        private float m_HopTimer;
        private int m_SilencedCount;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("AlarmSilence: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Lamps.Clear();
            m_Silenced.Clear();
            m_Status = null;
            m_LitIndex = -1;
        }

        protected override void ResetTask()
        {
            m_SilencedCount = 0;
            m_HopTimer = 0f;
            m_LitIndex = -1;
        }

        private void BindUI(VisualElement root)
        {
            m_Lamps.Clear();
            m_Silenced.Clear();
            m_SilencedCount = 0;
            m_HopTimer = 0f;

            m_Status = root.Q<Label>("alarm-status");
            root.Query<VisualElement>(className: "alarm-lamp").ToList(m_Lamps);

            if (m_Lamps.Count == 0)
            {
                Debug.LogError("AlarmSilence: No '.alarm-lamp' elements found in UXML.");
                return;
            }

            for (var i = 0; i < m_Lamps.Count; i++)
            {
                var index = i;
                m_Silenced.Add(false);

                var lamp = m_Lamps[i];
                lamp.RemoveFromClassList("alarm-lit");
                lamp.RemoveFromClassList("alarm-off");
                lamp.pickingMode = PickingMode.Position;
                lamp.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    TrySilence(index);
                });
            }

            HopAlarm();
            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Lamps.Count == 0)
                return;

            m_HopTimer -= Time.deltaTime;
            if (m_HopTimer <= 0f)
                HopAlarm();
        }

        /// <summary>
        ///     Moves the flashing state onto a different lamp that has not been silenced yet.
        /// </summary>
        private void HopAlarm()
        {
            m_HopTimer = m_HopInterval;

            if (m_LitIndex >= 0 && m_LitIndex < m_Lamps.Count)
                m_Lamps[m_LitIndex].RemoveFromClassList("alarm-lit");

            var candidates = new List<int>();
            for (var i = 0; i < m_Lamps.Count; i++)
                if (!m_Silenced[i] && i != m_LitIndex)
                    candidates.Add(i);

            // Only one lamp left screaming — keep it lit rather than going dark
            if (candidates.Count == 0)
                for (var i = 0; i < m_Lamps.Count; i++)
                    if (!m_Silenced[i])
                        candidates.Add(i);

            if (candidates.Count == 0)
            {
                m_LitIndex = -1;
                return;
            }

            m_LitIndex = candidates[Random.Range(0, candidates.Count)];
            m_Lamps[m_LitIndex].AddToClassList("alarm-lit");
        }

        private void TrySilence(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Lamps.Count || m_Silenced[index])
                return;

            // Hitting a dark lamp does nothing but cost time
            if (index != m_LitIndex)
            {
                if (m_Status != null)
                    m_Status.text = "That one is not flashing!";
                return;
            }

            m_Silenced[index] = true;
            m_SilencedCount++;

            var lamp = m_Lamps[index];
            lamp.RemoveFromClassList("alarm-lit");
            lamp.AddToClassList("alarm-off");
            lamp.pickingMode = PickingMode.Ignore;

            m_LitIndex = -1;

            if (m_SilencedCount >= m_Lamps.Count)
            {
                HandleWinSequence();
                return;
            }

            HopAlarm();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Alarms blaring: {m_Lamps.Count - m_SilencedCount}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[AlarmSilence] BRIDGE QUIET! Delaying close...</color>");

            foreach (var lamp in m_Lamps)
                lamp.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "Silence at last!";

            ScheduleCompletion();
        }
    }
}