#region
using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
#endregion

namespace Content.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Document")] 
        [SerializeField] private UIDocument m_UIDoc;

        // UI Element References
        private VisualElement m_UIRoot;
        private Label m_CountdownLabel;
        
        private ProgressBar m_HappinessBar;
        private ProgressBar m_FoodBar;
        private ProgressBar m_CleanlinessBar;
        
        private VisualElement m_DangerWarningOverlay;

        // --------------------------------------------------------
        // 1. BINDABLE PROPERTIES (The Modern Way)
        // External scripts (TaskManager, CatManager) simply update these values.
        // Because of [CreateProperty], the UI will update itself automatically!
        // --------------------------------------------------------
        
        [CreateProperty] public float LaunchCountdown { get; set; } = 300f; // e.g., 5 mins
        [CreateProperty] public float CatHappiness { get; set; } = 100f;
        [CreateProperty] public float CatFood { get; set; } = 100f;
        [CreateProperty] public float CatCleanliness { get; set; } = 100f;

        // --------------------------------------------------------
        // 2. SIDE-EFFECT PROPERTIES
        // When we need visual changes (like toggling CSS classes), 
        // we use a standard property setter instead of "dummy" bindings.
        // --------------------------------------------------------
        
        private int m_ActiveCleanupTasks;
        
        [CreateProperty]
        public int ActiveCleanupTasks
        {
            get => m_ActiveCleanupTasks;
            set
            {
                if (m_ActiveCleanupTasks == value) return;
                m_ActiveCleanupTasks = value;
                
                // Trigger visual changes instantly when this value changes
                UpdateDangerUI();
            }
        }
        
        // 1. The backing field
        private bool m_IsPatienceBarVisible;

        // 2. The Property Setter that triggers the visual update
        public bool IsPatienceBarVisible
        {
            get => m_IsPatienceBarVisible;
            set
            {
                // Prevent redundant updates
                if (m_IsPatienceBarVisible == value) return;
        
                m_IsPatienceBarVisible = value;
        
                // Trigger the visual UI Toolkit update method
                UpdatePatienceBarVisibility(m_IsPatienceBarVisible);
            }
        }

        // 3. The Visual Update Method
        private void UpdatePatienceBarVisibility(bool isVisible)
        {
            // Assuming you have a reference to your UIManager or the VisualElement directly
            if (isVisible)
            {
                // Remove the USS class that hides the element
                // m_PatienceBarElement.RemoveFromClassList("hidden");
            }
            else
            {
                // Add the USS class (e.g., .hidden { display: none; })
                // m_PatienceBarElement.AddToClassList("hidden");
            }
        }

        public void Awake()
        {
            if (!m_UIDoc)
            {
                Debug.LogError("No UIDoc found on the UI manager");
                return;
            }

            m_UIRoot = m_UIDoc.rootVisualElement;

            // Query the UI elements (Ensure these match your UI Builder names)
            m_CountdownLabel = m_UIRoot.Q<Label>("Countdown-Label");
            m_HappinessBar = m_UIRoot.Q<ProgressBar>("Happiness-Bar");
            m_FoodBar = m_UIRoot.Q<ProgressBar>("Food-Bar");
            m_CleanlinessBar = m_UIRoot.Q<ProgressBar>("Cleanliness-Bar");
            m_DangerWarningOverlay = m_UIRoot.Q<VisualElement>("Danger-Overlay");

            SetupBindings();
        }

        private void SetupBindings()
        {
            // --- COUNTDOWN TIMER ---
            var countdownBinding = new DataBinding
            {
                dataSource = this,
                dataSourcePath = new PropertyPath(nameof(LaunchCountdown)),
                bindingMode = BindingMode.ToTarget,
                updateTrigger = BindingUpdateTrigger.OnSourceChanged
            };

            // Converter: Turns the raw float (e.g. 125.5f) into a punchy string: "T-MINUS 02:05"
            countdownBinding.sourceToUiConverters.AddConverter((ref float timeInSeconds) =>
            {
                TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, timeInSeconds));
                // If under 10 seconds, maybe just show seconds for a punchier panic effect!
                if (time.TotalSeconds <= 10) return $"T-MINUS {time.Seconds:D2} !!!";
                
                return $"T-MINUS {time.Minutes:D2}:{time.Seconds:D2}";
            });

            m_CountdownLabel.SetBinding("text", countdownBinding);

            // --- HAPPINESS BAR ---
            var happinessBinding = new DataBinding
            {
                dataSource = this,
                dataSourcePath = new PropertyPath(nameof(CatHappiness)),
                bindingMode = BindingMode.ToTarget,
                updateTrigger = BindingUpdateTrigger.OnSourceChanged
            };
            
            // Optional Converter: If your UI Toolkit progress bar expects 0-100, and your game uses 0-100, 
            // you don't even need a converter! But if you need to map it, you do it here.
            m_HappinessBar.SetBinding("value", happinessBinding);
            
            // --- FOOD BAR ---
            var foodBinding = new DataBinding
            {
                dataSource = this,
                dataSourcePath = new PropertyPath(nameof(CatFood)),
                bindingMode = BindingMode.ToTarget,
                updateTrigger = BindingUpdateTrigger.OnSourceChanged
            };
            m_FoodBar.SetBinding("value", foodBinding);
        }

        /// <summary>
        /// This handles CSS visual toggles based on the amount of active cleanups
        /// </summary>
        private void UpdateDangerUI()
        {
            if (m_DangerWarningOverlay == null) return;

            // If there is 1 or more cleanup task (vomit, wee, broken wire), the ship is in danger!
            bool isInDanger = m_ActiveCleanupTasks > 0;
            
            // Toggles a CSS class that could make the screen flash red or shake
            m_DangerWarningOverlay.EnableInClassList("danger-flash-active", isInDanger);
        }
    }
}