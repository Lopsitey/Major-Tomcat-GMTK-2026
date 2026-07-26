#region

using UnityEngine;

#endregion

namespace Content.Scripts.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject m_PausePanel;

        private bool m_PausePanelOpen;

        public void TogglePausePanel()
        {
            m_PausePanelOpen = !m_PausePanelOpen;
            m_PausePanel.SetActive(m_PausePanelOpen);

            // Toggle timescale between 0f (paused) and 1f (running)
            Time.timeScale = m_PausePanelOpen ? 0f : 1f;
        }

        public void Quit()
            => Application.Quit();
    }
}