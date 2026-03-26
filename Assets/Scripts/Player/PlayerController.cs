using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float rotationSpeed = 540f;

    [SerializeField] private float jumpHeight = 6f;
    [SerializeField] private float gravityMultiplier = 1.8f;
    [SerializeField] private float fallGravityMultiplier = 3f;
    [SerializeField] private float minFallMultiplier = 1f;
    [SerializeField] private float fallGravityRampSpeed = 8f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float apexHangThreshold = 2f;
    [SerializeField] private float apexGravityScale = 0.5f;
    [SerializeField] private float terminalFallSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferDuration = 0.2f;

    private CharacterController _characterController;
    private InputSystem_Actions _inputActions;

    private Vector2 _rawMoveInput;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;

    private bool _sprintHeld;
    private bool _jumpBuffered;
    private bool _isJumping;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private MovingPlatform _currentPlatform;

    private float EffectiveGravity => Physics.gravity.y * gravityMultiplier;
    private float TargetSpeed => _sprintHeld ? sprintSpeed : walkSpeed;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputActions = new InputSystem_Actions();
        _inputActions.Player.SetCallbacks(this);
    }

    private void OnEnable() => _inputActions.Player.Enable();

    private void OnDisable() => _inputActions.Player.Disable();

    private void OnDestroy() => _inputActions.Dispose();

    private void Update()
    {
        UpdateGroundState();
        UpdatePlatformTracking();
        TickJumpBuffer();
        ProcessJump();
        ProcessHorizontalMovement();
        ApplyGravity();

        Vector3 platformCarry = _currentPlatform != null ? _currentPlatform.DeltaPosition : Vector3.zero;
        _characterController.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime + platformCarry);
    }

    private void UpdateGroundState()
    {
        if (_characterController.isGrounded)
        {
            _coyoteTimeCounter = coyoteTime;
            _isJumping = false;
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void UpdatePlatformTracking()
    {
        if (!_characterController.isGrounded)
        {
            _currentPlatform = null;
            return;
        }

        float rayLength = _characterController.height / 2f + _characterController.skinWidth + 0.1f;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength))
        {
            _currentPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
            hit.collider.GetComponentInParent<MushroomSpringboard>()?.TryBounce(this);
        }
        else
        {
            _currentPlatform = null;
        }
    }

    private void TickJumpBuffer()
    {
        if (_jumpBuffered)
        {
            _jumpBufferCounter = jumpBufferDuration;
            _jumpBuffered = false;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void ProcessJump()
    {
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * EffectiveGravity);
            _coyoteTimeCounter = 0f;
            _jumpBufferCounter = 0f;
            _isJumping = true;
        }
    }

    private void ProcessHorizontalMovement()
    {
        Vector3 desiredDirection = GetMoveDirection();
        Vector3 targetVelocity = desiredDirection * TargetSpeed;
        float blendRate = desiredDirection.sqrMagnitude > 0f ? acceleration : deceleration;

        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, blendRate * Time.deltaTime);

        if (Mathf.Abs(_horizontalVelocity.x) > 0.01f)
        {
            float targetYaw = _horizontalVelocity.x > 0f ? 90f : -90f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetMoveDirection()
    {
        if (Mathf.Abs(_rawMoveInput.x) < 0.01f)
            return Vector3.zero;

        return new Vector3(_rawMoveInput.x, 0f, 0f).normalized;
    }

    private void ApplyGravity()
    {
        float multiplier;

        if (_isJumping && Mathf.Abs(_verticalVelocity) <= apexHangThreshold)
        {
            float t = Mathf.SmoothStep(0f, 1f, 1f - (Mathf.Abs(_verticalVelocity) / apexHangThreshold));
            multiplier = Mathf.Lerp(1f, apexGravityScale, t);
        }
        else if (_verticalVelocity < 0f)
        {
            float fallProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, fallGravityRampSpeed, -_verticalVelocity));
            multiplier = Mathf.Lerp(minFallMultiplier, fallGravityMultiplier, fallProgress);
        }
        else
        {
            multiplier = 1f;
        }

        _verticalVelocity += EffectiveGravity * multiplier * Time.deltaTime;
        _verticalVelocity = Mathf.Max(_verticalVelocity, -terminalFallSpeed);
    }

    public float VerticalVelocity => _verticalVelocity;

    public void ApplyVerticalBoost(float launchSpeed)
    {
        _verticalVelocity = launchSpeed;
        _isJumping = true;
        _coyoteTimeCounter = 0f;
        _jumpBufferCounter = 0f;
    }


    public void OnMove(InputAction.CallbackContext context) =>
        _rawMoveInput = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            _jumpBuffered = true;

        if (context.canceled && _isJumping && _verticalVelocity > 0f)
            _verticalVelocity *= jumpCutMultiplier;
    }

    public void OnSprint(InputAction.CallbackContext context) =>
        _sprintHeld = context.ReadValueAsButton();

    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
}