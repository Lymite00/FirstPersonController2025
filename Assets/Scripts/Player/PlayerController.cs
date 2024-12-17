using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Action OnSprint;
    public Action OnIdle;
    public Action OnAir;
    public Action OnWalking;
    public Action OnJump;
    public Action OnCrouch;
    
    [Header("Movement")]
    [SerializeField] private float _moveSpeed;

    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _sprintSpeed;
    
    [SerializeField] private float _groundDrag;

    [Header("Air")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;

    [Header("Crouching")] 
    [SerializeField] private float _crouchSpeed;
    [SerializeField] private float _crouchYScale;
    [SerializeField] private float _startYScale;
    [SerializeField] private bool _isCrouching;

    [Header("Slope Handling")] 
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    
    [Header("Components")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Rigidbody rb;
    
    private float _horizontalInput;
    private float _verticalInput;
    
    private bool _grounded;
    private bool _readyToJump;
    private bool _exitingSlope;

    private Vector3 _moveDirection;
    
    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air,
        idle
    }
    
    #region States
    private void SetCrouching()
    {
        state = MovementState.crouching;
        _moveSpeed = _crouchSpeed;
    }
    private void SetSprinting()
    {
        state = MovementState.sprinting;
        _moveSpeed = _sprintSpeed;
    }
    private void SetWalking()
    {
        state = MovementState.walking;
        _moveSpeed = _walkSpeed;
    }
    private void SetAir()
    {
        state = MovementState.air;
    }
    private void SetIdle()
    {
        state = MovementState.idle;
    }
    private void StateHandler()
    {
        if (_grounded && Input.GetKey(_sprintKey))
        {
            OnSprint?.Invoke();
        }
        else if (_grounded && !_isCrouching)
        {
            OnWalking?.Invoke();
        }
        else if (!_grounded && !_isCrouching)
        {
            OnAir?.Invoke();
        }
        else if (_grounded && _isCrouching)
        {
            OnCrouch?.Invoke();
        }
    }
    #endregion
    #region Unity Functions
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        SetPlayerSettings();
    }
    private void OnEnable()
    {
        OnJump += Jump;
        OnCrouch += SetCrouching;
        OnSprint += SetSprinting;
        OnWalking += SetWalking;
        OnAir += SetAir;
        OnIdle += SetIdle;
    }
    private void OnDisable()
    {
        OnJump -= Jump;
        OnCrouch -= SetCrouching;
        OnSprint -= SetSprinting;
        OnWalking -= SetWalking;
        OnAir -= SetAir;
        OnIdle -= SetIdle;
    }
    private void Update()
    {
        _grounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.3f, whatIsGround);
        
        if (_grounded)
            rb.linearDamping = _groundDrag;
        else
            rb.linearDamping = 0;
        
        MyInput();
        SpeedControl();
        StateHandler();
        //Debug.Log(state);
    } 
    private void FixedUpdate()
    {
        MovePlayer();
    }
    #endregion
    #region CustomFunctions
    private void SetPlayerSettings()
    {
        rb.freezeRotation = true;
        _readyToJump = true;
        _startYScale = transform.localPosition.y-.45f;
    }
    private void MyInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(_jumpKey) && _readyToJump && _grounded)
        {
            _readyToJump = false;

            OnJump?.Invoke();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f,ForceMode.Impulse);
            _isCrouching = true;
        }

        if (Input.GetKeyUp(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
            rb.AddForce(Vector3.up * 5f,ForceMode.Impulse);
            _isCrouching = false;
        }
    }
    private void MovePlayer()
    {
        _moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

        if(_grounded)
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        else if(!_grounded)
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * airMultiplier, ForceMode.Force);
        
        if (OnSlope() && !_exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * _moveSpeed * 20f, ForceMode.Force);
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 50f, ForceMode.Force);
            }
        }

        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_exitingSlope)
        {
            if (rb.linearVelocity.magnitude> _moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * _moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if(flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            } 
        }
    }

    private void Jump()
    {
        _exitingSlope = true;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        _readyToJump = true;
        _exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position,Vector3.down,out _slopeHit,_playerHeight*.5f+.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
    }
    #endregion
}
