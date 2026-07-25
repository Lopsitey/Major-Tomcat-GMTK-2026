#region

using System.Collections;
using Content.Scripts.Managers.FSM.States;
using UnityEngine;
using UnityEngine.UIElements; 
using Unity.Properties;
using Content.Scripts.UI;
#endregion

namespace Content.Scripts.Managers.FSM
{
    //Essentially a wrapped C# classes, like how I had steering behaviours in the AI module - means there's something you can select in the inspector
    public enum CatRoomAssignment { Cockpit, LivingQuarters, ElectricalRoom, EngineRoom }

    [RequireComponent(typeof(UIDocument))] // Forces the Cat Prefab to have a UI
    // ReSharper disable once InconsistentNaming
    
    public class FSM_Manager : MonoBehaviour
    {
       [SerializeField] private UIManager m_GlobalUI;

        [Header("Cat Identity")]
        [SerializeField] private CatRoomAssignment m_AssignedRoom;
        [SerializeField] private GameObject[] m_HazardObjects;
        
        private IdleState m_Idle;
        
        private StateBase m_MaintenanceTask; 
        private StateBase m_CurrentState;

        //UI Toolkit Data Binding Properties 
        //These replace the need for Update() polling completely
        private UIDocument m_LocalUIDoc;
        private ProgressBar m_FloatingPatienceBar;

        [CreateProperty] public float PatienceRemaining { get; private set; }

        private bool m_IsPatienceBarVisible;
        public bool IsPatienceBarVisible
        {
            get => m_IsPatienceBarVisible;
            set
            {
                if (m_IsPatienceBarVisible == value) return;
                m_IsPatienceBarVisible = value;
                
                // Toggle CSS class directly on the Cat's personal UI
                if (m_FloatingPatienceBar != null)
                {
                    if (m_IsPatienceBarVisible)
                        m_FloatingPatienceBar.RemoveFromClassList("hidden");//uses CSS to hide the UI
                    else
                        m_FloatingPatienceBar.AddToClassList("hidden");
                }
            }
        }
        
        protected void Awake()
        {
            // States are plain C# classes that use a ref to this script - no extra comps needed
            m_Idle = new IdleState(this, null); // No hazards in idle state

            m_MaintenanceTask = m_AssignedRoom switch
            {
                CatRoomAssignment.Cockpit => new Cockpit(this, m_HazardObjects),
                CatRoomAssignment.LivingQuarters => new LivingQuarters(this, m_HazardObjects),
                CatRoomAssignment.ElectricalRoom => new ElectricalRoom(this, m_HazardObjects),
                _ => new EngineRoom(this, m_HazardObjects)
            };

            // Setup Local UI DataBinding
            m_LocalUIDoc = GetComponent<UIDocument>();
            if (m_LocalUIDoc && m_LocalUIDoc.rootVisualElement != null)
            {
                m_FloatingPatienceBar = m_LocalUIDoc.rootVisualElement.Q<ProgressBar>("Patience-Bar");
                
                // Bind the floating UI directly to this specific Cat's properties
                var patienceBinding = new DataBinding
                {
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(PatienceRemaining)),
                    bindingMode = BindingMode.ToTarget,
                    updateTrigger = BindingUpdateTrigger.OnSourceChanged
                };
                m_FloatingPatienceBar?.SetBinding("value", patienceBinding);
            }
        }

        // Needed because most of this relies on the variables initialised in Awake
        private void Start() => StartCoroutine(CatBrainLoop());

        private IEnumerator CatBrainLoop()
        {
            while (true)
            {
                // Idle by default
                SwitchState(m_Idle);
                
                // Hides the bar
                IsPatienceBarVisible = false; 
                
                yield return new WaitForSeconds(Random.Range(5f, 10f));

                // Maintenance RNG - calls enter on the room and starts the task
                SwitchState(m_MaintenanceTask);
                //Displays the bar
                IsPatienceBarVisible = true;  
                
                float patienceLimit = 60f; //player gets 1 min to fix the issue
                float timeInState = 0f;

                // Runs while time remains and the task isn't done yet
                while (timeInState < patienceLimit && !m_CurrentState.IsComplete)
                {
                    timeInState += Time.deltaTime;
                    
                    //updates UI
                    PatienceRemaining = 1.0f - (timeInState / patienceLimit); 
                    
                    yield return null; 
                }

                //If time runs out while the task is incomplete
                if (!m_CurrentState.IsComplete)
                {
                   //Escalates the hazard for the specific room
                    m_CurrentState.EscalateHazard();
                    
                    //Updates the main HUD if it exists
                    if (m_GlobalUI) m_GlobalUI.ActiveCleanupTasks++;

                    Debug.Log($"Successfully handled {m_CurrentState.GetType().Name}. Returning to idle!");
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void SwitchState(StateBase newState)
        {
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