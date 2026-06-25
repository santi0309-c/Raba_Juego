using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AC_PracticeBotNavMesh : MonoBehaviour
{
    [Header("Modo práctica opcional")]
    public Transform playerTarget;
    public float detectionRange = 7f;
    public Transform[] waypoints;

    private NavMeshAgent agent;
    private int waypointIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= detectionRange)
        {
            agent.SetDestination(playerTarget.position);
            return;
        }

        Patrol();
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[waypointIndex].position);
        }
    }
}
