using UnityEngine;

namespace Content.Scripts.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject m_PausePanel;

        private bool m_PausePanelOpen = false;
    
        public void TogglePausePanel()
        {
            m_PausePanelOpen = !m_PausePanelOpen;
            m_PausePanel.SetActive(m_PausePanelOpen);
        }

        public void CloseMenu()
        {
            m_PausePanelOpen = false;
            m_PausePanel.SetActive(m_PausePanelOpen);
        }
        
        public void Quit()
        => Application.Quit();
    }
}
