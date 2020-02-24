using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform mainCamera;
    public GameObject debugScreen;
    public bool invertLookX, invertLookY = false;
    private BlockDatabase blockDatabase;
    private WorldGenerator worldGenerator;
    private GameManager gameManager;
    [Space]
    public bool isGrounded;
    public bool isSprinting;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    private const float gravity = -12;
    // -57.46 m/s
    // -70 m/s

    // Diamiter of the player capsule
    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform highlightedBlock;
    public float checkIncrament = 0.25f;
    public float playerReach = 8;

    public byte selectedBlockIndex = 1;

    public Vector3 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public void SpawnPlayer(Vector3 positionToSpawn, BlockDatabase database, WorldGenerator world, GameManager manager)
    {
        blockDatabase = database;
        worldGenerator = world;
        gameManager = manager;
        Position = positionToSpawn;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void GetPlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxisRaw("Mouse X") * gameManager.settings.mouseSensitivity;
        mouseVertical = Input.GetAxisRaw("Mouse Y") * gameManager.settings.mouseSensitivity;

        if (Input.GetKeyDown(KeyCode.LeftShift))
            isSprinting = true;
        if (Input.GetKeyUp(KeyCode.LeftShift))
            isSprinting = false;

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            jumpRequest = true;

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if(scroll != 0)
        {
            if (scroll > 0)
                selectedBlockIndex++;
            else
                selectedBlockIndex--;

            if (selectedBlockIndex > (byte)(blockDatabase.blockDatabase.Count - 1))
                selectedBlockIndex = 1;
            else if (selectedBlockIndex < 1)
                selectedBlockIndex = (byte)(blockDatabase.blockDatabase.Count - 1);
        }

        if (highlightedBlock.gameObject.activeSelf)
        {
            // Destroy Block
            if (Input.GetMouseButtonDown(0))
            {
                worldGenerator.GetChunkFromVector3(highlightedBlock.position).EditVoxel(highlightedBlock.position, 0);
            }
            // Place Block
            if (Input.GetMouseButtonDown(1))
                worldGenerator.GetChunkFromVector3(highlightedBlock.position).EditVoxel(highlightedBlock.position, selectedBlockIndex);
        }
    }

    /// <summary>
    /// Create a loop, like a raycast but custom. Casts forward every <see cref="checkIncrament"/> to see if a block is in that place, if a block is there then place a new block in the direction of the last pos. 
    /// </summary>
    private void PlaceCursorBlock()
    {
        float step = checkIncrament;

        while (step < playerReach)
        {
            Vector3 position = mainCamera.position + (mainCamera.forward * step);

            if(worldGenerator.CheckForVoxel(position))
            {
                highlightedBlock.position = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));

                highlightedBlock.gameObject.SetActive(true);

                return;
            }

            step += checkIncrament;
        }

        highlightedBlock.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        PlaceCursorBlock();

        CalculateVelocity();
        if (jumpRequest)
            Jump();

        transform.Translate(velocity, Space.World);
    }

    private void Update()
    {
        GetPlayerInputs();

        transform.Rotate(Vector3.up * GetMouseHorizontal);
        mainCamera.Rotate(Vector3.right * GetMouseVertical);
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
        if (worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x - playerWidth), (int)(Position.y + downSpeed), (int)(Position.z - playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x + playerWidth), (int)(Position.y + downSpeed), (int)(Position.z - playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x + playerWidth), (int)(Position.y + downSpeed), (int)(Position.z + playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x - playerWidth), (int)(Position.y + downSpeed), (int)(Position.z - playerWidth))))
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
        if (worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x - playerWidth), (int)(Position.y + 2f + upSpeed), (int)(Position.z - playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x + playerWidth), (int)(Position.y + 2f + upSpeed), (int)(Position.z - playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x + playerWidth), (int)(Position.y + 2f + upSpeed), (int)(Position.z + playerWidth))) ||
            worldGenerator.CheckForVoxel(new Vector3Int((int)(Position.x - playerWidth), (int)(Position.y + 2f + upSpeed), (int)(Position.z + playerWidth))))
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
            if (worldGenerator.CheckForVoxel(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)(transform.position.z + playerWidth))) ||
                worldGenerator.CheckForVoxel(new Vector3Int((int)transform.position.x, (int)(transform.position.y + 1f), (int)(transform.position.z + playerWidth))))
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
            if (worldGenerator.CheckForVoxel(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)(transform.position.z - playerWidth))) ||
                worldGenerator.CheckForVoxel(new Vector3Int((int)transform.position.x, (int)(transform.position.y + 1f), (int)(transform.position.z - playerWidth))))
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
            if (worldGenerator.CheckForVoxel(new Vector3Int((int)(transform.position.x - playerWidth), (int)transform.position.y, (int)transform.position.z)) ||
                worldGenerator.CheckForVoxel(new Vector3Int((int)(transform.position.x - playerWidth), (int)(transform.position.y + 1f), (int)transform.position.z)))
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
            if (worldGenerator.CheckForVoxel(new Vector3Int((int)(transform.position.x + playerWidth), (int)transform.position.y, (int)transform.position.z)) ||
                worldGenerator.CheckForVoxel(new Vector3Int((int)(transform.position.x + playerWidth), (int)(transform.position.y + 1f), (int)transform.position.z)))
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
