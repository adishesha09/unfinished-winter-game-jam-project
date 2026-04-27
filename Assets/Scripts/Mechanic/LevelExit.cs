using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    public static event Action OnPlayerReachedExit;

    private bool _triggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (other.GetComponent<PlayerController>() == null) return;

        _triggered = true;
        OnPlayerReachedExit?.Invoke();
    }
}

