namespace Content.Scripts.Managers.FSM.States
{
    //sealed means it cannot be inherited - no more children    public sealed class IdleState : StateBase
    public sealed class IdleState : StateBase
    {
        //: base(params) calls the parent constructor which means Owner = owner; doesn't need to be repeated in every child class
        //Owner = owner was needed because when objects are created with new, if they are passed parameters they need to use a constructor to access them
        public IdleState() : base(null)
        {
        }

        public override void Enter()
        {
        }

        protected override void UpdateCat()
        {
            //TODO: Idle animation (frame by frame?)
        }

        public override void EscalateHazard()
        {
            //Cannot escalate from idle state, so do nothing
        }
    }
}