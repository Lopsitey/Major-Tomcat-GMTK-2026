#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Maintenance
{
    public sealed class WaterBowl : TaskBase
    {
        [Header("Grid Settings")] [Tooltip("The amount of columns in the UI grid.")] [SerializeField]
        private int m_GridWidth = 5;

        //UI refs
        private readonly List<VisualElement> m_PipeElements = new();
        private PipeNode[] m_PipeNodes;

        //For the DFS search
        private int m_StartIndex;
        private int m_TargetIndex;

        // Dictionary to map UI Builder USS classes directly to the bools
        private readonly Dictionary<string, PipeNode> m_PipeTemplates = new Dictionary<string, PipeNode>
        {
            { "type-cross", new PipeNode(top: true, right: true, bottom: true, left: true) },
            { "type-straight-horizontal", new PipeNode(top: false, right: true, bottom: false, left: true) },
            { "type-straight-vertical", new PipeNode(top: true, right: false, bottom: true, left: false) },
            { "type-corner", new PipeNode(top: false, right: true, bottom: true, left: false) }, // Top-Right bend
            { "type-tjunction", new PipeNode(top: true, right: true, bottom: true, left: false) }, // 3 pointer
            {
                "type-source", new PipeNode(top: true, right: true, bottom: true, left: true)
            }, // The water block for the pipes to use
            {
                "type-end", new PipeNode(top: true, right: true, bottom: true, left: true)
            } // The target for the pipe's water
        };

        protected override void Awake()
        {
            //Uses the parent to initially hide the UI
            base.Awake();

            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null) return;
            var root = m_MiniGameUI.rootVisualElement;

            // Queries pipes in order
            // Assumes they are ordered top-left to bottom-right in the UI builder
            root.Query<VisualElement>(className: "pipe-cell").ToList(m_PipeElements);

            // Initialises the graph data array to match the amount of UI elements
            m_PipeNodes = new PipeNode[m_PipeElements.Count];

            for (int i = 0; i < m_PipeElements.Count; ++i)
            {
                // Needed so the lambda can reference a local instance of i
                // If 'i' was referenced directly the reference would just be the final iteration
                // Only needs to be done in for loops with lambdas, for each doesn't need that  
                int index = i;
                VisualElement pipeVisual = m_PipeElements[i];

                // Default value (empty) - used if no class matches
                m_PipeNodes[i] = new PipeNode(false, false, false, false);

                // Checks UI against the dictionary
                foreach (var pipeType in m_PipeTemplates)
                {
                    // If the UI class matches
                    if (pipeVisual.ClassListContains(pipeType.Key))
                    {
                        // Clone the template so each individual pipe tracks its own rotation separately
                        PipeNode template = pipeType.Value;
                        m_PipeNodes[i] = new PipeNode(template.HasTop, template.HasRight, template.HasBottom,
                            template.HasLeft);
                        break; // Found the matching pipe type class, so stop checking the dictionary
                    }
                }

                // Register the native UI Toolkit click event to rotate this specific pipe
                m_PipeElements[i].RegisterCallback<ClickEvent>(_ => RotatePipe(index));
            }

            // Automatically find where the Tap and Bowl are located in the grid list
            for (int i = 0; i < m_PipeElements.Count; i++)
            {
                if (m_PipeElements[i].ClassListContains("type-source"))
                {
                    m_StartIndex = i;
                }

                if (m_PipeElements[i].ClassListContains("type-end"))
                {
                    m_TargetIndex = i;
                }
            }

            // Call the diagnostic right before Awake finishes
            DiagnosticInitCheck();
        }

        /// <summary>
        ///     Visually rotates the UI element and updates the node data
        /// </summary>
        private void RotatePipe(int index)
        {
            VisualElement pipeVisual = m_PipeElements[index];

            // Reads the current visual angle (which might be mid-animation, e.g., 45.3 degrees)
            float currentVisualAngle = pipeVisual.resolvedStyle.rotate.angle.value;

            // Clamp it to the nearest valid 90-degree increment
            // Example: 45.3 / 90 = 0.503. 0.503 rounded = 1. 1*90=90 which means it's now snapped to an increment of 90
            float clampedAngle = Mathf.Round(currentVisualAngle / 90f) * 90f;

            // Increment the snap to the next 90
            float newAngle = clampedAngle + 90f;

            // Update the UI 
            pipeVisual.style.rotate = new StyleRotate(new Rotate(new Angle(newAngle, AngleUnit.Degree)));

            // Update the data
            m_PipeNodes[index].RotateClockwise();

            if (CheckWinCondition()) HandleWinSequence(); //Won?
        }

        private void HandleWinSequence() //TODO hmmm
        {
            Debug.Log("<color=green>[WaterBowl] PUZZLE SOLVED! Delaying close...</color>");

            // Immediately turn off ray-casting for all pipes so the player can't keep clicking them
            foreach (var pipe in m_PipeElements)
            {
                pipe.pickingMode = PickingMode.Ignore;
            }

            // Use UI Toolkit's native scheduler to wait 1 second before running CompleteTask()
            m_MiniGameUI.rootVisualElement.schedule.Execute(CompleteTask).StartingIn(1500);
        }

        #region Depth-First Search Algorithm

        /// <summary>
        ///     Wrapper method for the DFS. Creates a fresh 'visited' list every time we check the board.
        /// </summary>
        private bool CheckWinCondition()
        {
            // Used to check the pipes aren't a complete circle so it doesn't loop infinitely
            // Hashset - so all the values are unique
            HashSet<int> visited = new HashSet<int>();

            // Start the search from the Tap
            bool isSolved = DFS(m_StartIndex, visited);

            // Paint the UI to show exactly which pipes the DFS added to the visited list
            if (isSolved) DisplayWaterPath(visited);

            return isSolved;
        }

        /// <summary>
        ///     A recursive algorithm that traces the physical path of the pipes.
        ///     It steps from node to node, checking all valid connections, until it hits a dead end or finds the target.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private bool DFS(int currentIndex, HashSet<int> visited)
        {
            // Mark this pipe as visited so we don't walk backwards or loop endlessly.
            visited.Add(currentIndex);

            // Won so return
            if (currentIndex == m_TargetIndex) return true;

            // Checks which pipes are actually physically connected to this one - validates grid edges
            List<int> connectedNeighbors = GetConnectedNeighbors(currentIndex);

            foreach (int neighbor in connectedNeighbors)
            {
                // If we haven't checked this neighbouring pipe yet, dive deeper into it!
                if (!visited.Contains(neighbor))
                {
                    // Recursively call DFS. If this path eventually finds the end, it returns true all the way up the chain.
                    if (DFS(neighbor, visited)) return true;
                }
            }

            // All connections and none of them reached the end, this path is a dead end. Backtrack.
            return false;
        }

        /// <summary>
        ///     Converts the 1D array index into 2D Grid coordinates to see if adjacent pipes have aligned openings.
        /// </summary>
        private List<int> GetConnectedNeighbors(int index)
        {
            List<int> neighbors = new List<int>();
            PipeNode current = m_PipeNodes[index];

            // Turns a 1D index (e.g., 6) into 2D coordinates (X:1, Y:1) based on the grid width
            int x = index % m_GridWidth;
            int y = index / m_GridWidth;

            // Calculate total rows based on element count
            int height = m_PipeElements.Count / m_GridWidth;

            // Ensures not in the top row and that our current pipe is open at the top
            if (y > 0 && current.HasTop)
            {
                int upIndex = index - m_GridWidth; // Move exactly one row up in the 1D array
                // If the pipe above us is open at the bottom, they connect!
                if (m_PipeNodes[upIndex].HasBottom) neighbors.Add(upIndex);
            }

            // Same check for right
            if (x < m_GridWidth - 1 && current.HasRight)
            {
                int rightIndex = index + 1; // Move one index forward
                if (m_PipeNodes[rightIndex].HasLeft) neighbors.Add(rightIndex);
            }

            // Checks bottom
            if (y < height - 1 && current.HasBottom)
            {
                int downIndex = index + m_GridWidth; // Move exactly one row down
                if (m_PipeNodes[downIndex].HasTop) neighbors.Add(downIndex);
            }

            // And left
            if (x > 0 && current.HasLeft)
            {
                int leftIndex = index - 1; // Move one index backward
                if (m_PipeNodes[leftIndex].HasRight) neighbors.Add(leftIndex);
            }

            return neighbors;
        }

        #endregion

        #region Diagnostics & Debugging

        /// <summary>
        ///     Validates the grid math and confirms the start/end points were found correctly.
        /// </summary>
        private void DiagnosticInitCheck()
        {
            Debug.Log(
                $"[WaterBowl Init] Grid Width: {m_GridWidth} | Total Pipes: {m_PipeElements.Count} | Start: {m_StartIndex} | Target: {m_TargetIndex}");

            // Mathematical sanity check: The grid must form a perfect rectangle
            if (m_PipeElements.Count % m_GridWidth != 0)
            {
                Debug.LogWarning(
                    $"<color=orange>[WaterBowl Error] Total pipes ({m_PipeElements.Count}) is not cleanly divisible by Grid Width ({m_GridWidth})! The DFS math will fail.</color>");
            }

            // Logic sanity check: Did we actually find the start and end?
            if (m_StartIndex == 0 && !m_PipeElements[0].ClassListContains("type-source"))
            {
                Debug.LogWarning(
                    "<color=orange>[WaterBowl Error] Start Index is 0, but it lacks the 'type-source' class. Dynamic lookup failed.</color>");
            }
        }

        /// <summary>
        ///     Visually paints the UI Toolkit background colours to show exactly how far the DFS algorithm reached.
        /// </summary>
        /// <param name="visitedPath"></param>
        private void DisplayWaterPath(HashSet<int> visitedPath)
        {
            // Reset all pipes back to a transparent background
            foreach (var pipe in m_PipeElements)
            {
                pipe.style.backgroundColor = new StyleColor(StyleKeyword.Null);
            }

            // Paint the currently connected path a semi-transparent red so you can physically see the logic
            foreach (int index in visitedPath)
            {
                m_PipeElements[index].style.backgroundColor = new StyleColor(new Color(0f, 1f, 0f, 0.4f));
            }
        }

        #endregion
    }

    /// <summary>
    ///     A simple data class representing where a pipe is open (its logical state).
    ///     NOT A STRUCT - no keyword and is mutable (can be changed after creation)
    /// </summary>
    public class PipeNode
    {
        public bool HasTop { get; private set; }
        public bool HasRight { get; private set; }
        public bool HasBottom { get; private set; }
        public bool HasLeft { get; private set; }

        public PipeNode(bool top, bool right, bool bottom, bool left)
        {
            HasTop = top;
            HasRight = right;
            HasBottom = bottom;
            HasLeft = left;
        }

        /// <summary>
        ///     Shifts all boolean values exactly one slot clockwise to simulate a 90-degree rotation.
        /// </summary>
        public void RotateClockwise()
        {
            bool previousTop = HasTop;
            HasTop = HasLeft;
            HasLeft = HasBottom;
            HasBottom = HasRight;
            HasRight = previousTop;
        }
    }
}