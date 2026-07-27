#region

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class TaskBase : MonoBehaviour
    {
        protected UIDocument m_MiniGameUI;
        private Coroutine m_CompletionCoroutine;
        private TaskReadyPulse m_ReadyPulse;

        /// <summary>
        ///     The task whose mini-game UI is currently open, if any.
        /// </summary>
        public static TaskBase ActiveOpenTask { get; private set; }

        public bool IsUIShown { get; private set; }

        public bool IsCompleting { get; private set; }

        protected virtual void Awake()
        {
            // Search this GameObject first, then children
            m_MiniGameUI = GetComponent<UIDocument>() ?? GetComponentInChildren<UIDocument>();
            IsUIShown = false;
            IsCompleting = false;
            if (!m_MiniGameUI)
            {
                Debug.LogError($"{gameObject.name}: No UIDocument found on this GameObject or its children!");
                return;
            }

            // Disable the UIDocument component when the object first wakes up
            m_MiniGameUI.enabled = false;
        }

        protected virtual void OnEnable()
        {
            // When task GameObject is enabled, ensure UIDocument stays disabled
            // It should ONLY be enabled when player clicks (Interact())
            CloseUI(true);
            ResetTask();
            StartReadyPulse();
        }

        protected virtual void OnDisable()
        {
            // Always disable UIDocument when task is deactivated
            CloseUI(true);
            StopReadyPulse();
        }

        /// <summary>
        ///     Finds the sprite that's actually visible for this task (its own SpriteRenderer if
        ///     it has one, otherwise its parent's) and starts a TaskReadyPulse on it so the player
        ///     notices the newly-activated task.
        /// </summary>
        private void StartReadyPulse()
        {
            var targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null && transform.parent != null)
                targetRenderer = transform.parent.GetComponent<SpriteRenderer>();

            if (targetRenderer == null)
                return;

            m_ReadyPulse = targetRenderer.GetComponent<TaskReadyPulse>();
            if (m_ReadyPulse == null)
                m_ReadyPulse = targetRenderer.gameObject.AddComponent<TaskReadyPulse>();

            m_ReadyPulse.StartPulse();
        }

        private void StopReadyPulse()
        {
            if (m_ReadyPulse != null)
                m_ReadyPulse.StopPulse();
        }

        /// <summary>
        ///     Override this in subclasses to reset puzzle state when task is reactivated
        /// </summary>
        protected virtual void ResetTask()
        {
        }

        /// <summary>
        ///     Called via the New Input System Raycast in InputHandler.cs
        /// </summary>
        public virtual void Interact()
        {
            if (!m_MiniGameUI)
            {
                Debug.LogError($"{gameObject.name}: Interact called but UIDocument is null!");
                return;
            }

            if (IsCompleting || !gameObject.activeInHierarchy)
                return;

            // Block opening another task while one mini-game is already open.
            if (ActiveOpenTask != null && ActiveOpenTask != this)
                return;

            // Already open — InputHandler owns click routing while UI is shown.
            if (IsUIShown)
                return;

            IsUIShown = true;
            ActiveOpenTask = this;
            m_MiniGameUI.enabled = true;
            StopReadyPulse();
            Debug.Log($"TaskBase: Opened {gameObject.name} mini-game UI.");

            // Call hook for subclasses to set up interaction (callbacks, etc.)
            // IMPORTANT: UIDocument rebuilds its visual tree every enable, so subclasses
            // must re-query elements here every time — never cache across closes.
            OnUIEnabled();
        }

        /// <summary>
        ///     Returns true when the pointer is over this task's UI panel.
        /// </summary>
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (!IsUIShown || m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
                return false;

            var panel = m_MiniGameUI.rootVisualElement.panel;
            if (panel == null)
                return false;

            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            return panel.Pick(panelPosition) != null;
        }

        /// <summary>
        ///     Override this in subclasses to set up event callbacks after UI is enabled
        /// </summary>
        protected virtual void OnUIEnabled()
        {
        }

        /// <summary>
        ///     Override to drop VisualElement refs when the document is closed.
        ///     UIDocument destroys the visual tree on disable, so cached refs go stale.
        /// </summary>
        protected virtual void OnUIClosed()
        {
        }

        /// <summary>
        ///     Schedules task completion after a short delay so win feedback can play.
        ///     Uses a MonoBehaviour coroutine (not UI Toolkit schedule) so disabling the
        ///     UIDocument during close cannot cancel the completion callback.
        /// </summary>
        protected void ScheduleCompletion(long delayMs = 1500)
        {
            if (IsCompleting || !isActiveAndEnabled)
                return;

            IsCompleting = true;
            CancelCompletionSchedule();
            m_CompletionCoroutine = StartCoroutine(CompletionDelay(delayMs / 1000f));
        }

        private IEnumerator CompletionDelay(float seconds)
        {
            // Realtime so pause / timescale 0 cannot stall the win close forever
            yield return new WaitForSecondsRealtime(seconds);
            m_CompletionCoroutine = null;
            CompleteTask();
        }

        /// <summary>
        ///     Hides the UI and disables the task object
        /// </summary>
        protected virtual void CompleteTask()
        {
            if (!m_MiniGameUI) return;

            m_CompletionCoroutine = null;
            IsCompleting = false;
            IsUIShown = false;

            if (ActiveOpenTask == this)
                ActiveOpenTask = null;

            // NEW: Tell the Room UI to clear the maintenance/hazard image
            //m_AssignedRoom.RoomUI.ClearCatActionImage(); 

            // Tell the FSM we are done
            //m_IsCompleted = true;

            OnUIClosed();

            if (m_MiniGameUI)
                m_MiniGameUI.enabled = false;

            gameObject.SetActive(false);

            Debug.Log($"Fixed {gameObject.name}!");
        }

        private void CloseUI(bool clearActiveTask)
        {
            CancelCompletionSchedule();
            IsCompleting = false;
            IsUIShown = false;

            if (clearActiveTask && ActiveOpenTask == this)
                ActiveOpenTask = null;

            if (m_MiniGameUI)
                m_MiniGameUI.enabled = false;

            OnUIClosed();
        }

        private void CancelCompletionSchedule()
        {
            if (m_CompletionCoroutine != null)
            {
                StopCoroutine(m_CompletionCoroutine);
                m_CompletionCoroutine = null;
            }
        }
    }
}
