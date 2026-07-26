#region

using UnityEngine;

#endregion

namespace Content.Scripts.Managers
{
    public class CameraController : Singleton<CameraController>
    {
        [Header("Room Anchors")]
        [Tooltip("Assign 4 empty GameObjects placed in the centre of the rooms.")]
        [SerializeField]
        private Transform[] m_RoomAnchors;

        private int m_CurrentRoomIndex = 0;

        private void Start()
            => UpdateCameraPosition();

        public void MoveUp()
        {
            m_CurrentRoomIndex--;
            if (m_CurrentRoomIndex < 0) m_CurrentRoomIndex = m_RoomAnchors.Length - 1; // Wrap around
            UpdateCameraPosition();
        }

        public void MoveDown()
        {
            m_CurrentRoomIndex++;
            if (m_CurrentRoomIndex >= m_RoomAnchors.Length) m_CurrentRoomIndex = 0; // Wrap around
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (m_RoomAnchors == null || m_RoomAnchors.Length == 0) return;

            //Snap to new room
            Vector3 newPos = m_RoomAnchors[m_CurrentRoomIndex].position;

            //Keeps the z so there's no funny business
            newPos.z = transform.position.z;

            transform.position = newPos;
        }
    }
}