using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public PlayerState currentState = PlayerState.Idle;
    public LayerMask floorLayer;
    private NavMeshAgent agent;
    private Animator anim;

    // 각 클래스별 스킬 스크립트 참조 (없으면 null)
    private PlayerSkills_Inquisitor inquisitorSkills;
    private PlayerSkills_BloodMage bloodMageSkills;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // 캐릭터에 붙어있는 스킬 스크립트를 각각 검색
        inquisitorSkills = GetComponent<PlayerSkills_Inquisitor>();
        bloodMageSkills = GetComponent<PlayerSkills_BloodMage>();

        agent.updateRotation = false;
    }

    // 현재 어떤 직업이든 "스킬 시전 중인가?"를 체크하는 함수
    private bool IsCastingSkill()
    {
        if (inquisitorSkills != null && inquisitorSkills.isCasting) return true;
        if (bloodMageSkills != null && bloodMageSkills.IsCasting) return true;
        return false;
    }

    void Update()
    {
        //  [핵심] 혈마법사든 이단심판관이든 스킬 시전 중일 때: 이동 정지 및 우클릭 이동 차단
        if (IsCastingSkill())
        {
            StopMovement(); // 진행 중이던 NavMesh 이동 즉시 멈춤

            // 애니메이션 블렌드 트리 파라미터 0으로 초기화 (제자리 서있는 포즈)
            anim.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("InputZ", 0f, 0.1f, Time.deltaTime);
            return; // 아래의 우클릭 이동 및 회전 로직을 실행하지 않고 리턴!
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }

        // 1. 우클릭 이동 처리
        if (Input.GetMouseButton(1)) MoveToMouse();

        // 2. 캐릭터의 상대적 이동 속도 계산 (스킬 사용 동시에 뒷걸음질용)
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        // 블렌드 트리의 파라미터에 값을 전달
        anim.SetFloat("InputX", localVelocity.x / agent.speed, 0.1f, Time.deltaTime);
        anim.SetFloat("InputZ", localVelocity.z / agent.speed, 0.1f, Time.deltaTime);

        // 3. 이동 중일 때만 이동 방향으로 자연스럽게 회전
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

    public void StopMovement()
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