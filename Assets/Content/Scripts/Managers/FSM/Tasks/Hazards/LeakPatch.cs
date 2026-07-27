#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

#endregion

namespace Content.Scripts.Managers.FSM.Tasks.Hazards
{
    /// <summary>
    ///     Engine hazard: claw holes are venting coolant. Pick a patch from the rack, then click the
    ///     hole it actually fits — the sizes have to match or the patch blows straight back off.
    /// </summary>
    public sealed class LeakPatch : TaskBase
    {
        // Diameter in pixels for each size class; index doubles as the matching key
        private static readonly float[] SizeDiameters = { 36f, 52f, 68f, 86f };

        // UI refs — rebuilt every OnUIEnabled because UIDocument destroys the tree on disable
        private readonly List<VisualElement> m_Holes = new();
        private readonly List<VisualElement> m_Patches = new();
        private readonly List<int> m_HoleSizes = new();
        private readonly List<int> m_PatchSizes = new();
        private readonly List<bool> m_HoleSealed = new();
        private readonly List<bool> m_PatchUsed = new();

        private int m_SelectedPatch = -1;
        private int m_SealedCount;
        private Label m_Status;

        protected override void OnUIEnabled()
        {
            if (m_MiniGameUI == null || m_MiniGameUI.rootVisualElement == null)
            {
                Debug.LogError("LeakPatch: UIDocument not found!");
                return;
            }

            BindUI(m_MiniGameUI.rootVisualElement);
        }

        protected override void OnUIClosed()
        {
            m_Holes.Clear();
            m_Patches.Clear();
            m_HoleSizes.Clear();
            m_PatchSizes.Clear();
            m_HoleSealed.Clear();
            m_PatchUsed.Clear();
            m_Status = null;
        }

        protected override void ResetTask()
        {
            m_SelectedPatch = -1;
            m_SealedCount = 0;
        }

        private void BindUI(VisualElement root)
        {
            m_Holes.Clear();
            m_Patches.Clear();
            m_HoleSizes.Clear();
            m_PatchSizes.Clear();
            m_HoleSealed.Clear();
            m_PatchUsed.Clear();
            m_SelectedPatch = -1;
            m_SealedCount = 0;

            m_Status = root.Q<Label>("leak-status");
            root.Query<VisualElement>(className: "leak-hole").ToList(m_Holes);
            root.Query<VisualElement>(className: "leak-patch").ToList(m_Patches);

            if (m_Holes.Count == 0 || m_Holes.Count != m_Patches.Count)
            {
                Debug.LogError(
                    $"LeakPatch: Hole and patch counts must match and be non-zero (holes {m_Holes.Count}, patches {m_Patches.Count}).");
                return;
            }

            var count = Mathf.Min(m_Holes.Count, SizeDiameters.Length);

            // Holes get one of each size in a shuffled order; patches get their own shuffle
            var holeOrder = BuildShuffledSizes(count);
            var patchOrder = BuildShuffledSizes(count);

            for (var i = 0; i < count; i++)
            {
                var holeIndex = i;
                var patchIndex = i;

                m_HoleSizes.Add(holeOrder[i]);
                m_PatchSizes.Add(patchOrder[i]);
                m_HoleSealed.Add(false);
                m_PatchUsed.Add(false);

                ApplyDiameter(m_Holes[i], SizeDiameters[holeOrder[i]]);
                ApplyDiameter(m_Patches[i], SizeDiameters[patchOrder[i]]);

                m_Holes[i].RemoveFromClassList("leak-sealed");
                m_Patches[i].RemoveFromClassList("leak-selected");
                m_Patches[i].style.opacity = 1f;

                m_Holes[i].pickingMode = PickingMode.Position;
                m_Patches[i].pickingMode = PickingMode.Position;

                m_Holes[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    TrySeal(holeIndex);
                });

                m_Patches[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    SelectPatch(patchIndex);
                });
            }

            UpdateStatus();
        }

        private static List<int> BuildShuffledSizes(int count)
        {
            var sizes = new List<int>();
            for (var i = 0; i < count; i++)
                sizes.Add(i);

            for (var i = sizes.Count - 1; i > 0; i--)
            {
                var swap = Random.Range(0, i + 1);
                (sizes[i], sizes[swap]) = (sizes[swap], sizes[i]);
            }

            return sizes;
        }

        private static void ApplyDiameter(VisualElement element, float diameter)
        {
            element.style.width = diameter;
            element.style.height = diameter;
        }

        private void SelectPatch(int index)
        {
            if (IsCompleting || index < 0 || index >= m_Patches.Count || m_PatchUsed[index])
                return;

            if (m_SelectedPatch >= 0 && m_SelectedPatch < m_Patches.Count)
                m_Patches[m_SelectedPatch].RemoveFromClassList("leak-selected");

            // Clicking the held patch again puts it back on the rack
            if (m_SelectedPatch == index)
            {
                m_SelectedPatch = -1;
                UpdateStatus();
                return;
            }

            m_SelectedPatch = index;
            m_Patches[index].AddToClassList("leak-selected");
            UpdateStatus();
        }

        private void TrySeal(int holeIndex)
        {
            if (IsCompleting || holeIndex < 0 || holeIndex >= m_Holes.Count || m_HoleSealed[holeIndex])
                return;

            if (m_SelectedPatch < 0)
            {
                if (m_Status != null)
                    m_Status.text = "Grab a patch first!";
                return;
            }

            if (m_PatchSizes[m_SelectedPatch] != m_HoleSizes[holeIndex])
            {
                // Wrong size — the patch pops off and goes back on the rack
                m_Patches[m_SelectedPatch].RemoveFromClassList("leak-selected");
                m_SelectedPatch = -1;

                if (m_Status != null)
                    m_Status.text = "Wrong size, it blew off!";
                return;
            }

            var patch = m_Patches[m_SelectedPatch];
            patch.RemoveFromClassList("leak-selected");
            patch.style.opacity = 0f;
            patch.pickingMode = PickingMode.Ignore;
            m_PatchUsed[m_SelectedPatch] = true;
            m_SelectedPatch = -1;

            var hole = m_Holes[holeIndex];
            hole.AddToClassList("leak-sealed");
            hole.pickingMode = PickingMode.Ignore;
            m_HoleSealed[holeIndex] = true;

            m_SealedCount++;
            UpdateStatus();

            if (m_SealedCount >= m_Holes.Count)
                HandleWinSequence();
        }

        private void UpdateStatus()
        {
            if (m_Status == null)
                return;

            if (m_SelectedPatch >= 0)
                m_Status.text = "Now click the hole it fits";
            else
                m_Status.text = $"Leaks open: {m_Holes.Count - m_SealedCount}";
        }

        private void HandleWinSequence()
        {
            Debug.Log("<color=green>[LeakPatch] HULL SEALED! Delaying close...</color>");

            foreach (var hole in m_Holes)
                hole.pickingMode = PickingMode.Ignore;

            foreach (var patch in m_Patches)
                patch.pickingMode = PickingMode.Ignore;

            if (m_Status != null)
                m_Status.text = "All sealed!";

            ScheduleCompletion();
        }
    }
}