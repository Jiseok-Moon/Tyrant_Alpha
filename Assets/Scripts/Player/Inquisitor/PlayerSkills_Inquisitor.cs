using System.Collections;
using UnityEngine;

public class PlayerSkills_Inquisitor : MonoBehaviour
{
    public static PlayerSkills_Inquisitor Instance;

    [Header("기본 성향 및 자원")]
    public float maxFaith = 50f;
    public float currentFaith = 0f;

    [Header("스킬 쿨타임 (최대 초)")]
    public float qMaxCD = 3f;
    public float wMaxCD = 6f;
    public float eMaxCD = 8f;
    public float rMaxCD = 15f;
    public float fMaxCD = 20f;

    [Header("스킬 실시간 타이머 (UI 연동용)")]
    public float qTimer;
    public float wTimer;
    public float eTimer;
    public float rTimer;
    public float fTimer;

    [Header("커서 설정")]
    public Texture2D normalCursor;
    public Texture2D targetCursor;
    public Vector2 cursorHotspot = new Vector2(16, 16);

    [Header("컴포넌트 및 레이어 참조")]
    public LayerMask floorLayer; // 바닥 레이어
    public Animator anim;
    public CharacterController controller; // 이동 및 돌진용

    [Header("시전 중 이동 제어")]
    public bool isCasting = false;
    private Coroutine activeSkillCoroutine;

    // 혈마법사 스크립트와 동일한 캐스팅 상태 체크 프로퍼티
    public bool IsCasting => activeSkillCoroutine != null || isCasting;

    [Header("E 스킬 에셋")]
    public GameObject chainPrefab;     // E_ChainProjectile 프리팹
    public Transform chainSpawnPoint; // 캐릭터 손 위치

    private void Awake()
    {
        Instance = this;
        if (anim == null) anim = GetComponent<Animator>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 쿨타임 타이머 감소
        HandleTimers();

        // 2. 마우스 타겟 감지 및 커서 비주얼 업데이트 (BloodMage 방식 연동)
        bool hasTarget = GetTarget(10000f, out RaycastHit hit);
        UpdateCursorVisual(hasTarget);

        // 3. 키 입력 처리
        HandleSkillInputs();
    }

    private void HandleTimers()
    {
        if (qTimer > 0) qTimer -= Time.deltaTime;
        if (wTimer > 0) wTimer -= Time.deltaTime;
        if (eTimer > 0) eTimer -= Time.deltaTime;
        if (rTimer > 0) rTimer -= Time.deltaTime;
        if (fTimer > 0) fTimer -= Time.deltaTime;
    }

    private void HandleSkillInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q) && qTimer <= 0) UseQ();
        if (Input.GetKeyDown(KeyCode.W) && wTimer <= 0) UseW();
        if (Input.GetKeyDown(KeyCode.E) && eTimer <= 0) UseE();
        if (Input.GetKeyDown(KeyCode.R) && rTimer <= 0) UseR();
        if (Input.GetKeyDown(KeyCode.F) && fTimer <= 0) UseF();
    }

    #region 커서 및 타겟 감지 (BloodMage 공통)
    private bool GetTarget(float range, out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100f))
        {
            return (hit.collider.CompareTag("Enemy") && Vector3.Distance(transform.position, hit.collider.transform.position) <= range);
        }
        return false;
    }

    private void UpdateCursorVisual(bool canTarget)
    {
        Cursor.SetCursor(canTarget ? targetCursor : normalCursor, cursorHotspot, CursorMode.Auto);
    }
    #endregion

    // 마우스 위치를 향해 즉시 회전하고, 그 바라보는 방향 Vector3를 반환
    public Vector3 RotateToMouseAndGetDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        bool hitSuccess = (floorLayer.value != 0)
            ? Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer)
            : Physics.Raycast(ray, out hit, 100f);

        if (hitSuccess)
        {
            Vector3 targetPoint = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            Vector3 direction = (targetPoint - transform.position).normalized;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                return direction;
            }
        }

        return transform.forward;
    }

    private IEnumerator LockMovementForDuration(float duration)
    {
        isCasting = true;
        yield return new WaitForSeconds(duration);
        isCasting = false;
        activeSkillCoroutine = null;
    }

    #region Q 스킬: [징벌]
    public void UseQ()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        qTimer = qMaxCD;
        if (anim != null) anim.SetTrigger("Skill_Q");

        activeSkillCoroutine = StartCoroutine(LockMovementForDuration(1.55f));

        AddFaith(15f);
        Debug.Log("Q 스킬 [징벌] 시전!");
    }
    #endregion

    #region W 스킬: [방패 돌파]
    public void UseW()
    {
        if (IsCasting) return;

        Vector3 dashDirection = RotateToMouseAndGetDirection();

        wTimer = wMaxCD;
        if (anim != null) anim.SetTrigger("Skill_W");

        activeSkillCoroutine = StartCoroutine(DashRoutine(1.11f, 5f, dashDirection));
        Debug.Log("W 스킬 [방패 돌파] 시전!");
    }

    private IEnumerator DashRoutine(float duration, float speed, Vector3 direction)
    {
        isCasting = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
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
        activeSkillCoroutine = null;
    }
    #endregion

    #region E 스킬: [이단 구속]
    public void UseE()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        eTimer = eMaxCD;
        if (anim != null) anim.SetTrigger("Skill_E");

        activeSkillCoroutine = StartCoroutine(ExecuteHookRoutine());
    }

    private IEnumerator ExecuteHookRoutine()
    {
        isCasting = true;

        RotateToMouseAndGetDirection();

        yield return new WaitForSeconds(0.2f);

        Vector3 spawnPos = (chainSpawnPoint != null) ? chainSpawnPoint.position : transform.position + Vector3.up * 1f;

        if (chainPrefab != null)
        {
            GameObject chainObj = Instantiate(chainPrefab, spawnPos, transform.rotation);
            ChainProjectile chain = chainObj.GetComponent<ChainProjectile>();
            if (chain != null)
            {
                chain.Initialize(transform);
            }
        }

        yield return new WaitForSeconds(0.81f);
        isCasting = false;
        activeSkillCoroutine = null;
    }
    #endregion

    #region R 스킬: [심판]
    public void UseR()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        rTimer = rMaxCD;
        if (anim != null) anim.SetTrigger("Skill_R");

        activeSkillCoroutine = StartCoroutine(ExecuteSmashRoutine());
        Debug.Log("R 스킬 [심판] 시전!");
    }

    private IEnumerator ExecuteSmashRoutine()
    {
        isCasting = true;

        yield return new WaitForSeconds(1.06f);

        AddFaith(30f);

        yield return new WaitForSeconds(1.07f);
        isCasting = false;
        activeSkillCoroutine = null;
    }
    #endregion

    #region F 스킬: [폭발하는 신념]
    public void UseF()
    {
        if (currentFaith < 20f || IsCasting)
        {
            Debug.Log("신앙심이 부족하거나 시전 중입니다!");
            return;
        }

        RotateToMouseAndGetDirection();

        fTimer = fMaxCD;
        if (anim != null) anim.SetTrigger("Skill_F");

        activeSkillCoroutine = StartCoroutine(LockMovementForDuration(2.11f));

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