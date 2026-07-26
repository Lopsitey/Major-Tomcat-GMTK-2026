#region

using Content.Scripts.Managers.FSM;
using Content.Scripts.UI;
using UnityEngine;

#endregion

namespace Content.Scripts.Managers
{
    public sealed class GameManager : Singleton<GameManager>
    {
        private static UIManager GlobalUI => UIManager.Instance; //Gets the global UI manager singleton
        private float m_TimeRemaining = 300f; // 5 minutes

        //Global stats
        public float RoomHappiness { get; set; } = 100f;
        public float RoomFood { get; set; } = 100f;
        public float RoomCleanliness { get; set; } = 100f;

        private void Update()
        {
            // Decrements the clock
            m_TimeRemaining -= Time.deltaTime;

            // Sets the property in the UI
            GlobalUI.LaunchCountdown = m_TimeRemaining;

            if (m_TimeRemaining <= 0)
            {
                //LaunchRocket();
            }
        }

        public void RegisterTaskState(CatRoomAssignment assignedRoom, bool b)
        {
        }
    }
}