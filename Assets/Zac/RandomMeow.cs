using System.Collections;
using UnityEngine;

public class RandomMeow : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource m_AudioSource;

    [SerializeField]
    private AudioClip[] m_CatSounds;

    [Header("Random Interval")]
    [SerializeField]
    private float m_MinInterval = 5f;

    [SerializeField]
    private float m_MaxInterval = 20f;

    private void Start()
    {
        StartCoroutine(PlayRandomCatSounds());
    }

    private IEnumerator PlayRandomCatSounds()
    {
        while (true)
        {
            // Wait for a random amount of time.
            float randomWaitTime = Random.Range(
                m_MinInterval,
                m_MaxInterval
            );

            yield return new WaitForSeconds(randomWaitTime);

            // Pick a random cat sound.
            AudioClip randomSound = m_CatSounds[
                Random.Range(0, m_CatSounds.Length)
            ];

            // Play the sound.
            m_AudioSource.PlayOneShot(randomSound);
        }
    }
}

