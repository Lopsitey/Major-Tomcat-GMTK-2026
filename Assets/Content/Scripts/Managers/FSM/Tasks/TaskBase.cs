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

        protected virtual void Awake()
        {
            m_MiniGameUI = GetComponent<UIDocument>();
            if (!m_MiniGameUI || m_MiniGameUI.rootVisualElement == null) return;

            // Ensure the mini-game UI is hidden when the object first wakes up
            m_MiniGameUI.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     Called via the New Input System Raycast in InputHandler.cs
        /// </summary>
        public virtual void Interact()
        {
            if (!m_MiniGameUI || m_MiniGameUI.rootVisualElement == null) return;

            // The player clicked the task - show the mini-game UI
            m_MiniGameUI.rootVisualElement.style.display = DisplayStyle.Flex;
            Debug.Log($"Opened {gameObject.name} mini-game UI.");
        }

        /// <summary>
        ///     Hides the UI and disables the task object
        /// </summary>
        protected virtual void CompleteTask()
        {
            if (!m_MiniGameUI || m_MiniGameUI.rootVisualElement == null) return;

            // Hide the popup mini-game UI
            m_MiniGameUI.rootVisualElement.style.display = DisplayStyle.None;
            gameObject.SetActive(false);

            // NEW: Tell the Room UI to clear the maintenance/hazard image
            //m_AssignedRoom.RoomUI.ClearCatActionImage(); 

            // Tell the FSM we are done
            //m_IsCompleted = true;

            Debug.Log($"Fixed {gameObject.name}!");
        }
    }
}