using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public PlayerState currentState = PlayerState.Idle;
    public LayerMask floorLayer;
    private NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Update()
    {
        if (Input.GetMouseButton(1)) MoveToMouse();
    }

    void MoveToMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
        {
            agent.SetDestination(hit.point);
            currentState = PlayerState.Moving;
        }
    }
}