using System.Collections;
using UnityEngine;

public enum SwitchVisualState { Normal, Hovered, Selected, Dragging }

[RequireComponent(typeof(Collider))]
public class SwitchableObject : MonoBehaviour
{
    [SerializeField] private int switchGroup = 0;
    [SerializeField] private float swapDuration = 0.5f;
    [SerializeField] private float minArcHeight = 1.5f;
    [SerializeField] private Color hoverTint     = new Color(1f,    0.85f, 0.25f);
    [SerializeField] private Color selectedTint  = new Color(0.25f, 0.8f,  1f);
    [SerializeField] private Color draggingTint  = new Color(0.25f, 1f,    0.45f);
    [SerializeField] private Color blockedTint   = new Color(1f,    0.15f, 0.15f);
    [SerializeField] private float blockedFlashDuration = 0.35f;

    private static readonly int BaseColorId   = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private Color _baseColor;
    private Coroutine _moveCoroutine;
    private Coroutine _flashCoroutine;
    private Vector3 _initialPosition;
    private SwitchVisualState _currentState;

    public int SwitchGroup => switchGroup;
    public bool IsMoving    => _moveCoroutine != null;

    private void Awake()
    {
        _renderer      = GetComponentInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _baseColor     = ResolveBaseColor();
    }

    private void Start()
    {
        _initialPosition = transform.position;
    }

    public void ResetToInitialPosition() => MoveTo(_initialPosition, arcMultiplier: 0.6f);

    private Color ResolveBaseColor()
    {
        if (_renderer == null || _renderer.sharedMaterial == null)
            return Color.white;

        if (_renderer.sharedMaterial.HasProperty(BaseColorId))
            return _renderer.sharedMaterial.GetColor(BaseColorId);

        if (_renderer.sharedMaterial.HasProperty(LegacyColorId))
            return _renderer.sharedMaterial.GetColor(LegacyColorId);

        return Color.white;
    }

    public bool CanSwitchWith(SwitchableObject other)
    {
        if (other == this || IsMoving || other.IsMoving) return false;
        if (switchGroup == 0 || other.switchGroup == 0) return true;
        return switchGroup == other.switchGroup;
    }

    public void SetVisualState(SwitchVisualState state)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        _currentState = state;
        ApplyTint(TintForState(state));
    }

    public void FlashBlocked()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(BlockedFlash());
    }

    private IEnumerator BlockedFlash()
    {
        ApplyTint(blockedTint);
        yield return new WaitForSeconds(blockedFlashDuration);
        ApplyTint(TintForState(_currentState));
        _flashCoroutine = null;
    }

    private Color TintForState(SwitchVisualState state) => state switch
    {
        SwitchVisualState.Hovered  => hoverTint,
        SwitchVisualState.Selected => selectedTint,
        SwitchVisualState.Dragging => draggingTint,
        _                          => _baseColor
    };

    private void ApplyTint(Color tint)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, tint);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    public void MoveTo(Vector3 target, float arcMultiplier = 1f)
    {
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(MoveAlongArc(target, arcMultiplier));
    }

    public void CancelMovement()
    {
        if (_moveCoroutine == null) return;
        StopCoroutine(_moveCoroutine);
        _moveCoroutine = null;
    }

    private IEnumerator MoveAlongArc(Vector3 target, float arcMultiplier)
    {
        Vector3 start    = transform.position;
        float distance   = Vector3.Distance(start, target);
        float arcHeight  = Mathf.Max(minArcHeight, distance * 0.25f) * arcMultiplier;
        Vector3 peak     = (start + target) * 0.5f + Vector3.up * arcHeight;
        float elapsed    = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / swapDuration);
            transform.position = Vector3.Lerp(
                Vector3.Lerp(start, peak, t),
                Vector3.Lerp(peak, target, t), t);
            yield return null;
        }

        transform.position = target;
        _moveCoroutine = null;
    }
}