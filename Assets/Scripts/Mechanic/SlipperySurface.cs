using UnityEngine;

public class SlipperySurface : MonoBehaviour
{
    [SerializeField] private float slipperiness = 0.85f;

    public float Slipperiness => slipperiness;
}