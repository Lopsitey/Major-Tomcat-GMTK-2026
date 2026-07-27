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

        // Logical rotation in degrees (0/90/180/270) — do NOT read resolvedStyle (can desync).
        private int[] m_PipeAngles;

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
        }

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null) return;

            // Re-bind every open — UIDocument rebuilds the visual tree on enable
            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_PipeElements.Clear();
            m_PipeNodes = null;
            m_PipeAngles = null;
        }

        protected override void ResetTask()
        {
            // Visual/node reset happens in BindUI after the tree is rebuilt
        }

        private void BindUI(VisualElement root)
        {
            m_PipeElements.Clear();
            root.Query<VisualElement>(className: "pipe-cell").ToList(m_PipeElements);

            m_PipeNodes = new PipeNode[m_PipeElements.Count];
            m_PipeAngles = new int[m_PipeElements.Count];

            for (var i = 0; i < m_PipeElements.Count; ++i)
            {
                var pipeVisual = m_PipeElements[i];

                m_PipeNodes[i] = new PipeNode(false, false, false, false);

                // Straight-vertical uses the horizontal sprite drawn upright via a 90° rotate.
                // Keep visual angle and PipeNode openings in lockstep from the start.
                var initialAngle = 0;
                if (pipeVisual.ClassListContains("type-straight-vertical"))
                    initialAngle = 90;

                m_PipeAngles[i] = initialAngle;

                foreach (var pipeType in m_PipeTemplates)
                    if (pipeVisual.ClassListContains(pipeType.Key))
                    {
                        var template = pipeType.Value;
                        m_PipeNodes[i] = new PipeNode(template.HasTop, template.HasRight, template.HasBottom,
                            template.HasLeft);
                        break;
                    }

                pipeVisual.style.rotate =
                    new StyleRotate(new Rotate(new Angle(initialAngle, AngleUnit.Degree)));
                pipeVisual.pickingMode = PickingMode.Position;
            }

            m_StartIndex = 0;
            m_TargetIndex = 0;
            for (var i = 0; i < m_PipeElements.Count; i++)
            {
                if (m_PipeElements[i].ClassListContains("type-source")) m_StartIndex = i;

                if (m_PipeElements[i].ClassListContains("type-end")) m_TargetIndex = i;
            }

            // Scramble rotatable pipes so the board takes real work to solve.
            // Source/end stay fixed. Keep scrambling until the path is NOT already complete.
            for (var attempt = 0; attempt < 24; attempt++)
            {
                for (var i = 0; i < m_PipeElements.Count; ++i)
                {
                    if (i == m_StartIndex || i == m_TargetIndex)
                        continue;

                    // Crosses are rotationally symmetric for connectivity — skip to bias scramble
                    // onto corners / straights / T-junctions that actually change the maze.
                    if (m_PipeElements[i].ClassListContains("type-cross"))
                        continue;

                    var turns = Random.Range(1, 4); // always at least one turn away from the authored pose
                    for (var t = 0; t < turns; t++)
                        ApplyRotation(i);
                }

                if (!IsPathConnected())
                    break;
            }

            // Clear any leftover paint from connectivity probes
            foreach (var pipe in m_PipeElements)
                pipe.style.backgroundColor = new StyleColor(StyleKeyword.Null);

            for (var i = 0; i < m_PipeElements.Count; ++i)
            {
                var index = i;
                m_PipeElements[i].RegisterCallback<ClickEvent>(_ => RotatePipe(index));
            }

            DiagnosticInitCheck();
        }

        /// <summary>
        ///     Applies one 90° clockwise step to both the visual and the PipeNode data.
        /// </summary>
        private void ApplyRotation(int index)
        {
            m_PipeAngles[index] = (m_PipeAngles[index] + 90) % 360;
            m_PipeElements[index].style.rotate =
                new StyleRotate(new Rotate(new Angle(m_PipeAngles[index], AngleUnit.Degree)));
            m_PipeNodes[index].RotateClockwise();
        }

        /// <summary>
        ///     Visually rotates the UI element and updates the node data
        /// </summary>
        private void RotatePipe(int index)
        {
            if (IsCompleting || m_PipeNodes == null || m_PipeAngles == null)
                return;

            if (index < 0 || index >= m_PipeElements.Count)
                return;

            ApplyRotation(index);

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

            ScheduleCompletion();
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
        ///     Silent connectivity probe used while scrambling (no green paint).
        /// </summary>
        private bool IsPathConnected()
        {
            var visited = new HashSet<int>();
            return DFS(m_StartIndex, visited);
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