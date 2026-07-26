namespace Content.Scripts.Managers.FSM.States
{
    public sealed class Cockpit : StateBase
    {
        public Cockpit(FSM_Manager owner) : base(owner)
        {
        }

        protected override void UpdateCat()
        {
            // Litter Box task logic is handled via UI interactions
        }
    }
}