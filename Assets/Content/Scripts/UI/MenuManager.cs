#region

using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace Content.Scripts.UI
{
    public sealed class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject m_MenuPanel;
        [SerializeField] private GameObject m_SettingsPanel;

        private bool m_SettingsPanelOpen;

        public void ToggleSettingsPanel()
        {
            if (m_SettingsPanelOpen)
            {
                m_MenuPanel.SetActive(true); //open the menu
                m_SettingsPanel.SetActive(false); //close the settings panel
            }
            else
            {
                m_MenuPanel.SetActive(false); //otherwise close the menu
                m_SettingsPanel.SetActive(true); //and open the settings panel
            }

            m_SettingsPanelOpen = !m_SettingsPanelOpen;
        }

        public void LoadGame()
        {
            SceneManager.LoadScene("MainScene");
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}