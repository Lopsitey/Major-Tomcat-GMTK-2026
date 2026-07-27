#region

using System;
using System.Collections.Generic;
using Content.Scripts.Managers;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))] //Forced to have the comp attached
    public sealed class UIManager : Singleton<UIManager>
    {
        private UIDocument m_UIDoc;

        // UI Element References
        private VisualElement m_UIRoot;
        private Label m_CountdownLabel;
        private VisualElement m_DangerWarningOverlay;

        private Button m_PauseButton; // Added Global Pause Button

        //BINDABLE PROPERTIES
        // External scripts (TaskManager, CatManager) simply update these values
        // Because of [CreateProperty], the UI will update itself automatically

        [CreateProperty] public float LaunchCountdown { get; set; } = 120f;

        //SIDE-EFFECT PROPERTIES
        //Use a standard property setter visual changes (CSS stuff)

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

        private Button m_UpArrowButton;
        private Button m_DownArrowButton;

        private VisualElement m_PatienceBarsContainer;
        private readonly Dictionary<ProgressBar, VisualElement> m_PatienceBarContainers = new();

        private static CameraController CameraController =>
            CameraController.Instance; //Sets the camera controller using the Singleton instance

        private PauseMenu m_PauseMenu;

        protected override void Awake()
        {
            //Calls the singleton awake to ensure there are no duplicates of this manager
            base.Awake();

            m_PauseMenu = GetComponent<PauseMenu>();
            m_UIDoc = GetComponent<UIDocument>();
            if (!m_UIDoc) return;
            m_UIRoot = m_UIDoc.rootVisualElement;

            // Query the UI elements
            m_CountdownLabel = m_UIRoot.Q<Label>("Countdown-Label");
            m_DangerWarningOverlay = m_UIRoot.Q<VisualElement>("Danger-Overlay");

            SetupBindings();
        }

        private void Start()
        {
            //Query the buttons
            m_PauseButton = m_UIRoot.Q<Button>("Pause-Button");
            m_UpArrowButton = m_UIRoot.Q<Button>("Arrow-Up");
            m_DownArrowButton = m_UIRoot.Q<Button>("Arrow-Down");

            //Query the patience bars container
            m_PatienceBarsContainer = m_UIRoot.Q<VisualElement>("Patience-Bars-Container");

            //Register the events
            if (m_PauseButton != null && m_PauseMenu) m_PauseButton.clicked += m_PauseMenu.TogglePausePanel;

            if (m_UpArrowButton != null && CameraController != null)
                m_UpArrowButton.clicked += CameraController.MoveUp;

            if (m_DownArrowButton != null && CameraController != null)
                m_DownArrowButton.clicked += CameraController.MoveDown;
        }

        private void SetupBindings()
        {
            //These replace the need for Update() polling completely
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
                // If under 30 seconds, displays the seconds and milliseconds to make the player panic more
                if (time.TotalSeconds <= 30) return $"T-MINUS {time.Seconds:D2}{time.Milliseconds:D3} !!!";
                // Under 10 seconds and display the ticks to try and make it even more dramatic
                if (time.TotalSeconds <= 10)
                    return
                        $"T-MINUS {time.Seconds:D2}{time.Milliseconds:D3}{time.Ticks % TimeSpan.TicksPerSecond:D4} !!!!!!"; //D4 is 4 decimal places that get displayed

                return $"T-MINUS {time.Minutes:D2}:{time.Seconds:D2}";
            });

            m_CountdownLabel.SetBinding("text", countdownBinding);
        }

        /// <summary>
        ///     This handles CSS visual toggles based on the amount of active clean-ups
        /// </summary>
        private void UpdateDangerUI()
        {
            if (m_DangerWarningOverlay == null) return;

            // If there is 1 or more clean-up task, the ship is in danger
            bool isInDanger = m_ActiveCleanupTasks > 0;

            // Toggles a CSS class that could make the screen go red
            m_DangerWarningOverlay.EnableInClassList("danger-flash-active", isInDanger);
        }

        /// <summary>
        ///     Creates and adds a patience bar with a room label to the UI
        /// </summary>
        public ProgressBar AddPatienceBar(string roomName)
        {
            if (m_PatienceBarsContainer == null) return null;

            // Create container for label and progress bar. A dark pill sits behind the
            // label text only so it stays legible over the bright sky/cloud backdrop.
            var barContainer = new VisualElement();
            barContainer.AddToClassList("patience-bar-entry");

            // Create and add room label
            var roomLabel = new Label(roomName);
            roomLabel.AddToClassList("patience-bar-label");
            barContainer.Add(roomLabel);

            // Create and add ProgressBar
            var progressBar = new ProgressBar();
            progressBar.AddToClassList("patience-bar");
            progressBar.lowValue = 0f;
            progressBar.highValue = 100f;
            progressBar.value = 100f;
            barContainer.Add(progressBar);

            m_PatienceBarsContainer.Add(barContainer);
            m_PatienceBarContainers[progressBar] = barContainer;

            return progressBar;
        }

        /// <summary>
        ///     Removes a patience bar from the UI
        /// </summary>
        public void RemovePatienceBar(ProgressBar progressBar)
        {
            if (progressBar == null || m_PatienceBarsContainer == null) return;

            if (m_PatienceBarContainers.TryGetValue(progressBar, out var barContainer))
            {
                m_PatienceBarsContainer.Remove(barContainer);
                m_PatienceBarContainers.Remove(progressBar);
            }
        }
    }
}