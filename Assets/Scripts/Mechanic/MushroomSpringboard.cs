using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomSpringboard : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 18f;
    [SerializeField] private float cooldownDuration = 0.4f;
    [SerializeField] private string bounceAnimationTrigger = "Bounce";
    [SerializeField] private float squishAmount = 0.35f;
    [SerializeField] private float squishDuration = 0.15f;

    private Animator _animator;
    private Vector3 _originalScale;
    private bool _onCooldown;
    private PlayerCameraController _cameraController;

    private Vector3 _frozenPosition;
    private Quaternion _frozenRotation;

    private Transform[] _frozenTransforms;
    private Vector3[] _frozenLocalPositions;
    private Quaternion[] _frozenLocalRotations;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();

        if (_animator != null)
            _animator.applyRootMotion = false;

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        _originalScale = transform.localScale;
        _cameraController = FindFirstObjectByType<PlayerCameraController>();
        SnapshotFrozenTransforms();
    }

    private void SnapshotFrozenTransforms()
    {
        _frozenPosition = transform.position;
        _frozenRotation = transform.rotation;

        List<Transform> structural = new List<Transform>();

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t == transform) continue;
            if (t.GetComponent<Collider>() != null || t.GetComponent<Renderer>() != null)
                structural.Add(t);
        }

        _frozenTransforms      = structural.ToArray();
        _frozenLocalPositions  = new Vector3[_frozenTransforms.Length];
        _frozenLocalRotations  = new Quaternion[_frozenTransforms.Length];

        for (int i = 0; i < _frozenTransforms.Length; i++)
        {
            _frozenLocalPositions[i] = _frozenTransforms[i].localPosition;
            _frozenLocalRotations[i] = _frozenTransforms[i].localRotation;
        }
    }

    private void LateUpdate()
    {
        transform.position = _frozenPosition;
        transform.rotation = _frozenRotation;

        for (int i = 0; i < _frozenTransforms.Length; i++)
        {
            if (_frozenTransforms[i] == null) continue;
            _frozenTransforms[i].localPosition = _frozenLocalPositions[i];
            _frozenTransforms[i].localRotation = _frozenLocalRotations[i];
        }
    }

    public void TryBounce(PlayerController player)
    {
        if (_onCooldown) return;

        player.ApplyVerticalBoost(launchSpeed);
        _cameraController?.OnSpringboardBounce(squishDuration);

        if (_animator != null
            && _animator.runtimeAnimatorController != null
            && !string.IsNullOrEmpty(bounceAnimationTrigger))
        {
            _animator.SetTrigger(bounceAnimationTrigger);
        }

        StartCoroutine(ProceduralSquish());
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator ProceduralSquish()
    {
        Vector3 squished = new Vector3(
            _originalScale.x * (1f + squishAmount),
            _originalScale.y * (1f - squishAmount),
            _originalScale.z * (1f + squishAmount));

        float half = squishDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale, squished, elapsed / half);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squished, _originalScale, elapsed / half);
            yield return null;
        }

        transform.localScale = _originalScale;
    }

    private IEnumerator CooldownRoutine()
    {
        _onCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        _onCooldown = false;
    }
}