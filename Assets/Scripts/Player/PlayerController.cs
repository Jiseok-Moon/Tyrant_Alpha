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

    // [기획 의도] 탑다운 액션 게임의 핵심인 '카이팅(Kiting)' 조작감 구현.
    // InverseTransformDirection을 사용하여 캐릭터가 타겟을 바라본 상태에서도 
    // 마우스 우클릭 이동 시 자연스러운 뒷걸음질 애니메이션(Blend Tree)이 출력되도록 설계함.

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }
        // 1. 우클릭 이동 처리
        if (Input.GetMouseButton(1)) MoveToMouse();

        // 2. 캐릭터의 상대적 이동 속도 계산 (스킬 사용 동시에 뒷걸음질용)
        // 캐릭터가 바라보는 방향(transform.forward)을 기준으로 현재 속도(agent.velocity)를 변환.
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        // 블렌드 트리의 파라미터에 값을 전달.
        // agent.speed로 나누어 0~1(또는 -1~1) 사이의 값으로 정규화.
        anim.SetFloat("InputX", localVelocity.x / agent.speed, 0.1f, Time.deltaTime);
        anim.SetFloat("InputZ", localVelocity.z / agent.speed, 0.1f, Time.deltaTime);

        // 3. 시선 처리 (스킬 사용 중이 아닐 때만 이동 방향을 바라봄)
        if (PlayerSkills.Instance != null && PlayerSkills.Instance.IsCasting)
        {
            return;
        }

        // 이동 중이거나, 에이전트에 아직 가야 할 경로가 남아있을 때만 회전
        if (agent.velocity.sqrMagnitude > 0.01f || (agent.hasPath && !agent.isStopped))
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
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
            agent.SetDestination(hit.point);
            currentState = PlayerState.Moving;
        }
    }

    void StopMovement()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();      // 기존 목적지 제거
            agent.isStopped = true; // 에이전트 정지
            agent.velocity = Vector3.zero; // 물리 관성 제거
            currentState = PlayerState.Idle;
        }
    }
}