using UnityEngine;

namespace Content.Scripts.Managers.FSM.States
{
    public sealed class EngineRoom : StateBase
    {
        public EngineRoom(FSM_Manager owner, GameObject[] hazardObjects) : base(owner, hazardObjects) { }

        public override void Enter()
        {
            base.Enter();
            
            m_CurrentHazardIndex = Random.Range(0, 2); 
            
            //Update the UI here
            Debug.Log($"Cat has started maintenance task {m_CurrentHazardIndex} in Engine Room!");
            // Randomly select which hazard *will* trigger if the player fails
            if (m_HazardObjects != null && m_HazardObjects.Length > 0)
                m_CurrentHazardIndex = Random.Range(0, m_HazardObjects.Length);
            
        }

        protected override void UpdateCat()
        {
            //TODO: Cat movement update animation.
        }

        public override void EscalateHazard()
        {
            // Instead of Instantiate, we simply enable the pre-existing hazard object!
            if (m_HazardObjects != null && m_HazardObjects.Length > m_CurrentHazardIndex)
            {
                GameObject selectedHazard = m_HazardObjects[m_CurrentHazardIndex];
                
                if (selectedHazard)
                {
                    selectedHazard.SetActive(true); // Turns on the MonoBehaviour, UIDocument, and UI visuals!
                    Debug.Log($"Hazard activated: {selectedHazard.name}");
                }
            }
        }
    }
}