using System;
using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private string speedParam       = "Speed";
    [SerializeField] private string isGroundedParam  = "IsGrounded";
    [SerializeField] private string castTriggerParam = "Cast";

    private Animator _animator;
    private PlayerController _playerController;
    private Coroutine _castRoutine;
    private float _castClipLength;

    public bool IsCasting { get; private set; }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _playerController = GetComponent<PlayerController>();

        if (_animator != null)
        {
            _animator.applyRootMotion = false;

            if (_animator.gameObject.GetComponent<CharacterRootMotionGuard>() == null)
                _animator.gameObject.AddComponent<CharacterRootMotionGuard>();

            foreach (Collider col in _animator.gameObject.GetComponentsInChildren<Collider>(true))
            {
                if (col is CharacterController) continue;
                col.enabled = false;
            }
        }

        CacheCastClipLength();
    }

    private void CacheCastClipLength()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.IndexOf("cast", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _castClipLength = clip.length;
                return;
            }
        }
    }

    private void Update()
    {
        if (_animator == null || _playerController == null) return;

        _animator.applyRootMotion = false;

        _animator.SetFloat(speedParam, _playerController.HorizontalSpeed);
        _animator.SetBool(isGroundedParam, _playerController.IsGrounded);
    }

    public void TriggerCast(Action onComplete = null)
    {
        if (_castRoutine != null)
            StopCoroutine(_castRoutine);

        if (_animator != null)
            _animator.SetTrigger(castTriggerParam);

        _castRoutine = StartCoroutine(WaitForCastComplete(onComplete));
    }

    private IEnumerator WaitForCastComplete(Action onComplete)
    {
        IsCasting = true;

        yield return null;
        yield return null;

        float timeout = Mathf.Max(_castClipLength + 0.5f, 1.5f);
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Cast") ||
                (_animator.IsInTransition(0) && _animator.GetNextAnimatorStateInfo(0).IsName("Cast")))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName("Cast") && state.normalizedTime >= 0.95f)
                break;

            if (!state.IsName("Cast") && !_animator.IsInTransition(0))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        IsCasting = false;
        _castRoutine = null;
        onComplete?.Invoke();
    }
}