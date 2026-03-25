using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float fixedPitch = 8f;

    [SerializeField] private float horizontalSmoothTime = 0.12f;
    [SerializeField] private float verticalRiseSmoothTime = 0.25f;
    [SerializeField] private float verticalFallSmoothTime = 0.08f;

    [SerializeField] private float lookAheadDistance = 2.5f;
    [SerializeField] private float lookAheadSmoothTime = 0.4f;

    [SerializeField] private float verticalDeadzone = 0.8f;

    [SerializeField] private float baseFov = 62f;
    [SerializeField] private float sprintFovBonus = 8f;
    [SerializeField] private float fallFovBonus = 5f;
    [SerializeField] private float fallFovMaxSpeed = 15f;
    [SerializeField] private float fovSmoothTime = 0.25f;
    [SerializeField] private float fovRampStartSpeed = 6f;
    [SerializeField] private float fovRampEndSpeed = 9f;

    [SerializeField] private float maxShakeAngle = 3f;
    [SerializeField] private float traumaDecayRate = 1.5f;

    [SerializeField] private bool useLevelBounds;
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -10f);
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 20f);

    [SerializeField] private bool useCameraHeightProgression = true;
    [SerializeField] private float levelStartX = -50f;
    [SerializeField] private float levelEndX = 50f;
    [SerializeField] private float progressionStartYDelta = -1f;
    [SerializeField] private float progressionEndYDelta = 2f;

    private Camera _camera;

    private float _xVelocity;
    private float _yVelocity;
    private float _fovVelocity;
    private float _lookAheadX;
    private float _lookAheadVelocity;
    private float _anchoredY;
    private float _trauma;
    private float _currentHorizontalSpeed;
    private float _currentVerticalSpeed;

    private Vector3 _previousTargetPosition;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        if (followTarget != null)
            SnapToTarget();

        if (_camera != null)
            _camera.fieldOfView = baseFov;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        _currentHorizontalSpeed = (followTarget.position.x - _previousTargetPosition.x) / Time.deltaTime;
        _currentVerticalSpeed = (followTarget.position.y - _previousTargetPosition.y) / Time.deltaTime;

        UpdateLookAhead();
        UpdateVerticalAnchor();
        UpdatePosition();
        UpdateFov();
        ApplyScreenShake();

        _previousTargetPosition = followTarget.position;
    }

    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    public void SnapToTarget()
    {
        if (followTarget == null) return;

        float x = followTarget.position.x + cameraOffset.x;
        float y = followTarget.position.y + DynamicYOffset;
        float z = followTarget.position.z + cameraOffset.z;

        transform.position = new Vector3(x, y, z);
        _anchoredY = y;
        _xVelocity = 0f;
        _yVelocity = 0f;
        _lookAheadX = 0f;
        _lookAheadVelocity = 0f;
        _previousTargetPosition = followTarget.position;

        if (_camera != null)
            _camera.fieldOfView = baseFov;
    }

    private void UpdateLookAhead()
    {
        float speedFraction = Mathf.Clamp(_currentHorizontalSpeed / fovRampEndSpeed, -1f, 1f);
        float targetLookAhead = speedFraction * lookAheadDistance;
        _lookAheadX = Mathf.SmoothDamp(_lookAheadX, targetLookAhead, ref _lookAheadVelocity, lookAheadSmoothTime);
    }

    private void UpdateVerticalAnchor()
    {
        float playerTargetY = followTarget.position.y + DynamicYOffset;

        if (playerTargetY > _anchoredY + verticalDeadzone)
            _anchoredY = playerTargetY - verticalDeadzone;
        else if (playerTargetY < _anchoredY - verticalDeadzone)
            _anchoredY = playerTargetY + verticalDeadzone;
    }

    private void UpdatePosition()
    {
        float targetX = followTarget.position.x + cameraOffset.x + _lookAheadX;
        float targetY = _anchoredY;
        float targetZ = followTarget.position.z + cameraOffset.z;

        float smoothedX = Mathf.SmoothDamp(transform.position.x, targetX, ref _xVelocity, horizontalSmoothTime);

        float verticalSmoothTime = targetY < transform.position.y ? verticalFallSmoothTime : verticalRiseSmoothTime;
        float smoothedY = Mathf.SmoothDamp(transform.position.y, targetY, ref _yVelocity, verticalSmoothTime);

        if (useLevelBounds)
        {
            smoothedX = Mathf.Clamp(smoothedX, boundsMin.x, boundsMax.x);
            smoothedY = Mathf.Clamp(smoothedY, boundsMin.y, boundsMax.y);
        }

        transform.position = new Vector3(smoothedX, smoothedY, targetZ);
    }

    private void UpdateFov()
    {
        if (_camera == null) return;

        float sprintProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fovRampStartSpeed, fovRampEndSpeed, Mathf.Abs(_currentHorizontalSpeed)));
        float fallProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, -fallFovMaxSpeed, _currentVerticalSpeed));

        float targetFov = baseFov + sprintProgress * sprintFovBonus + fallProgress * fallFovBonus;
        _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, targetFov, ref _fovVelocity, fovSmoothTime);
    }

    private void ApplyScreenShake()
    {
        _trauma = Mathf.Max(0f, _trauma - traumaDecayRate * Time.deltaTime);
        float shake = _trauma * _trauma;

        float shakeX = maxShakeAngle * shake * (Mathf.PerlinNoise(Time.time * 40f, 0f) * 2f - 1f);
        float shakeY = maxShakeAngle * shake * (Mathf.PerlinNoise(0f, Time.time * 40f) * 2f - 1f);

        transform.rotation = Quaternion.Euler(fixedPitch + shakeY, shakeX, 0f);
    }

    private float DynamicYOffset
    {
        get
        {
            if (!useCameraHeightProgression || followTarget == null)
                return cameraOffset.y;

            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(levelStartX, levelEndX, followTarget.position.x));
            return cameraOffset.y + Mathf.Lerp(progressionStartYDelta, progressionEndYDelta, progress);
        }
    }
}