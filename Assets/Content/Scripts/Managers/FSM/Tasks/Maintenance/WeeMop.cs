#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    /// <summary>
    ///     Living Quarters maintenance: someone missed the litter box. Every puddle needs two
    ///     mop passes before it lifts. From the GMTK Miro board ("mop oil/vomit/wee").
    /// </summary>
    public sealed class WeeMop : TaskBase
    {
        [Header("Mop Settings")] [Tooltip("Mop passes needed before a puddle is gone.")] [SerializeField]
        private int m_PassesPerPuddle = 2;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Puddles = new();
        private readonly List<int> m_PassesLeft = new();

        private int m_ClearedPuddles;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("WeeMop: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Puddles.Clear();
            m_PassesLeft.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_ClearedPuddles = 0;
        }

        private void BindUI(VisualElement root)
        {
            m_Puddles.Clear();
            m_PassesLeft.Clear();
            m_ClearedPuddles = 0;

            m_Status = root.Q<Label>("wee-status");
            root.Query<VisualElement>(className: "wee-puddle").ToList(m_Puddles);

            if (m_Puddles.Count == 0)
            {
                Debug.LogError("WeeMop: No '.wee-puddle' elements found in UXML.");
                return;
            }

            var passes = Mathf.Max(1, m_PassesPerPuddle);

            for (var i = 0; i < m_Puddles.Count; i++)
            {
                var index = i;
                var puddle = m_Puddles[i];

                m_PassesLeft.Add(passes);

                // Scatter the puddles so the layout differs every time the task reopens
                puddle.style.left = new StyleLength(Length.Percent(Random.Range(6f, 74f)));
                puddle.style.top = new StyleLength(Length.Percent(Random.Range(8f, 66f)));
                puddle.style.opacity = 1f;
                puddle.style.display = DisplayStyle.Flex;
                puddle.pickingMode = PickingMode.Position;
                puddle.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    MopPuddle(index);
                });
            }

            UpdateStatus();
        }

        /// <summary>
        ///     Called when a puddle is clicked - fades it one pass at a time until it is gone.
        /// </summary>
        private void MopPuddle(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Puddles.Count)
                return;

            if (m_PassesLeft[index] <= 0)
                return;

            m_PassesLeft[index]--;

            var puddle = m_Puddles[index];

            if (m_PassesLeft[index] <= 0)
            {
                puddle.style.opacity = 0f;
                puddle.pickingMode = PickingMode.Ignore;
                m_ClearedPuddles++;
            }
            else
            {
                // Partially mopped — smaller and paler so progress reads clearly
                puddle.style.opacity = 0.45f;
                puddle.style.scale = new StyleScale(new Scale(new Vector2(0.7f, 0.7f)));
            }

            UpdateStatus();

            if (m_ClearedPuddles >= m_Puddles.Count)
                HandleWinSequence();
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Puddles left: {m_Puddles.Count - m_ClearedPuddles}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[WeeMop] FLOOR DRY! Delaying close...</color>");

            foreach (var puddle in m_Puddles)
                puddle.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "All dry!";

            ScheduleCompletion();
        }
    }
}