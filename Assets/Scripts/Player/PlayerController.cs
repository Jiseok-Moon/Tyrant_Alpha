using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public PlayerState currentState = PlayerState.Idle;
    public LayerMask floorLayer;
    private NavMeshAgent agent;
    private Animator anim;

    // 각 클래스별 스킬 스크립트 참조
    private PlayerSkills_Inquisitor inquisitorSkills;
    private PlayerSkills_BloodMage bloodMageSkills;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        inquisitorSkills = GetComponent<PlayerSkills_Inquisitor>();
        bloodMageSkills = GetComponent<PlayerSkills_BloodMage>();

        agent.updateRotation = false;
    }

    void Update()
    {
        // 1. 이단심판관 전용: 스킬 시전 중일 때는 이동 정지 및 제자리 대기
        if (inquisitorSkills != null && inquisitorSkills.isCasting)
        {
            StopMovement();

            // 스킬 시전 시 애니메이션 파라미터 즉시 0으로 초기화
            if (anim != null)
            {
                anim.SetFloat("InputX", 0f);
                anim.SetFloat("InputZ", 0f);
            }
            return;
        }

        // 2. S키 정지 기능
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }

        // 3. 우클릭 이동 처리
        if (Input.GetMouseButton(1)) MoveToMouse();

        // 4. 이동 애니메이션 파라미터 전달 (원래대로 로컬 속도 계산 복원!)
        if (anim != null && agent != null && agent.speed > 0f)
        {
            // 캐릭터 회전 기준 상대 속도 계산 (원래 잘 작동하던 코드)
            Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

            float inputX = localVelocity.x / agent.speed;
            float inputZ = localVelocity.z / agent.speed;

            // Damp Time을 0.05f로 살짝 좁혀 반응 속도를 극대화
            anim.SetFloat("InputX", inputX, 0.05f, Time.deltaTime);
            anim.SetFloat("InputZ", inputZ, 0.05f, Time.deltaTime);
        }

        // 5. 이동 중일 때만 이동 방향으로 자연스럽게 회전
        if (agent != null && (agent.velocity.sqrMagnitude > 0.01f || (agent.hasPath && !agent.isStopped)))
        {
            if (agent.velocity.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 15f);
            }
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
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            currentState = PlayerState.Idle;
        }
    }
}