namespace Content.Scripts.Managers.FSM.Hazards
{
    public sealed class WaterBowl : HazardBase
    {
        // Called when the player solves the mini-game
        public override void ResolveHazard()
        {
            // 1. Tell the global UI or manager that this task is fixed
            // m_GlobalUI.ActiveCleanupTasks--;

            // 2. Hide/Disable this hazard object until next time
            gameObject.SetActive(false);
        }
    }
}