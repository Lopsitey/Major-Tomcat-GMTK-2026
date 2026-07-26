#region

using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class TaskBase : MonoBehaviour
    {
        protected UIDocument m_MiniGameUI;
        private bool m_IsUIShown;

        protected virtual void Awake()
        {
            // Search this GameObject first, then children
            m_MiniGameUI = GetComponent<UIDocument>() ?? GetComponentInChildren<UIDocument>();
            m_IsUIShown = false;
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
            m_IsUIShown = false;
            if (!m_MiniGameUI) return;
            m_MiniGameUI.enabled = false;
            ResetTask();
        }

        protected virtual void OnDisable()
        {
            // Always disable UIDocument when task is deactivated
            m_IsUIShown = false;
            if (!m_MiniGameUI) return;
            m_MiniGameUI.enabled = false;
        }

        protected virtual void Update()
        {
            // Safety check: ensure UIDocument matches our tracked state
            if (m_MiniGameUI && m_MiniGameUI.enabled != m_IsUIShown) m_MiniGameUI.enabled = m_IsUIShown;
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

            // The player clicked the task - enable the UIDocument to show it
            m_IsUIShown = true;
            m_MiniGameUI.enabled = true;
            Debug.Log($"TaskBase: Opened {gameObject.name} mini-game UI.");

            // Call hook for subclasses to set up interaction (callbacks, etc.)
            OnUIEnabled();
        }

        /// <summary>
        ///     Override this in subclasses to set up event callbacks after UI is enabled
        /// </summary>
        protected virtual void OnUIEnabled()
        {
        }

        /// <summary>
        ///     Hides the UI and disables the task object
        /// </summary>
        protected virtual void CompleteTask()
        {
            if (!m_MiniGameUI) return;

            // Disable the UIDocument
            m_IsUIShown = false;
            m_MiniGameUI.enabled = false;
            gameObject.SetActive(false);

            // NEW: Tell the Room UI to clear the maintenance/hazard image
            //m_AssignedRoom.RoomUI.ClearCatActionImage(); 

            // Tell the FSM we are done
            //m_IsCompleted = true;

            Debug.Log($"Fixed {gameObject.name}!");
        }
    }
}