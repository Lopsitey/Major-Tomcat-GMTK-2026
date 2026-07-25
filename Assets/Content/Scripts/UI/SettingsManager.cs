using Managers;
using UnityEngine;

namespace Content.Scripts.UI
{
    public class SettingsManager : MonoBehaviour
    {
        public void SetMasterVolume(float volume)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }
}