using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public PlayerData data;
    [SerializeField] public Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private LayerMask groundLayer;

    

    #region Variables
    public Rigidbody2D rb;

    public Vector2 moveInput;
    public bool isFacingRight;

    [Space]
    [Header("Collision")]
    public bool isGrounded;
    public bool isOnWall;

    [Space]
    [Header("Wall Mechanics")]
    public bool isWallSliding;
    public bool isWallJumping;

    [Space]
    [Header("Power Ups")]
    public bool isMoveSpeed;
    private bool moveSpeedInit;
    public bool isJumpBoost;
    public bool canDoubleJump;
    public bool doubleJumpPressed;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Start is called before the first frame update
    void Start()
    {
        isFacingRight = true;
    }

    // Update is called once per frame
    void Update()
    {

        #region INPUT HANDLERS
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        #endregion


        #region JUMP 
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, data.jumpPower);
        }
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * data.jumpCutPower);
        }
        #endregion



        // Checks collision detected by collision checkers
        CollisionCheck();
        
        // Check if wall sliding
        WallSlide();
        // Check if can wall jump
        WallJump();


        if (!isWallJumping)
        {
            // Checks if sprite needs to be flipped depending on direction faced
            Flip();
        }


        // Check powerups

        UpdatePowerUps();
        
    }


    private void FixedUpdate()
    {
        if (!isWallJumping) // move player horizontally if not wall jumping
        {
            // move player horizontally
            rb.velocity = new Vector2(moveInput.x * data.speed, rb.velocity.y);
        }
   
    }



    #region WALL MECHANICS
    private void WallSlide()
    {
        if (isOnWall && !isGrounded && moveInput.x != 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -data.wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            data.wallJumpingDirection = -transform.localScale.x;
            data.wallJumpingCounter = data.wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            data.wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && data.wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(data.wallJumpingPower.x * data.wallJumpingDirection, data.wallJumpingPower.y);
            data.wallJumpingCounter = 0f;

            if (transform.localScale.x != data.wallJumpingDirection)
            {
                PerformFlip();
            }

            Invoke(nameof(StopWallJumping), data.wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }
    #endregion


    #region COLLISION CHECK

    private void CollisionCheck()
    {
        GroundCollisionCheck();
        WallCollisionCheck();
    }

    private void GroundCollisionCheck()
    {
        //Ground Check
        if (Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer)) //checks if set box overlaps with ground
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void WallCollisionCheck()
    {
        if (Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer))
        {
            isOnWall = true;
        }
        else
        {
            isOnWall = false;
        }
    }

    #endregion


    #region GENERAL METHODS
    private void Flip()
    {
        if (isFacingRight && moveInput.x < 0f || !isFacingRight && moveInput.x > 0f)
        {
            PerformFlip();
        }
    }

    private void PerformFlip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    #endregion


    #region POWER UPS
    private void UpdatePowerUps()
    {
        #region MOVESPEED
        if (isMoveSpeed)
        {
            // Initial move speed increase
            if (!moveSpeedInit) 
            {
                data.speed = data.moveSpeedIncrease;
                moveSpeedInit = true;
            }
            
            data.moveSpeedTimer += Time.deltaTime;
            
            data.speed -= 0.002f; // decrement move speed over time
            if (data.moveSpeedTimer >= data.moveSpeedTimerCap)
            {
                data.speed = data.defaultMoveSpeed;
                data.moveSpeedTimer = 0f;
                moveSpeedInit = false;
                isMoveSpeed = false;
            }
        }
        #endregion

        #region DOUBLE JUMP
        if (canDoubleJump)
        {
            if (Input.GetButtonDown("Jump"))
            {
                rb.velocity = new Vector2(rb.velocity.x, data.jumpPower);
                if (isGrounded)
                    doubleJumpPressed = false;
                else if (!isGrounded && !isWallJumping)
                    doubleJumpPressed = true;
            } 

            // cant DJ anymore when picking a DJ powerup while midair AND landing on ground not using the DJ
            if (!doubleJumpPressed && isGrounded)
            {
                canDoubleJump = false;
            }
            // cant DJ anymore when picking a DJ powerup while midair AND wallsliding not using the DJ
            if (!doubleJumpPressed && isWallSliding)
            {
                canDoubleJump = false;
            }
        }
        if (doubleJumpPressed && !isGrounded)
        {
            canDoubleJump = false;
            if (isGrounded)
            {
                doubleJumpPressed = false;
            }
        }
        if (isGrounded && !canDoubleJump)
        {
            doubleJumpPressed = false;
        }
        #endregion

        #region JUMP BOOST
        if (isJumpBoost)
        {
            data.jumpPower = data.jumpBoostIncrease;
            data.jumpBoostTimer += Time.deltaTime;
            if (data.jumpBoostTimer >= data.jumpBoostTimerCap)
            {
                data.jumpPower = data.defaultJumpPower;
                data.jumpBoostTimer = 0f;
                isJumpBoost = false;
            }
        }
        #endregion
    }
    #endregion


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        Gizmos.color = Color.blue;
        /*Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);*/
    }
    #endregion
}