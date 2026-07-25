using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Rooms")]
    [Tooltip("Drag your room camera points here from bottom to top.")]
    [SerializeField] private Transform[] roomPositions;

    [Header("Camera Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input")]
    [SerializeField] private KeyCode moveUpKey = KeyCode.W;
    [SerializeField] private KeyCode moveDownKey = KeyCode.S;

    private int currentRoom = 0;
    private bool isMoving = false;

    private void Start()
    {
        if (roomPositions == null || roomPositions.Length == 0)
        {
            Debug.LogWarning("No room positions have been assigned.");
            return;
        }

        // Start at the first room
        transform.position = roomPositions[currentRoom].position;
    }

    private void Update()
    {
        HandleInput();
        MoveCamera();
    }

    private void HandleInput()
    {
        // Move up one room
        if (Input.GetKeyDown(moveUpKey))
        {
            MoveUp();
        }

        // Move down one room
        if (Input.GetKeyDown(moveDownKey))
        {
            MoveDown();
        }
    }

    public void MoveUp()
    {
        if (isMoving)
            return;

        if (currentRoom < roomPositions.Length - 1)
        {
            currentRoom++;
            isMoving = true;
        }
    }

    public void MoveDown()
    {
        if (isMoving)
            return;

        if (currentRoom > 0)
        {
            currentRoom--;
            isMoving = true;
        }
    }

    private void MoveCamera()
    {
        if (roomPositions == null || roomPositions.Length == 0)
            return;

        Vector3 targetPosition = roomPositions[currentRoom].position;

        // Smoothly move towards the room
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // Check if camera has arrived
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}
