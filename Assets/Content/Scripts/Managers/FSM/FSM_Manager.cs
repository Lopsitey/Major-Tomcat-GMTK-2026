#region

using System.Collections;
using Content.Scripts.Managers.FSM.States;
using Content.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM
{
    //Essentially a wrapped C# classes, like how I had steering behaviours in the AI module - means there's something you can select in the inspector
    public enum CatRoomAssignment
    {
        Cockpit,
        LivingQuarters,
        ElectricalRoom,
        EngineRoom
    }

    // ReSharper disable once InconsistentNaming
    public sealed class FSM_Manager : MonoBehaviour
    {
        [SerializeField] private CatRoomAssignment m_AssignedRoom;

        [SerializeField] private GameObject[] m_MaintenanceObjects;
        [SerializeField] private GameObject[] m_HazardObjects; //Escalations
        [SerializeField] private float m_MaxPatience = 20f;

        public GameObject[] MaintenanceObjects => m_MaintenanceObjects;
        public GameObject[] HazardObjects => m_HazardObjects;

        // Blackboard Pattern Getters - allows states to access these centralised resources
        // This uses the cool object property passed into the constructors

        private static UIManager GlobalUI => UIManager.Instance; //Gets the global UI manager singleton

        private IdleState m_Idle;

        private StateBase m_MaintenanceTask;
        private StateBase m_CurrentState;

        private ProgressBar m_PatienceBar;

        private void Awake()
        {
            // States are plain C# classes that use a ref to this script - no extra comps needed
            m_Idle = new IdleState(); // No hazards in idle state

            m_MaintenanceTask = m_AssignedRoom switch
            {
                CatRoomAssignment.Cockpit => new Cockpit(this),
                CatRoomAssignment.LivingQuarters => new LivingQuarters(this),
                CatRoomAssignment.ElectricalRoom => new ElectricalRoom(this),
                _ => new EngineRoom(this)
            };
        }

        private void Start()
        {
            print("aa");
            StartCoroutine(CatBrainLoop());
        }

        private IEnumerator CatBrainLoop()
        {
            while (true)
            {
                // Idle by default
                SwitchState(m_Idle);

                yield return new WaitForSeconds(Random.Range(5f, 10));

                // Prevent new tasks in the final 10 seconds.
                // If we are in the final countdown, skip spawning a task and loop back to the top (continue idling).
                if (GameManager.Instance != null && GameManager.Instance.IsInFinalCountdown)
                {
                    Debug.Log("[FSM] Final 10 seconds reached. Cat is blocked from starting new tasks.");
                    continue;
                }

                // Maintenance RNG - calls enter on the room and starts the task
                SwitchState(m_MaintenanceTask);

                // Remove any existing bar before creating a new one
                if (m_PatienceBar != null)
                {
                    GlobalUI.RemovePatienceBar(m_PatienceBar);
                    m_PatienceBar = null;
                }

                // Create patience bar via UIManager
                m_PatienceBar = GlobalUI.AddPatienceBar(m_AssignedRoom.ToString());

                //Tells the GM the task is starting
                GameManager.Instance.RegisterTaskState(m_AssignedRoom, true);

                float timeInState = 0f;

                // Runs while time remains and the task isn't done yet
                while (timeInState < m_MaxPatience && !m_CurrentState.IsComplete)
                {
                    timeInState += Time.deltaTime;
                    m_CurrentState.CheckTaskCompletion();

                    // If task completed, remove patience bar immediately
                    if (m_CurrentState.IsComplete && m_PatienceBar != null)
                    {
                        GlobalUI.RemovePatienceBar(m_PatienceBar);
                        m_PatienceBar = null;
                        break;
                    }

                    // Update the patience bar directly
                    if (m_PatienceBar != null)
                    {
                        var remainingPercentage = (1f - timeInState / m_MaxPatience) * 100f;
                        m_PatienceBar.value = remainingPercentage;
                    }

                    yield return null;
                }

                // Remove the ProgressBar via UIManager (if not already removed)
                if (m_PatienceBar != null)
                {
                    GlobalUI.RemovePatienceBar(m_PatienceBar);
                    m_PatienceBar = null;
                }

                //Tells the GM the task is ending
                GameManager.Instance.RegisterTaskState(m_AssignedRoom, false);

                //Avoids hazard escalation if the task is complete
                if (m_CurrentState.IsComplete)
                {
                    Debug.Log($"Successfully handled {m_CurrentState.GetType().Name}. Returning to idle!");
                    continue;
                }

                //Escalates the hazard for the specific room
                m_CurrentState.EscalateHazard();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void SwitchState(StateBase newState)
        {
            Debug.Log($"Switched state!");
            // Ignore invalid states and prevent swapping to the same state
            if (newState == null || m_CurrentState == newState)
                return;

            // Exit the old state first if not null
            m_CurrentState?.Exit();

            // Assigns and enters the new state
            m_CurrentState = newState;
            m_CurrentState.Enter();
        }
    }
}