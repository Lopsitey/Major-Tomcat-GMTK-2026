#region

using System.Collections;
using UnityEngine;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks
{
    /// <summary>
    ///     Reusable "hey, look here!" pulse for a task's visible sprite. TaskBase auto-attaches
    ///     this to whichever SpriteRenderer is actually on screen (the logic object's own sprite
    ///     if it has one, otherwise its visible parent) and drives it while the task is waiting
    ///     to be clicked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TaskReadyPulse : MonoBehaviour
    {
        [Header("Pulse Settings")] [Tooltip("How fast the pulse cycles.")] [SerializeField]
        private float m_PulseSpeed = 2.5f;

        [Tooltip("Extra scale added at the peak of the pulse (e.g. 0.08 = up to 8% bigger).")] [SerializeField]
        private float m_ScaleAmount = 0.08f;

        [Tooltip("Lowest alpha reached at the bottom of the pulse.")] [SerializeField]
        private float m_MinAlpha = 0.55f;

        [SerializeField] private bool m_PulseScale = true;
        [SerializeField] private bool m_PulseAlpha = true;

        private SpriteRenderer m_SpriteRenderer;
        private Vector3 m_BaseScale;
        private Color m_BaseColor;
        private Coroutine m_PulseRoutine;

        private void Awake()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            m_BaseScale = transform.localScale;
            if (m_SpriteRenderer != null)
                m_BaseColor = m_SpriteRenderer.color;
        }

        /// <summary>
        ///     Begins pulsing. Safe to call repeatedly - restarts cleanly from the base scale/color.
        /// </summary>
        public void StartPulse()
        {
            if (!isActiveAndEnabled)
                return;

            StopPulseInternal(true);
            m_PulseRoutine = StartCoroutine(PulseRoutine());
        }

        /// <summary>
        ///     Stops pulsing and restores the original scale/alpha.
        /// </summary>
        public void StopPulse()
        {
            StopPulseInternal(true);
        }

        private void StopPulseInternal(bool resetVisuals)
        {
            if (m_PulseRoutine != null)
            {
                StopCoroutine(m_PulseRoutine);
                m_PulseRoutine = null;
            }

            if (!resetVisuals) return;

            transform.localScale = m_BaseScale;
            if (m_SpriteRenderer != null)
                m_SpriteRenderer.color = m_BaseColor;
        }

        private void OnDisable()
        {
            // Don't fight the base-scale restore when the whole object is going away.
            StopPulseInternal(true);
        }

        private IEnumerator PulseRoutine()
        {
            var t = 0f;
            while (true)
            {
                t += Time.deltaTime * m_PulseSpeed;
                var wave = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1

                if (m_PulseScale)
                    transform.localScale = m_BaseScale * (1f + wave * m_ScaleAmount);

                if (m_PulseAlpha && m_SpriteRenderer != null)
                {
                    var color = m_BaseColor;
                    color.a = Mathf.Lerp(m_MinAlpha, m_BaseColor.a, wave);
                    m_SpriteRenderer.color = color;
                }

                yield return null;
            }
        }
    }
}