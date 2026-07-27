#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    /// <summary>
    ///     Engine maintenance: stowaway mice are gnawing the wiring. Click each one while it
    ///     scurries across the bay. Companion piece to the litter box clean-up on the Miro board.
    /// </summary>
    public sealed class MouseHunt : TaskBase
    {
        [Header("Hunt Settings")] [Tooltip("Percent of the field a mouse crosses per second.")] [SerializeField]
        private float m_MouseSpeed = 34f;

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Mice = new();
        private readonly List<Vector2> m_Positions = new();
        private readonly List<Vector2> m_Targets = new();
        private readonly List<bool> m_Caught = new();

        private int m_CaughtCount;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("MouseHunt: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Mice.Clear();
            m_Positions.Clear();
            m_Targets.Clear();
            m_Caught.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_CaughtCount = 0;
        }

        private void BindUI(VisualElement root)
        {
            m_Mice.Clear();
            m_Positions.Clear();
            m_Targets.Clear();
            m_Caught.Clear();
            m_CaughtCount = 0;

            m_Status = root.Q<Label>("mouse-status");
            root.Query<VisualElement>(className: "mouse").ToList(m_Mice);

            if (m_Mice.Count == 0)
            {
                Debug.LogError("MouseHunt: No '.mouse' elements found in UXML.");
                return;
            }

            for (var i = 0; i < m_Mice.Count; i++)
            {
                var index = i;
                var mouse = m_Mice[i];

                m_Positions.Add(RandomPoint());
                m_Targets.Add(RandomPoint());
                m_Caught.Add(false);

                mouse.style.opacity = 1f;
                mouse.pickingMode = PickingMode.Position;
                mouse.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    CatchMouse(index);
                });

                ApplyPosition(index);
            }

            UpdateStatus();
        }

        private void Update()
        {
            if (!IsUIShown || IsCompleting || m_Mice.Count == 0)
                return;

            var step = m_MouseSpeed * Time.deltaTime;

            for (var i = 0; i < m_Mice.Count; i++)
            {
                if (m_Caught[i])
                    continue;

                var position = Vector2.MoveTowards(m_Positions[i], m_Targets[i], step);
                m_Positions[i] = position;

                // Reached the waypoint — pick somewhere new to scurry off to
                if (Vector2.Distance(position, m_Targets[i]) < 0.5f)
                    m_Targets[i] = RandomPoint();

                ApplyPosition(i);
            }
        }

        private static Vector2 RandomPoint()
        {
            return new Vector2(Random.Range(4f, 82f), Random.Range(6f, 70f));
        }

        private void ApplyPosition(int index)
        {
            var mouse = m_Mice[index];
            var position = m_Positions[index];

            mouse.style.left = new StyleLength(Length.Percent(position.x));
            mouse.style.top = new StyleLength(Length.Percent(position.y));

            // Flip the sprite so the mouse faces the way it is running
            var facing = m_Targets[index].x < position.x ? -1f : 1f;
            mouse.style.scale = new StyleScale(new Scale(new Vector2(facing, 1f)));
        }

        private void CatchMouse(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Mice.Count || m_Caught[index])
                return;

            m_Caught[index] = true;
            m_CaughtCount++;

            var mouse = m_Mice[index];
            mouse.style.opacity = 0f;
            mouse.pickingMode = PickingMode.Ignore;

            UpdateStatus();

            if (m_CaughtCount >= m_Mice.Count)
                HandleWinSequence();
        }

        private void UpdateStatus()
        {
            if (m_Status != null)
                m_Status.text = $"Mice loose: {m_Mice.Count - m_CaughtCount}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[MouseHunt] BAY CLEARED! Delaying close...</color>");

            foreach (var mouse in m_Mice)
                mouse.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "All rounded up!";

            ScheduleCompletion();
        }
    }
}