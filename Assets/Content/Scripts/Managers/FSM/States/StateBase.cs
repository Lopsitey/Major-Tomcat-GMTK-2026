using UnityEngine;

namespace Content.Scripts.Managers.FSM.States
{
    public abstract class StateBase
    {
        // ReSharper disable once InconsistentNaming
        protected readonly FSM_Manager Owner;
        protected readonly GameObject[] m_HazardObjects; // Pre-placed in the room
        public bool IsComplete { get; protected set; }
        public bool IsActive { get; protected set; }
        
        protected int m_CurrentHazardIndex;
        protected StateBase(FSM_Manager owner, GameObject[] hazardObjects)
        {
            Owner = owner;
            m_HazardObjects = hazardObjects;
        }

        /// <summary>
        /// Ensures the state is locked.
        /// </summary>
        public virtual void Enter()
        {
            IsActive = true;
            IsComplete = false;
        }

        /// <summary>
        /// Ensures the state is unlocked.
        /// </summary>
        public virtual void Exit() 
            => IsActive = false;

        protected abstract void UpdateCat();

        public abstract void EscalateHazard();
    }
}