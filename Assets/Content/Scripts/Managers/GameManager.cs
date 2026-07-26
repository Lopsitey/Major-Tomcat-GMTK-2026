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

        private int m_ActiveLivingQuartersTasks;
        private int m_ActiveElectricalTasks;
        private int m_ActiveCockpitTasks;
        private int m_ActiveEngineRoomTasks;
        public bool GameEnded { get; private set; }

        //A public property the FSM can read to know if it's allowed to spawn tasks
        public bool IsInFinalCountdown => m_TimeRemaining <= 10f;

        //Total of all active tasks across the entire ship
        public int TotalActiveTasks { get; private set; }

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

            // 2. NEW: Liftoff Timer Logic
            if (m_TimeRemaining > 0)
            {
                m_TimeRemaining -= Time.deltaTime;

                // The exact frame the timer hits 0, evaluate the win/loss condition
                if (m_TimeRemaining <= 0)
                    HandleLiftoffSequence();
            }
        }

        public void RegisterTaskState(CatRoomAssignment room, bool isStarting)
        {
            var modifier = isStarting ? 1 : -1;

            // Tracks the absolute total of active tasks for the win condition check
            TotalActiveTasks += modifier;

            switch (room)
            {
                case CatRoomAssignment.LivingQuarters:
                    m_ActiveLivingQuartersTasks += modifier;
                    break;
                case CatRoomAssignment.ElectricalRoom:
                    m_ActiveElectricalTasks += modifier;
                    break;
                case CatRoomAssignment.Cockpit:
                    m_ActiveCockpitTasks += modifier;
                    break;
                case CatRoomAssignment.EngineRoom:
                    m_ActiveEngineRoomTasks += modifier;
                    break;
            }

            //Updates the global HUD
            if (GlobalUI) GlobalUI.ActiveCleanupTasks++; //Gets the global UI manager singleton
        }

        // The final evaluation
        private void HandleLiftoffSequence()
        {
            GameEnded = true;

            // If any task is open during liftoff, you fail. Otherwise, you win.
            if (TotalActiveTasks > 0)
                Debug.Log(
                    "<color=red>[GameManager] GAME OVER! A maintenance task was left unfinished during liftoff!</color>");
            // TODO: Trigger game over UI/Scene transition here
            else
                Debug.Log(
                    "<color=green>[GameManager] YOU WIN! Liftoff successful. The ship is fully operational!</color>");
            // TODO: Trigger victory UI/Scene transition here
        }
    }
}