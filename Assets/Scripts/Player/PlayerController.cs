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

        if (agent != null) agent.updateRotation = false;
    }

    void Update()
    {
        // 1. 이단심판관 전용: 스킬 시전 중일 때는 이동 정지
        if (inquisitorSkills != null && inquisitorSkills.IsCasting)
        {
            StopMovement();

            // 스킬 시전 중에는 이동 Blend Tree 파라미터를 0으로 초기화
            if (anim != null)
            {
                anim.SetFloat("InputX", 0f, 0.05f, Time.deltaTime);
                anim.SetFloat("InputZ", 0f, 0.05f, Time.deltaTime);
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

        // 4. 이동 애니메이션 파라미터 전달 (로컬 속도 계산)
        if (anim != null && agent != null)
        {
            // NavMeshAgent가 실제로 이동하고자 하는 목표 방향 속도 사용
            Vector3 moveVelocity = agent.desiredVelocity;

            // 캐릭터 기준 로컬 방향으로 변환
            Vector3 localVelocity = transform.InverseTransformDirection(moveVelocity);

            // -1 ~ 1 사이 범위로 Clamp 처리 (0으로 나누기 연산 자체를 제거)
            float inputX = Mathf.Clamp(localVelocity.x, -1f, 1f);
            float inputZ = Mathf.Clamp(localVelocity.z, -1f, 1f);

            // 혹시 모를 예외 대비 안전장치
            if (float.IsNaN(inputX)) inputX = 0f;
            if (float.IsNaN(inputZ)) inputZ = 0f;

            anim.SetFloat("InputX", inputX, 0.1f, Time.deltaTime);
            anim.SetFloat("InputZ", inputZ, 0.1f, Time.deltaTime);
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
            if (agent != null)
            {
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }
                agent.SetDestination(hit.point);
                currentState = PlayerState.Moving;
            }
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