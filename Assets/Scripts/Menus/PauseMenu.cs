using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject m_pausePanel;

    private bool m_pausePanelOpen = false;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_pausePanelOpen)
            {
                m_pausePanel.SetActive(false);//close the pause menu
            }
            else
            {
                m_pausePanel.SetActive(true);//open the pause menu
            }
            m_pausePanelOpen = !m_pausePanelOpen;
        }
    }
    public void CloseMenu() 
    {
        m_pausePanel.SetActive(false);//closes the menu via back button press
    }
    public void Quit()
    {
        Application.Quit();
    }
}
