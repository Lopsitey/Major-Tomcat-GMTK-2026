#region

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    public sealed class Vomit : TaskBase
    {
        [Header("Vomit Settings")] [Tooltip("Number of vomit spots to clean.")] [SerializeField]
        private int m_VomitSpotCount = 5;

        [Header("Win VFX")]
        [Tooltip("Optional bubble particle system. If empty, one is created at runtime.")]
        [SerializeField]
        private ParticleSystem m_BubbleParticles;

        private readonly List<VisualElement> m_VomitSpots = new();
        private int m_CleanedSpots;
        private ParticleSystem m_RuntimeBubbles;
        private Coroutine m_BubbleSweepRoutine;

        protected override void Awake()
        {
            base.Awake();

            m_CleanedSpots = 0;
            EnsureBubbleParticles();
        }

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null) return;

            // Re-bind every open — UIDocument rebuilds the visual tree on enable
            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_VomitSpots.Clear();
            StopBubbleSweep();
        }

        protected override void ResetTask()
        {
            m_CleanedSpots = 0;
            StopBubbleSweep();
            if (m_RuntimeBubbles != null)
                m_RuntimeBubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void BindUI(VisualElement root)
        {
            m_VomitSpots.Clear();
            root.Query<VisualElement>(className: "vomit-spot").ToList(m_VomitSpots);

            for (var i = 0; i < m_VomitSpots.Count; i++)
            {
                var index = i;
                var spot = m_VomitSpots[i];
                spot.style.opacity = 1f;
                spot.pickingMode = PickingMode.Position;
                spot.RegisterCallback<ClickEvent>(_ => CleanSpot(index));
            }
        }

        /// <summary>
        ///     Called when a vomit spot is clicked - removes it visually and checks for completion
        /// </summary>
        private void CleanSpot(int index)
        {
            if (IsCompleting || index < 0 || index >= m_VomitSpots.Count)
                return;

            var spot = m_VomitSpots[index];

            // Disable this spot's interactions
            spot.pickingMode = PickingMode.Ignore;
            spot.style.opacity = 0f;

            m_CleanedSpots++;

            // Check if all spots are cleaned
            if (m_CleanedSpots >= m_VomitSpots.Count)
                HandleWinSequence();
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[Vomit] ALL CLEANED UP! Delaying close...</color>");

            // Disable all interactions
            foreach (var spot in m_VomitSpots)
                spot.pickingMode = PickingMode.Ignore;

            PlayBubbleSweep();
            ScheduleCompletion();
        }

        private void EnsureBubbleParticles()
        {
            if (m_BubbleParticles != null)
            {
                m_RuntimeBubbles = m_BubbleParticles;
                return;
            }

            var go = new GameObject("VomitBubbleVFX");
            go.transform.SetParent(transform, false);
            m_RuntimeBubbles = go.AddComponent<ParticleSystem>();

            var main = m_RuntimeBubbles.main;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 0.4f;
            main.startSize = 0.18f;
            main.startColor = new Color(0.55f, 0.85f, 1f, 0.75f);
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = m_RuntimeBubbles.emission;
            emission.rateOverTime = 28f;

            var shape = m_RuntimeBubbles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var colorOverLifetime = m_RuntimeBubbles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(0.9f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = m_RuntimeBubbles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 50;

            m_RuntimeBubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>
        ///     Sweeps bubbles diagonally from bottom-left to top-right across the UI during the win delay.
        /// </summary>
        private void PlayBubbleSweep()
        {
            if (m_RuntimeBubbles == null)
                EnsureBubbleParticles();

            StopBubbleSweep();
            m_BubbleSweepRoutine = StartCoroutine(BubbleSweepCoroutine());
        }

        private IEnumerator BubbleSweepCoroutine()
        {
            var cam = Camera.main;
            if (cam == null || m_RuntimeBubbles == null)
                yield break;

            // Screen-space diagonal: bottom-left -> top-right, projected into world
            var start = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, 5f));
            var end = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, 5f));
            start.z = 0f;
            end.z = 0f;

            m_RuntimeBubbles.transform.position = start;
            m_RuntimeBubbles.Play();

            const float duration = 1.4f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                m_RuntimeBubbles.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            m_RuntimeBubbles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            m_BubbleSweepRoutine = null;
        }

        private void StopBubbleSweep()
        {
            if (m_BubbleSweepRoutine != null)
            {
                StopCoroutine(m_BubbleSweepRoutine);
                m_BubbleSweepRoutine = null;
            }
        }
    }
}
