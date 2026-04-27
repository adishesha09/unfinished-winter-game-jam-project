using UnityEngine;

public class SlipperySurface : MonoBehaviour
{
    [SerializeField] private float slipperiness = 0.85f;
    [SerializeField] private float slipForce    = 5f;

    public float Slipperiness => slipperiness;
    public float SlipForce    => slipForce;
}