using Managers;
using UnityEngine;

namespace Menus
{
    public class SettingsManager : MonoBehaviour
    {
        public void SetMasterVolume(float volume)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }
}