#region

using Content.Scripts.Managers;
using Content.Scripts.Managers.FSM.Tasks;
using Content.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;

#endregion

namespace Content.Scripts.Input
{
    public sealed class InputHandler : MonoBehaviour
    {
        private InputSystem_Actions m_ActionMap; //input

        private static CameraController CameraController =>
            CameraController.Instance; // Get the singleton instance of CameraController

        private PauseMenu m_PauseManager;

        private void Awake()
        {
            m_ActionMap = new InputSystem_Actions();
            m_PauseManager = GetComponent<PauseMenu>();
        }

        private void OnEnable()
        {
            m_ActionMap.UI.Navigate.performed += HandleNavigatePerformed;
            m_ActionMap.UI.Click.performed += Handle_InteractPerformed;
            m_ActionMap.UI.Cancel.performed += Handle_PausePerformed;

            m_ActionMap.Enable();
        }

        private void OnDisable()
        {
            m_ActionMap.UI.Navigate.performed -= HandleNavigatePerformed;
            m_ActionMap.UI.Click.performed -= Handle_InteractPerformed;
            m_ActionMap.UI.Cancel.performed -= Handle_PausePerformed;

            m_ActionMap.Disable();
        }

        /// <summary>
        ///     Toggles with the pause menu.
        /// </summary>
        /// <param name="obj"></param>
        private void Handle_PausePerformed(InputAction.CallbackContext obj)
        {
            if (!m_PauseManager) return;

            m_PauseManager.TogglePausePanel();
        }

        private void HandleNavigatePerformed(InputAction.CallbackContext context)
        {
            if (!CameraController) return;
            var navigationInput = context.ReadValue<Vector2>();

            // Y > 0 means up navigation
            if (navigationInput.y > 0)
            {
                CameraController.MoveUp();
            }
            // Y < 0 means down navigation
            else if (navigationInput.y < 0)
            {
                CameraController.MoveDown();
            }
        }

        /// <summary>
        ///     Fires when the player clicks the mouse. Shoots a ray to find task objects in the world to make the UI appear.
        /// </summary>
        private void Handle_InteractPerformed(InputAction.CallbackContext context)
        {
            if (!CameraController) return;

            // Gets the camera using the camera manager
            var nativeCamera = CameraController.GetComponent<Camera>();
            if (!nativeCamera) return;

            // Current mouse pos
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            // Converts to usable pos
            Vector3 worldPosition = nativeCamera.ScreenToWorldPoint(mousePosition);

            // Raycast to check click location
            var hit = Physics2D.Raycast(worldPosition, Vector2.zero);

            //Stop if nothing was found
            if (!hit.collider) return;

            // If the raycast object has a task component - interact with it
            if (hit.collider.TryGetComponent<TaskBase>(out var task))
                task.Interact();
        }
    }
}