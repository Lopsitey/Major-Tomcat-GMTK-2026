#region

using UnityEngine;

#endregion

namespace Content.Scripts.Managers.FSM.States
{
    public abstract class StateBase
    {
        // ReSharper disable once InconsistentNaming
        protected readonly FSM_Manager Owner;

        public bool IsComplete { get; protected set; }
        public bool IsActive { get; protected set; }

        protected int m_CurrentHazardIndex = -1;
        protected int m_CurrentMaintenanceIndex = -1;

        protected StateBase(FSM_Manager owner)
        {
            Owner = owner;
        }

        /// <summary>
        ///     Ensures the state is locked.
        /// </summary>
        public virtual void Enter()
        {
            IsActive = true;
            IsComplete = false;

            // Deactivate ALL previous tasks and hazards first
            if (Owner.MaintenanceObjects != null)
                foreach (var task in Owner.MaintenanceObjects)
                    if (task && task.activeInHierarchy)
                        task.SetActive(false);

            if (Owner.HazardObjects != null)
                foreach (var hazard in Owner.HazardObjects)
                    if (hazard && hazard.activeInHierarchy)
                        hazard.SetActive(false);

            // Roll random maintenance task to display
            if (Owner.MaintenanceObjects != null && Owner.MaintenanceObjects.Length > 0)
            {
                m_CurrentMaintenanceIndex = Random.Range(0, Owner.MaintenanceObjects.Length);
                GameObject maintenanceTask = Owner.MaintenanceObjects[m_CurrentMaintenanceIndex];

                if (maintenanceTask)
                {
                    maintenanceTask.SetActive(true); //Now visible and clickable
                    Debug.Log($"Maintenance task activated: {maintenanceTask.name}");
                }
            }

            Debug.Log($"Cat is tampering with task {m_CurrentMaintenanceIndex}.");
        }

        /// <summary>
        ///     For when the maintenance timer runs out and the player has not completed the task in time. This will escalate to a
        ///     hazard.
        /// </summary>
        public virtual void EscalateHazard()
        {
            // Disable the current maintenance task
            if (Owner.MaintenanceObjects != null && Owner.MaintenanceObjects.Length > m_CurrentMaintenanceIndex)
            {
                GameObject maintenanceTask = Owner.MaintenanceObjects[m_CurrentMaintenanceIndex];
                if (maintenanceTask)
                {
                    maintenanceTask.SetActive(false);
                    Debug.Log($"Maintenance task deactivated: {maintenanceTask.name}");
                }
            }

            // Roll random hazard to spawn
            if (Owner.HazardObjects != null && Owner.HazardObjects.Length > 0)
            {
                m_CurrentHazardIndex = Random.Range(0, Owner.HazardObjects.Length);
                GameObject selectedHazard = Owner.HazardObjects[m_CurrentHazardIndex];

                if (selectedHazard)
                {
                    // Enables the hazard and UI
                    selectedHazard.SetActive(true);
                    Debug.Log($"Hazard activated: {selectedHazard.name}");
                }
            }
        }

        /// <summary>
        ///     Ensures the state is unlocked.
        /// </summary>
        public void Exit() => IsActive = false;

        /// <summary>
        ///     Called each frame to check if the current maintenance task is complete.
        ///     Tasks complete themselves by calling CompleteTask() which deactivates their GameObject.
        /// </summary>
        public virtual void CheckTaskCompletion()
        {
            if (Owner.MaintenanceObjects != null && Owner.MaintenanceObjects.Length > m_CurrentMaintenanceIndex)
            {
                var maintenanceTask = Owner.MaintenanceObjects[m_CurrentMaintenanceIndex];
                if (maintenanceTask && !maintenanceTask.activeInHierarchy) IsComplete = true;
            }
        }

        protected abstract void UpdateCat();
    }
}