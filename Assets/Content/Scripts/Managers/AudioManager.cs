using UnityEngine;
namespace Managers
{ 
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Volume Settings (0 to 1)")] [SerializeField] [Range(0f, 1f)]
        private float m_MasterVolume = 0.5f; //Controls the loudness of everything

        [Header("Audio Clips")] 
        [SerializeField] private AudioClip m_UIClip;
        [SerializeField] private AudioClip m_catClip;//the sounds itself

        private AudioSource m_SFXSource;//The component that actually plays the sound

        protected override void Awake()
        {
            base.Awake();
            //the component that actually plays the sound
            m_SFXSource = GetComponent<AudioSource>();
        }
/*
        public void Init(CharacterMovement movement)
        {
            //Subscribes to movement events
            movement.OnJump += PlayJump;
            movement.OnAim += PlayChargeDash;
            movement.OnDash += PlayDash;

            if (movement.gameObject.TryGetComponent(out HealthComponent health))
            {
                //Subscribe to health events
                health.OnDamaged += PlayHurt;
                health.OnDeath += PlayDeath;
            }
        }


        private void PlayJump() => PlaySFX(m_JumpClip, 0.5f, 0.4f);

        //??= means a new object will only be created if one doesn't already exist
        private void PlayChargeDash() => m_ChargeSource ??= PlayLoopingSFX(m_ChargeDashClip, 0.6f);

        private void PlayDash()
        {
            //Stops charging once dashing
            m_ChargeSource.Stop();
            Destroy(m_ChargeSource); //gets rid of any ghost objects
            m_ChargeSource = null; //nullified so it can be started again
            PlaySFX(m_DashClip, 0.65f);
        }

        private void PlayHurt(float current, float max, float damage) => PlaySFX(m_HurtClip);
        private void PlayDeath(MonoBehaviour instigator) => PlaySFX(m_DeathClip, 1.2f); //Slightly louder
*/
        //For accessing any clip, anywhere
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (!clip) return;

            //PlayOneShot allows multiple sounds to overlap without cutting each other off
            m_SFXSource.PlayOneShot(clip, m_MasterVolume * volumeMultiplier);
        }

        // Overload the function to accept pitch
        private void PlaySFX(AudioClip clip, float volumeMultiplier, float pitch)
        {
            if (!clip) return;

            //Creates a temporary GameObject for the new audio source
            GameObject audioObj = new GameObject($"SFX_{clip.name}");
            AudioSource source = audioObj.AddComponent<AudioSource>();

            // Copies the mixer groups from the original audio source
            if (m_SFXSource)
                source.outputAudioMixerGroup = m_SFXSource.outputAudioMixerGroup;

            source.clip = clip;
            source.volume = m_MasterVolume * volumeMultiplier;
            source.pitch = pitch;

            //Plays it normally - doesn't need to be one-shot since this is on a separate output
            source.Play();

            //Destroys the object after the clip finishes plus a tiny buffer for loading sound etc 
            Destroy(audioObj, clip.length / Mathf.Abs(pitch) + 0.1f);
        }

        //This returns an audio source so you can have more control over the audio after its played
        private AudioSource PlayLoopingSFX(AudioClip clip, float volumeMultiplier = 1.0f)
        {
            if (!clip) return null;

            GameObject audioObj = new GameObject($"Loop_{clip.name}");
            AudioSource source = audioObj.AddComponent<AudioSource>();

            if (m_SFXSource)
                source.outputAudioMixerGroup = m_SFXSource.outputAudioMixerGroup;

            source.clip = clip;
            source.volume = m_MasterVolume * volumeMultiplier;
            source.loop = true;
            source.spatialBlend = 0f;
            source.Play();

            return source;
        }

        public void SetMasterVolume(float volume)
        {
            //Clamps the volume between 0 and 1 to prevent errors
            m_MasterVolume = Mathf.Clamp01(volume);
        }
    }
}
