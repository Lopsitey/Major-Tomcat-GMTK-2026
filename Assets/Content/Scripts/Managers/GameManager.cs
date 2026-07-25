using Content.Scripts.UI;
using UnityEngine;

namespace Content.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private UIManager m_UI;
        private float m_TimeRemaining = 300f; // 5 minutes

        private void Update()
        {
            // Decrements the clock
            m_TimeRemaining -= Time.deltaTime;
        
            // Sets the property in the UI
            m_UI.LaunchCountdown = m_TimeRemaining;
        
            if (m_TimeRemaining <= 0)
            {
                //LaunchRocket();
            }
        }
    }
}
