using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{


    public PlayerState currentState = PlayerState.Idle;
    public LayerMask floorLayer;
    private NavMeshAgent agent;
    private Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.updateRotation = false;
    }

    void Update()
    {
        // 1. 우클릭 이동 처리
        if (Input.GetMouseButton(1)) MoveToMouse();

        // 2. [핵심] 캐릭터의 상대적 이동 속도 계산 (뒷걸음질용)
        // 캐릭터가 바라보는 방향(transform.forward)을 기준으로 현재 속도(agent.velocity)를 변환합니다.
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        // 블렌드 트리의 파라미터에 값을 전달합니다.
        // agent.speed로 나누어 0~1(또는 -1~1) 사이의 값으로 정규화합니다.
        anim.SetFloat("InputX", localVelocity.x / agent.speed, 0.1f, Time.deltaTime);
        anim.SetFloat("InputZ", localVelocity.z / agent.speed, 0.1f, Time.deltaTime);

        // 3. 시선 처리 (스킬 사용 중이 아닐 때만 이동 방향을 바라봄)
        if (PlayerSkills.Instance != null && PlayerSkills.Instance.IsCasting)
        {
            return;
        }

        // 일반 이동 시 회전 (부드럽게 이동 방향 바라보기)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);
        }
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