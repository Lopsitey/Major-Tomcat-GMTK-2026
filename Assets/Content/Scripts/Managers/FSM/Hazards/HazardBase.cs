using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Managers.FSM.Hazards
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class HazardBase : MonoBehaviour
    {
        // Called when the player solves the mini-game
        public abstract void ResolveHazard();
    }
}