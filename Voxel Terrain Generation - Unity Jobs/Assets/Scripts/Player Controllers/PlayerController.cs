using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform mainCamera;
    [SerializeField] private bool invertLookX, invertLookY = false;
    [SerializeField] private Vector2 mouseSensitivity = Vector2.one;
    private WorldData worldData;
    [Space]
    public bool isGrounded;
    public bool isSprinting;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    private const float gravity = -9.8f;

    // Diamiter of the player capsule
    public float playerWidth = 0.15f;
    public float boundsTolerance = 0.1f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Vector3 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public void SpawnPlayer(Vector3 positionToSpawn, WorldData _WorldData)
    {
        worldData = _WorldData;
        Position = positionToSpawn;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void GetPlayerInputs()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X") * mouseSensitivity.x;
        mouseVertical = Input.GetAxis("Mouse Y") * mouseSensitivity.y;

        if (Input.GetKeyDown(KeyCode.LeftShift))
            isSprinting = true;
        if (Input.GetKeyUp(KeyCode.LeftShift))
            isSprinting = false;

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            jumpRequest = true;

    }

    private void FixedUpdate()
    {
        CalculateVelocity();
        if (jumpRequest)
            Jump();

        transform.Rotate(Vector3.up * GetMouseHorizontal);
        mainCamera.Rotate(Vector3.right * GetMouseVertical);
        transform.Translate(velocity, Space.World);
    }

    private void Update()
    {
        GetPlayerInputs();
    }


    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity()
    {
        // Affect vertical momentum with gravity.
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        // If we're sprinting, use the sprint multiplier.
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

        // Apply vertical momentum (falling/jumping).
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && CheckFront) || (velocity.z < 0 && CheckBack))
            velocity.z = 0;
        if ((velocity.x > 0 && CheckRight) || (velocity.x < 0 && CheckLeft))
            velocity.x = 0;

        if (velocity.y < 0)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = CheckUpSpeed(velocity.y);
    }

    /// <summary>
    /// Calculates if the player is currently falling at (<paramref name="downSpeed"/>) or standing on a block that is marked solid.
    /// </summary>
    /// <param name="downSpeed"></param>
    /// <returns></returns>
    private float CheckDownSpeed(float downSpeed)
    {
        if (worldData.CheckForVoxelInSpace(new Vector3(Position.x - playerWidth, Position.y + downSpeed, Position.z - playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x + playerWidth, Position.y + downSpeed, Position.z - playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x + playerWidth, Position.y + downSpeed, Position.z + playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x - playerWidth, Position.y + downSpeed, Position.z - playerWidth)))
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    /// <summary>
    /// Calculates if the player is currently jumping up at (<paramref name="upSpeed"/>).
    /// </summary>
    /// <param name="upSpeed"></param>
    /// <returns></returns>
    private float CheckUpSpeed(float upSpeed)
    {
        if (worldData.CheckForVoxelInSpace(new Vector3(Position.x - playerWidth, Position.y + 2f + upSpeed, Position.z - playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x + playerWidth, Position.y + 2f + upSpeed, Position.z - playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x + playerWidth, Position.y + 2f + upSpeed, Position.z + playerWidth)) ||
            worldData.CheckForVoxelInSpace(new Vector3(Position.x - playerWidth, Position.y + 2f + upSpeed, Position.z + playerWidth)))
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    /// <summary>
    /// Is the player walking forward into a block, or a block is colliding with wither their head or feet in a 2 voxel block height
    /// </summary>
    public bool CheckFront
    {
        get
        {
            if (worldData.CheckForVoxelInSpace(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                worldData.CheckForVoxelInSpace(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
                return true;
            else
                return false;
        }
    }
    /// <summary>
    /// Is the player walking back into a block, or a block is colliding with wither their head or feet in a 2 voxel block height
    /// </summary>
    public bool CheckBack
    {
        get
        {
            if (worldData.CheckForVoxelInSpace(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) || 
                worldData.CheckForVoxelInSpace(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
                return true;
            else
                return false;
        }
    }

    /// <summary>
    /// Is the player walking left into a block, or a block is colliding with wither their head or feet in a 2 voxel block height
    /// </summary>
    public bool CheckLeft
    {
        get
        {
            if (worldData.CheckForVoxelInSpace(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                worldData.CheckForVoxelInSpace(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            else
                return false;
        }
    }

    /// <summary>
    /// Is the player walking right into a block, or a block is colliding with wither their head or feet in a 2 voxel block height
    /// </summary>
    public bool CheckRight
    {
        get
        {
            if (worldData.CheckForVoxelInSpace(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                worldData.CheckForVoxelInSpace(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            else
                return false;
        }
    }

    private float GetMouseVertical
    {
        get{ return (invertLookY) ?  Mathf.Clamp(mouseVertical, -80, 90) : Mathf.Clamp(-mouseVertical, -80, 90); }
    }

    private float GetMouseHorizontal
    {
        get { return (invertLookX) ? -mouseHorizontal : mouseHorizontal; }
    }
}
