#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    /// <summary>
    ///     Cockpit maintenance: click each nose-cone ring to twist until all align (0°).
    ///     From the GMTK Miro board ("nose cone twist").
    /// </summary>
    public sealed class NoseConeTwist : TaskBase
    {
        private readonly List<VisualElement> m_Rings = new();
        private readonly List<int> m_RingAngles = new(); // multiples of 90

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
                return;

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Rings.Clear();
            m_RingAngles.Clear();
        }

        protected override void ResetTask()
        {
            // Angles re-rolled in BindUI
        }

        private void BindUI(VisualElement root)
        {
            m_Rings.Clear();
            m_RingAngles.Clear();
            root.Query<VisualElement>(className: "nose-ring").ToList(m_Rings);

            for (var i = 0; i < m_Rings.Count; i++)
            {
                // Random misalignment so at least one click is needed
                var turns = Random.Range(1, 4);
                m_RingAngles.Add(turns * 90);
                ApplyRingVisual(i);

                var index = i;
                m_Rings[i].pickingMode = PickingMode.Position;
                m_Rings[i].RegisterCallback<ClickEvent>(_ => TwistRing(index));
            }
        }

        private void TwistRing(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Rings.Count)
                return;

            m_RingAngles[index] = (m_RingAngles[index] + 90) % 360;
            ApplyRingVisual(index);

            if (AllAligned())
                HandleWinSequence();
        }

        private void ApplyRingVisual(int index)
        {
            m_Rings[index].style.rotate =
                new StyleRotate(new Rotate(new Angle(m_RingAngles[index], AngleUnit.Degree)));
        }

        private bool AllAligned()
        {
            for (var i = 0; i < m_RingAngles.Count; i++)
                if (m_RingAngles[i] != 0)
                    return false;
            return m_RingAngles.Count > 0;
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[NoseConeTwist] CONE ALIGNED! Delaying close...</color>");

            foreach (var ring in m_Rings)
                ring.pickingMode = PickingMode.Ignore;

            ScheduleCompletion();
        }
    }
}