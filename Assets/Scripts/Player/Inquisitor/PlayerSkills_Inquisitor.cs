using System.Collections;
using UnityEngine;

public class PlayerSkills_Inquisitor : MonoBehaviour
{
    public static PlayerSkills_Inquisitor Instance { get; private set; }

    [Header("기본 성향 및 자원")]
    public float maxFaith = 50f;
    public float currentFaith = 0f;

    [Header("스킬 쿨타임 (초)")]
    public float cdQ = 0f;
    public float cdW = 8f;
    public float cdE = 4f;
    public float cdR = 20f;
    public float cdF = 5f;

    [Header("컴포넌트 및 레이어 참조")]
    public LayerMask floorLayer; // 바닥 레이어 (Inspector에서 설정 안 할 시 전체 대상 반응)
    private Animator anim;
    private CharacterController controller; // 이동 및 돌진용

    [Header("시전 중 이동 제어")]
    public bool isCasting = false;

    [Header("E 스킬 에셋")]
    public GameObject chainPrefab;     // 방금 만든 E_ChainProjectile 프리팹
    public Transform chainSpawnPoint; // 캐릭터 손 위치 (없으면 플레이어 중심에서 발사)


    [HideInInspector] public float timerQ;
    [HideInInspector] public float timerW;
    [HideInInspector] public float timerE;
    [HideInInspector] public float timerR;
    [HideInInspector] public float timerF;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 쿨타임 타이머 감소
        if (timerQ > 0) timerQ -= Time.deltaTime;
        if (timerW > 0) timerW -= Time.deltaTime;
        if (timerE > 0) timerE -= Time.deltaTime;
        if (timerR > 0) timerR -= Time.deltaTime;
        if (timerF > 0) timerF -= Time.deltaTime;

        // 키 입력 테스트
        HandleSkillInputs();
    }

    private void HandleSkillInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q) && timerQ <= 0) UseQ();
        if (Input.GetKeyDown(KeyCode.W) && timerW <= 0) UseW();
        if (Input.GetKeyDown(KeyCode.E) && timerE <= 0) UseE();
        if (Input.GetKeyDown(KeyCode.R) && timerR <= 0) UseR();
        if (Input.GetKeyDown(KeyCode.F) && timerF <= 0) UseF();
    }

    // [핵심] 마우스 위치를 향해 즉시 회전하고, 그 바라보는 방향 Vector3를 반환
    private Vector3 RotateToMouseAndGetDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        bool hitSuccess = (floorLayer.value != 0)
            ? Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer)
            : Physics.Raycast(ray, out hit, 100f);

        if (hitSuccess)
        {
            // Y축(높이) 차이로 인해 캐릭터가 위아래로 기우는 것을 방지
            Vector3 targetPoint = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            Vector3 direction = (targetPoint - transform.position).normalized;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                return direction; // 마우스 방향 반환
            }
        }

        return transform.forward; // Raycast 실패 시 현재 전방 반환
    }

    private IEnumerator LockMovementForDuration(float duration)
    {
        isCasting = true;
        yield return new WaitForSeconds(duration);
        isCasting = false;
    }

    #region Q 스킬: [징벌] (2연타 콤보)
    public void UseQ()
    {
        if (isCasting) return;

        RotateToMouseAndGetDirection(); // 마우스 방향 바라보기

        timerQ = cdQ;
        anim.SetTrigger("Skill_Q");

        StartCoroutine(LockMovementForDuration(1.55f));

        AddFaith(15f);
        Debug.Log("Q 스킬 [징벌] 시전!");
    }
    #endregion

    #region W 스킬: [방패 돌파] (가만히 서 있을 때 빙글빙글 도는 문제 해결)
    public void UseW()
    {
        if (isCasting) return;

        // 1. 마우스 방향으로 돌아보고, 돌진할 '고정 방향'을 미리 추출
        Vector3 dashDirection = RotateToMouseAndGetDirection();

        timerW = cdW;
        anim.SetTrigger("Skill_W");

        // 2. 미리 구해둔 고정 방향(dashDirection)으로 돌진 실행!
        StartCoroutine(DashRoutine(1.11f, 5f, dashDirection));
        Debug.Log("W 스킬 [방패 돌파] 시전!");
    }

    private IEnumerator DashRoutine(float duration, float speed, Vector3 direction)
    {
        isCasting = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 시전 순간 고정된 direction 방향으로 일직선 돌진
            if (controller != null && controller.enabled)
            {
                controller.Move(direction * speed * Time.deltaTime);
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isCasting = false;
    }
    #endregion

    #region E 스킬: [이단 구속] (제자리 사슬 투척)
    public void UseE()
    {
        if (isCasting) return;

        // 1. 마우스 위치를 즉시 바라보게 회전
        RotateToMouseAndGetDirection();

        timerE = cdE;
        anim.SetTrigger("Skill_E");

        StartCoroutine(LockMovementForDuration(1.01f));
        StartCoroutine(ExecuteHook());
    }

    private IEnumerator ExecuteHook()
    {
        RotateToMouseAndGetDirection();

        yield return new WaitForSeconds(0.2f);

        Vector3 spawnPos = (chainSpawnPoint != null) ? chainSpawnPoint.position : transform.position + Vector3.up * 1f;

        if (chainPrefab != null)
        {
            GameObject chainObj = Instantiate(chainPrefab, spawnPos, transform.rotation);

            ChainProjectile chain = chainObj.GetComponent<ChainProjectile>();
            if (chain != null)
            {
                chain.Initialize(transform); // 손 인자 없이 캐릭터 Transform만 넘김!
            }
        }
    }
    #endregion

    #region R 스킬: [심판] (점프 내리찍기 / 광역 대미지)
    public void UseR()
    {
        if (isCasting) return;

        RotateToMouseAndGetDirection(); // 마우스 방향 바라보기

        timerR = cdR;
        anim.SetTrigger("Skill_R");

        StartCoroutine(LockMovementForDuration(2.13f)); // 애니메이션 길이에 맞춰 조정 필요
        StartCoroutine(ExecuteSmash());
        Debug.Log("R 스킬 [심판] 시전!");
    }

    private IEnumerator ExecuteSmash()
    {
        yield return new WaitForSeconds(1.06f); // 내리찍는 타격 순간에 대미지

        AddFaith(30f);
        // TODO: 주변 360도 범위 대미지(80) 및 카메라 흔들림 처리
    }
    #endregion

    #region F 스킬: [폭발하는 신념] (신앙심 소모 광역 기절)
    public void UseF()
    {
        if (currentFaith < 20f || isCasting)
        {
            Debug.Log("신앙심이 부족하거나 시전 중입니다!");
            return;
        }

        RotateToMouseAndGetDirection(); // 마우스 방향 바라보기

        timerF = cdF;
        anim.SetTrigger("Skill_F");

        StartCoroutine(LockMovementForDuration(2.11f));

        float consumedFaith = currentFaith;
        currentFaith = 0f;
        Debug.Log($"F 스킬 [폭발하는 신념] 시전! (소모된 신앙심: {consumedFaith})");
    }
    #endregion

    #region 자원(신앙심) 관리
    public void AddFaith(float amount)
    {
        currentFaith = Mathf.Clamp(currentFaith + amount, 0f, maxFaith);
        Debug.Log($"현재 신앙심: {currentFaith} / {maxFaith}");
    }
    #endregion
}