using UnityEngine;

namespace Content.Scripts.Managers.FSM.States
{
    public sealed class LivingQuarters : StateBase
    {
        public LivingQuarters(FSM_Manager owner, GameObject[] hazardObjects) : base(owner, hazardObjects) { }
        protected override void UpdateCat()
        {
            //
        }

        public override void EscalateHazard()
        {
            //
        }
    }
}