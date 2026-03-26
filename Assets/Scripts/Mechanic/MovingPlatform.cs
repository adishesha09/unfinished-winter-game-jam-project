using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop, OneWay }

    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float waitTimeAtWaypoint = 0.5f;
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;
    [SerializeField] private bool startMovingOnPlay = true;

    public Vector3 DeltaPosition { get; private set; }

    private Vector3 _previousPosition;
    private int _currentWaypointIndex;
    private int _direction = 1;

    private void Start()
    {
        if (waypoints.Length > 0)
            transform.position = waypoints[0].position;

        _previousPosition = transform.position;

        if (startMovingOnPlay && waypoints.Length >= 2)
            StartCoroutine(TravelWaypoints());
    }

    private void LateUpdate()
    {
        DeltaPosition = transform.position - _previousPosition;
        _previousPosition = transform.position;
    }

    private IEnumerator TravelWaypoints()
    {
        while (true)
        {
            int nextIndex = NextWaypointIndex();
            Vector3 origin      = transform.position;
            Vector3 destination = waypoints[nextIndex].position;
            float travelTime    = Vector3.Distance(origin, destination) / speed;
            float elapsed       = 0f;

            while (elapsed < travelTime)
            {
                elapsed            += Time.deltaTime;
                float t             = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / travelTime));
                transform.position  = Vector3.Lerp(origin, destination, t);
                yield return null;
            }

            transform.position     = destination;
            _currentWaypointIndex  = nextIndex;

            if (waitTimeAtWaypoint > 0f)
                yield return new WaitForSeconds(waitTimeAtWaypoint);

            if (loopMode == LoopMode.OneWay && _currentWaypointIndex == waypoints.Length - 1)
                yield break;
        }
    }

    private int NextWaypointIndex()
    {
        switch (loopMode)
        {
            case LoopMode.PingPong:
                int next = _currentWaypointIndex + _direction;
                if (next >= waypoints.Length) { _direction = -1; next = _currentWaypointIndex - 1; }
                else if (next < 0)            { _direction =  1; next = 1; }
                return next;

            case LoopMode.Loop:
                return (_currentWaypointIndex + 1) % waypoints.Length;

            default:
                return Mathf.Min(_currentWaypointIndex + 1, waypoints.Length - 1);
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.color = i == 0 ? Color.green : Color.yellow;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);

            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        if (loopMode == LoopMode.Loop && waypoints.Length > 1
            && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }
    }
}

