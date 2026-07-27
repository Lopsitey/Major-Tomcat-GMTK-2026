namespace Content.Scripts.Managers.FSM.States
{
    public sealed class Cockpit : StateBase
    {
        public Cockpit(FSM_Manager owner) : base(owner)
        {
        }

        protected override void UpdateCat()
        {
            // Nose Cone Twist (and other cockpit tasks) are handled via UI interactions
        }
    }
}