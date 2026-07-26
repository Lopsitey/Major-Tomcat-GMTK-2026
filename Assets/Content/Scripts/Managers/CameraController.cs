#region

using System.Collections;
using UnityEngine;

#endregion

namespace Content.Scripts.Managers
{
    public class CameraController : Singleton<CameraController>
    {
        [Header("Room Anchors")]
        [Tooltip("Assign room anchors in order from top to bottom.")]
        [SerializeField]
        private Transform[] m_RoomAnchors;

        [Header("Camera Movement")]
        [SerializeField]
        private float m_CameraSpeed = 5f;

        private int m_CurrentRoomIndex = 0;
        private bool m_IsTransitioning = false;

        private void Start()
            => UpdateCameraPosition();

        public void MoveUp()
        {
            if (m_IsTransitioning)
                return;

            if (m_CurrentRoomIndex > 0)
            {
                m_CurrentRoomIndex--;
                UpdateCameraPosition();
            }
        }

        public void MoveDown()
        {
            if (m_IsTransitioning)
                return;

            if (m_CurrentRoomIndex < m_RoomAnchors.Length - 1)
            {
                m_CurrentRoomIndex++;
                UpdateCameraPosition();
            }
        }

        private void UpdateCameraPosition()
        {
            if (m_RoomAnchors == null || m_RoomAnchors.Length == 0)
                return;

            Vector3 newPos = m_RoomAnchors[m_CurrentRoomIndex].position;

            // Keep the camera's current Z position.
            newPos.z = transform.position.z;

            StartCoroutine(LerpCameraPosition(newPos));
        }

        private IEnumerator LerpCameraPosition(Vector3 targetPosition)
        {
            m_IsTransitioning = true;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    m_CameraSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Make sure we end exactly at the target.
            transform.position = targetPosition;

            m_IsTransitioning = false;
        }
    }
}