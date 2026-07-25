using Content.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Content.Scripts.Input
{
	public class InputHandler : MonoBehaviour
	{
	    private InputSystem_Actions m_ActionMap; //input
	    [SerializeField] private PauseMenu m_PauseManager; 
		
		private void Awake()
		{
			m_ActionMap = new InputSystem_Actions();
		}
		
		private void OnEnable()
		{
			m_ActionMap.Enable();

			m_ActionMap.UI.Navigate.performed += HandleNavigatePerformed;
			m_ActionMap.UI.Navigate.canceled += Handle_NavigateCanceled;
			m_ActionMap.UI.Cancel.performed += Handle_PausePerformed;
		}

		private void OnDisable()
		{
			m_ActionMap.Disable();

			m_ActionMap.UI.Navigate.performed -= HandleNavigatePerformed;
			m_ActionMap.UI.Navigate.canceled -= Handle_NavigateCanceled;
			m_ActionMap.UI.Cancel.performed -= Handle_PausePerformed;
		}

		/// <summary>
		///Toggles with the pause menu.
		/// </summary>
		/// <param name="obj"></param>
		private void Handle_PausePerformed(InputAction.CallbackContext obj)
		{
			if (!m_PauseManager) return;
			m_PauseManager.TogglePausePanel();
		}

		private void HandleNavigatePerformed(InputAction.CallbackContext context)
		{
			return;
		}

		private void Handle_NavigateCanceled(InputAction.CallbackContext context)
		{
			return;
		}
	}
}