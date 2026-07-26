#region

using Content.Scripts.Managers.FSM;
using Content.Scripts.UI;
using UnityEngine;

#endregion

namespace Content.Scripts.Managers
{
    public sealed class GameManager : Singleton<GameManager>
    {
        private static UIManager GlobalUI => UIManager.Instance;

        [Header("Game Settings")]
        [SerializeField]
        private float m_TimeRemaining = 30f;

        [Header("Rocket Animation")]
        [SerializeField]
        private Animator m_RocketAnimator;

        private int m_ActiveLivingQuartersTasks;
        private int m_ActiveElectricalTasks;
        private int m_ActiveCockpitTasks;
        private int m_ActiveEngineRoomTasks;

        public bool GameEnded { get; private set; }

        public bool IsInFinalCountdown => m_TimeRemaining <= 10f;

        public int TotalActiveTasks { get; private set; }

        private void Update()
        {
            // Don't continue counting once the game has ended.
            if (GameEnded)
                return;

            // Decrement the clock.
            m_TimeRemaining -= Time.deltaTime;

            // Update the UI countdown.
            GlobalUI.LaunchCountdown = m_TimeRemaining;

            // Check for liftoff.
            if (m_TimeRemaining <= 0)
            {
                HandleLiftoffSequence();
            }
        }

        public void RegisterTaskState(
            CatRoomAssignment room,
            bool isStarting)
        {
            var modifier = isStarting ? 1 : -1;

            // Track total active tasks.
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

            // Update global HUD.
            if (GlobalUI)
            {
                GlobalUI.ActiveCleanupTasks += modifier;
            }
        }

        private void HandleLiftoffSequence()
        {
            // Prevent this method from being called multiple times.
            if (GameEnded)
                return;

            GameEnded = true;

            // Check if any tasks remain.
            if (TotalActiveTasks > 0)
            {
                Debug.Log(
                    "<color=red>[GameManager] GAME OVER! " +
                    "A maintenance task was left unfinished during liftoff!" +
                    "</color>"
                );

                PlayLoseAnimation();
            }
            else
            {
                Debug.Log(
                    "<color=green>[GameManager] YOU WIN! " +
                    "Liftoff successful. The ship is fully operational!" +
                    "</color>"
                );

                PlayWinAnimation();
            }
        }

        private void PlayWinAnimation()
        {
            if (m_RocketAnimator == null)
            {
                Debug.LogWarning(
                    "[GameManager] No Rocket Animator assigned."
                );

                return;
            }

            m_RocketAnimator.SetTrigger("Win");
        }

        private void PlayLoseAnimation()
        {
            if (m_RocketAnimator == null)
            {
                Debug.LogWarning(
                    "[GameManager] No Rocket Animator assigned."
                );

                return;
            }

            m_RocketAnimator.SetTrigger("Lose");
        }
    }
}