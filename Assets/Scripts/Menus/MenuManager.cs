using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject m_menuPanel;
    [SerializeField] private GameObject m_settingsPanel;

    private bool m_settingsPanelOpen = false;

    public void ToggleSettingsPanel() 
    {
        if (m_settingsPanelOpen) 
        {
            m_menuPanel.SetActive(true);//open the menu
            m_settingsPanel.SetActive(false);//close the settings panel
        }
        else 
        {
            m_menuPanel.SetActive(false);//otherwise close the menu
            m_settingsPanel.SetActive(true);//and open the settings panel
        }
        m_settingsPanelOpen = !m_settingsPanelOpen;
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
