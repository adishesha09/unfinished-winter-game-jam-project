using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterRootMotionGuard : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Animator>().applyRootMotion = false;
    }

    private void OnAnimatorMove() { }
}

