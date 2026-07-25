using UnityEngine;

namespace Content.Scripts.Managers.FSM.States
{
    public sealed class Cockpit : StateBase
    {
        public Cockpit(FSM_Manager owner, GameObject[] hazardObjects) : base(owner, hazardObjects) { }

        protected override void UpdateCat()
        {
            //Logic
        }

        public override void EscalateHazard()
        {
            //
        }
    }
}