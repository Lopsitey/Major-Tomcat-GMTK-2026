using UnityEngine;
using System.Collections;

public class catWalk : MonoBehaviour
{
    [Header("Wander Area")]
    [Tooltip("The centre of the area the cat can wander in.")]
    [SerializeField]
    private Transform m_WanderCentre;

    [SerializeField]
    private Vector2 m_WanderArea = new Vector2(5f, 3f);

    [Header("Movement")]
    [SerializeField]
    private float m_MoveSpeed = 1f;

    [SerializeField]
    private float m_MinWalkDistance = 1f;

    [SerializeField]
    private float m_MaxWalkDistance = 3f;

    [Header("Waiting")]
    [SerializeField]
    private float m_MinWaitTime = 1f;

    [SerializeField]
    private float m_MaxWaitTime = 4f;

    [Header("Movement Animation")]
    [SerializeField]
    private float m_TiltAmount = 8f;

    [SerializeField]
    private float m_ShuffleAmount = 0.05f;

    [SerializeField]
    private float m_ShuffleSpeed = 10f;

    private Vector3 m_StartingPosition;
    private Quaternion m_StartingRotation;

    private void Start()
    {
        m_StartingPosition = transform.position;
        m_StartingRotation = transform.rotation;

        StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Wait before choosing a new destination.
            float waitTime = Random.Range(
                m_MinWaitTime,
                m_MaxWaitTime
            );

            yield return new WaitForSeconds(waitTime);

            // Choose a random destination.
            Vector3 targetPosition = GetRandomDestination();

            // Walk towards the destination.
            yield return StartCoroutine(
                WalkToPosition(targetPosition)
            );
        }
    }

    private IEnumerator WalkToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        // Determine which direction the cat is moving.
        float direction = Mathf.Sign(
            targetPosition.x - transform.position.x
        );

        // If the target is directly above/below, default to right.
        if (Mathf.Approximately(direction, 0f))
        {
            direction = 1f;
        }

        // Flip the cat depending on walking direction.
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        float distance = Vector3.Distance(
            startPosition,
            targetPosition
        );

        float travelTime = distance / m_MoveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / travelTime;

            // Move the cat towards the destination.
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                progress
            );

            // Create a little bobbing/shuffling motion.
            float shuffle = Mathf.Sin(
                elapsedTime * m_ShuffleSpeed
            ) * m_ShuffleAmount;

            Vector3 position = transform.position;

            // Apply vertical shuffle.
            position.y += shuffle;

            transform.position = position;

            // Tilt the cat as it walks.
            float tilt = Mathf.Sin(
                elapsedTime * m_ShuffleSpeed
            ) * m_TiltAmount;

            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                tilt
            );

            yield return null;
        }

        // Make sure the cat finishes exactly at the target.
        transform.position = targetPosition;

        // Return to normal rotation.
        transform.rotation = m_StartingRotation;
    }

    private Vector3 GetRandomDestination()
    {
        Vector3 centre;

        if (m_WanderCentre != null)
        {
            centre = m_WanderCentre.position;
        }
        else
        {
            centre = m_StartingPosition;
        }

        float x = Random.Range(
            -m_WanderArea.x / 2f,
            m_WanderArea.x / 2f
        );

        float y = Random.Range(
            -m_WanderArea.y / 2f,
            m_WanderArea.y / 2f
        );

        return centre + new Vector3(x, y, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (m_WanderCentre == null)
            return;

        Gizmos.DrawWireCube(
            m_WanderCentre.position,
            new Vector3(
                m_WanderArea.x,
                m_WanderArea.y,
                0f
            )
        );
    }
}

