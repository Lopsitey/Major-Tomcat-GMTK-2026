using UnityEngine;

namespace Content.Scripts.Managers.FSM.States
{
    public sealed class ElectricalRoom : StateBase
    {
        public ElectricalRoom(FSM_Manager owner, GameObject[] hazardObjects) : base(owner, hazardObjects) { }

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