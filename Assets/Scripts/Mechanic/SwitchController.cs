using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask switchableMask = ~0;
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private float dragThresholdPixels = 8f;
    [SerializeField] private bool snapToGrid;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private int moveLimit = -1;
    [SerializeField] private bool dragCostsMoves = true;
    [SerializeField] private int maxUndoSteps = 10;

    public event Action<int> OnMovesRemainingChanged;

    public int MovesRemaining => moveLimit < 0 ? int.MaxValue : moveLimit - _movesUsed;

    private bool HasMovesRemaining => moveLimit < 0 || _movesUsed < moveLimit;

    private readonly struct SwitchOperation
    {
        public readonly SwitchableObject ObjectA;
        public readonly Vector3 OriginalPositionA;
        public readonly SwitchableObject ObjectB;
        public readonly Vector3 OriginalPositionB;
        public bool IsDrag => ObjectB == null;

        public SwitchOperation(SwitchableObject a, Vector3 posA, SwitchableObject b = null, Vector3 posB = default)
        {
            ObjectA = a;
            OriginalPositionA = posA;
            ObjectB = b;
            OriginalPositionB = posB;
        }
    }

    private enum InputState { Idle, PendingAction, Dragging }

    private InputState _inputState = InputState.Idle;

    private SwitchableObject _hoveredObject;
    private SwitchableObject _selectedObject;
    private SwitchableObject _dragObject;

    private Plane _dragPlane;
    private Vector3 _dragOffset;
    private Vector3 _dragStartPosition;
    private Vector2 _mouseDownPosition;

    private readonly List<SwitchOperation> _undoStack = new();
    private int _movesUsed;
    private LineRenderer _previewLine;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        SetupPreviewLine();
    }

    private void SetupPreviewLine()
    {
        _previewLine = gameObject.AddComponent<LineRenderer>();
        _previewLine.positionCount = 2;
        _previewLine.startWidth = 0.06f;
        _previewLine.endWidth = 0.06f;
        _previewLine.useWorldSpace = true;
        _previewLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _previewLine.receiveShadows = false;
        _previewLine.enabled = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");

        if (shader != null)
            _previewLine.material = new Material(shader);
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        switch (_inputState)
        {
            case InputState.Idle:
                UpdateHover();
                UpdatePreviewLine();
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    BeginPendingAction();
                if (Mouse.current.rightButton.wasPressedThisFrame)
                    Deselect();
                break;

            case InputState.PendingAction:
                float moved = Vector2.Distance(Mouse.current.position.ReadValue(), _mouseDownPosition);
                if (moved > dragThresholdPixels)
                    TryBeginDrag();
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    ResolveAsClick();
                break;

            case InputState.Dragging:
                UpdateDrag();
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    EndDrag();
                if (Mouse.current.rightButton.wasPressedThisFrame)
                    CancelDrag();
                break;
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame)
            Undo();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            ResetPuzzle();
    }

    private void BeginPendingAction()
    {
        _mouseDownPosition = Mouse.current.position.ReadValue();
        SwitchableObject underCursor = Raycast();
        _dragObject = underCursor != null && !underCursor.IsMoving ? underCursor : null;
        _inputState = InputState.PendingAction;
    }

    private void TryBeginDrag()
    {
        if (_dragObject == null)
        {
            _inputState = InputState.Idle;
            return;
        }

        if (_dragObject.IsMoving)
        {
            _dragObject = null;
            _inputState = InputState.Idle;
            return;
        }

        _dragStartPosition = _dragObject.transform.position;
        _dragPlane = new Plane(-targetCamera.transform.forward, _dragStartPosition);

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (_dragPlane.Raycast(ray, out float distance))
            _dragOffset = _dragStartPosition - ray.GetPoint(distance);

        _dragObject.CancelMovement();
        Deselect();

        if (_hoveredObject != null)
        {
            _hoveredObject.SetVisualState(SwitchVisualState.Normal);
            _hoveredObject = null;
        }

        _dragObject.SetVisualState(SwitchVisualState.Dragging);
        _inputState = InputState.Dragging;
    }

    private void UpdateDrag()
    {
        if (_dragObject == null) return;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!_dragPlane.Raycast(ray, out float distance)) return;

        Vector3 worldPoint = ray.GetPoint(distance) + _dragOffset;

        float x = snapToGrid ? Snap(worldPoint.x) : worldPoint.x;
        float y = snapToGrid ? Snap(worldPoint.y) : worldPoint.y;

        _dragObject.transform.position = new Vector3(x, y, _dragStartPosition.z);
    }

    private void EndDrag()
    {
        if (_dragObject != null)
        {
            bool positionChanged = Vector3.Distance(_dragObject.transform.position, _dragStartPosition) > 0.001f;

            if (positionChanged)
            {
                if (dragCostsMoves && !HasMovesRemaining)
                {
                    _dragObject.MoveTo(_dragStartPosition, arcMultiplier: 0.5f);
                    _dragObject.FlashBlocked();
                    FindFirstObjectByType<PlayerCameraController>()?.AddTrauma(0.2f);
                    _dragObject.SetVisualState(SwitchVisualState.Normal);
                    _dragObject = null;
                    _inputState = InputState.Idle;
                    return;
                }

                PushOperation(new SwitchOperation(_dragObject, _dragStartPosition));

                if (dragCostsMoves)
                {
                    _movesUsed++;
                    OnMovesRemainingChanged?.Invoke(MovesRemaining);
                }
            }

            _dragObject.SetVisualState(SwitchVisualState.Normal);
            _dragObject = null;
        }

        _inputState = InputState.Idle;
    }

    private void CancelDrag()
    {
        if (_dragObject != null)
        {
            _dragObject.MoveTo(_dragStartPosition, arcMultiplier: 0.5f);
            _dragObject.SetVisualState(SwitchVisualState.Normal);
            _dragObject = null;
        }

        _inputState = InputState.Idle;
    }

    private void ResolveAsClick()
    {
        _dragObject = null;
        _inputState = InputState.Idle;
        HandleClick();
    }

    private void HandleClick()
    {
        SwitchableObject clicked = Raycast();

        if (clicked == null)
        {
            Deselect();
            return;
        }

        if (_selectedObject == null)
        {
            Select(clicked);
            return;
        }

        if (clicked == _selectedObject)
        {
            Deselect();
            return;
        }

        if (_selectedObject.CanSwitchWith(clicked))
        {
            if (HasMovesRemaining)
            {
                PerformSwap(_selectedObject, clicked);
                Deselect();
            }
            else
            {
                _selectedObject.FlashBlocked();
                clicked.FlashBlocked();
                FindFirstObjectByType<PlayerCameraController>()?.AddTrauma(0.2f);
            }
        }
        else
        {
            Deselect();
            Select(clicked);
        }
    }

    private void Select(SwitchableObject obj)
    {
        _selectedObject = obj;
        _selectedObject.SetVisualState(SwitchVisualState.Selected);
    }

    private void Deselect()
    {
        if (_selectedObject == null) return;

        _selectedObject.SetVisualState(
            _hoveredObject == _selectedObject ? SwitchVisualState.Hovered : SwitchVisualState.Normal);

        _selectedObject = null;
    }

    private void PerformSwap(SwitchableObject a, SwitchableObject b)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        PushOperation(new SwitchOperation(a, posA, b, posB));
        _movesUsed++;
        OnMovesRemainingChanged?.Invoke(MovesRemaining);

        a.MoveTo(posB, arcMultiplier: 1f);
        b.MoveTo(posA, arcMultiplier: 0.4f);
    }

    private void UpdateHover()
    {
        SwitchableObject newHovered = Raycast();

        if (newHovered == _hoveredObject) return;

        if (_hoveredObject != null && _hoveredObject != _selectedObject)
            _hoveredObject.SetVisualState(SwitchVisualState.Normal);

        _hoveredObject = newHovered;

        if (_hoveredObject != null && _hoveredObject != _selectedObject)
            _hoveredObject.SetVisualState(SwitchVisualState.Hovered);
    }

    private void UpdatePreviewLine()
    {
        if (_selectedObject == null || _hoveredObject == null
            || !_selectedObject.CanSwitchWith(_hoveredObject) || !HasMovesRemaining)
        {
            _previewLine.enabled = false;
            return;
        }

        _previewLine.enabled = true;
        _previewLine.SetPosition(0, _selectedObject.transform.position);
        _previewLine.SetPosition(1, _hoveredObject.transform.position);

        float alpha = Mathf.Lerp(0.3f, 1f, Mathf.PingPong(Time.time * 2f, 1f));
        Color c = new Color(0.25f, 0.8f, 1f, alpha);
        _previewLine.startColor = c;
        _previewLine.endColor = c;
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        SwitchOperation op = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);

        if (op.ObjectA != null)
            op.ObjectA.MoveTo(op.OriginalPositionA, arcMultiplier: 0.5f);

        if (!op.IsDrag && op.ObjectB != null)
            op.ObjectB.MoveTo(op.OriginalPositionB, arcMultiplier: 0.5f);

        _movesUsed = Mathf.Max(0, _movesUsed - 1);
        OnMovesRemainingChanged?.Invoke(MovesRemaining);
    }

    public void ResetPuzzle()
    {
        Deselect();
        _undoStack.Clear();
        _movesUsed = 0;
        OnMovesRemainingChanged?.Invoke(MovesRemaining);

        foreach (SwitchableObject obj in FindObjectsOfType<SwitchableObject>())
            obj.ResetToInitialPosition();
    }

    private void PushOperation(SwitchOperation op)
    {
        _undoStack.Add(op);
        if (_undoStack.Count > maxUndoSteps)
            _undoStack.RemoveAt(0);
    }

    private SwitchableObject Raycast()
    {
        if (targetCamera == null) return null;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, switchableMask))
            return hit.collider.GetComponentInParent<SwitchableObject>();

        return null;
    }

    private float Snap(float value) => Mathf.Round(value / gridSize) * gridSize;
}