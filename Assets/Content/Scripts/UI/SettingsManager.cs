#region

using Managers;
using UnityEngine;

#endregion

namespace Content.Scripts.UI
{
    public sealed class SettingsManager : MonoBehaviour
    {
        public void SetMasterVolume(float volume)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }
}