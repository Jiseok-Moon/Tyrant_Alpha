using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerSkills_Inquisitor : MonoBehaviour
{
    public static PlayerSkills_Inquisitor Instance;

    [Header("스킬 피해량 설정")]
    [Tooltip("Q스킬 1회 타격당 피해량 (총 2타 시전)")]
    public float qDamagePerHit = 16f;
    public float wDamage = 8f;
    public float eDamage = 4f;
    public float rDamage = 80f;

    [Header("스킬 쿨타임 (최대 초)")]
    public float qMaxCD = 0f;
    public float wMaxCD = 8f;
    public float eMaxCD = 4f;
    public float rMaxCD = 20f;
    public float fMaxCD = 5f;

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
    public LayerMask floorLayer;
    public Animator anim;
    public CharacterController controller;
    private NavMeshAgent agent;
    private PlayerStats stats;

    [Header("시전 중 이동 제어")]
    public bool isCasting = false;
    private Coroutine activeSkillCoroutine;

    public bool IsCasting => activeSkillCoroutine != null || isCasting;

    [Header("E 스킬 에셋")]
    public GameObject chainPrefab;
    public Transform chainSpawnPoint;

    [Header("Skill F (폭발하는 신념)")]
    public GameObject faithFXPrefab;   // FX_FaithExplosion 프리팹
    public float fSkillRadius = 3f;    // 폭발 이펙트 및 스킬 영향 범위 (반지름)
    public float fSkillDuration = 0.4f; // 이펙트가 퍼지는 시간
    public float fSkillDelay = 0.2f;  // 함성을 지르며 폭발하기까지의 딜레이 (0.15초)

    private void Awake()
    {
        Instance = this;
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (controller == null) controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<PlayerStats>();

        // 게임 시작 시 UpperBody 레이어 Weight를 0으로 강제 초기화
        if (anim != null)
        {
            int upperLayerIndex = anim.GetLayerIndex("UpperBody");
            if (upperLayerIndex != -1)
            {
                anim.SetLayerWeight(upperLayerIndex, 0f);
            }
        }
    }

    private void Update()
    {
        HandleTimers();

        bool hasTarget = GetTarget(10000f, out RaycastHit hit);
        UpdateCursorVisual(hasTarget);

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

    #region 커서 및 타겟 감지
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

    private void DealAreaDamage(Vector3 centerPoint, float radius, float damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(centerPoint, radius);

        foreach (Collider col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null) enemy = col.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage, gameObject);
                }
            }
        }
    }

    #region Q 스킬: [징벌] (2연타 - 0.8초, 1.4초 / 반경 3.5m)
    public void UseQ()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        qTimer = qMaxCD;
        if (anim != null) anim.SetTrigger("Skill_Q");

        activeSkillCoroutine = StartCoroutine(ExecuteQRoutine());
    }

    private IEnumerator ExecuteQRoutine()
    {
        isCasting = true;

        yield return new WaitForSeconds(0.8f);
        DealAreaDamage(transform.position + transform.forward * 1.5f, 3.5f, qDamagePerHit);

        yield return new WaitForSeconds(0.6f);
        DealAreaDamage(transform.position + transform.forward * 1.5f, 3.5f, qDamagePerHit);

        yield return new WaitForSeconds(0.15f);
        isCasting = false;
        activeSkillCoroutine = null;
    }
    #endregion

    #region W 스킬: [방패 돌파] (NavMesh 위치 동기화 및 부드러운 돌진)
    public void UseW()
    {
        if (IsCasting) return;

        Vector3 dashDirection = RotateToMouseAndGetDirection();

        wTimer = wMaxCD;
        if (anim != null) anim.SetTrigger("Skill_W");

        activeSkillCoroutine = StartCoroutine(DashRoutine(1.11f, 5f, dashDirection));
    }

    private IEnumerator DashRoutine(float duration, float speed, Vector3 direction)
    {
        isCasting = true;
        HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        // 1. W 스킬 시작시 상체 레이어 Weight = 1 (상체 방패 모션 덮어쓰기)
        int upperLayerIndex = (anim != null) ? anim.GetLayerIndex("UpperBody") : -1;
        if (upperLayerIndex != -1)
        {
            anim.SetLayerWeight(upperLayerIndex, 1f);
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 moveDelta = direction * speed * Time.deltaTime;

            if (controller != null && controller.enabled)
                controller.Move(moveDelta);
            else
                transform.position += moveDelta;

            // 돌진 타격 판정
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 0.8f, 1.2f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy") && !hitEnemies.Contains(hit.gameObject))
                {
                    hitEnemies.Add(hit.gameObject);
                    Enemy enemy = hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>();
                    if (enemy != null) enemy.TakeDamage(wDamage, gameObject);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null && agent.enabled)
        {
            agent.Warp(transform.position);
            agent.updatePosition = true;
        }

        // 2. W 스킬 종료시 상체 레이어 Weight = 0 (원상 복구)
        if (upperLayerIndex != -1)
        {
            anim.SetLayerWeight(upperLayerIndex, 0f);
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

        yield return new WaitForSeconds(0.2f);

        Vector3 spawnPos = (chainSpawnPoint != null) ? chainSpawnPoint.position : transform.position + Vector3.up * 1f;

        if (chainPrefab != null)
        {
            GameObject chainObj = Instantiate(chainPrefab, spawnPos, transform.rotation);
            ChainProjectile chain = chainObj.GetComponent<ChainProjectile>();
            if (chain != null)
            {
                chain.damage = eDamage;
                chain.Initialize(transform);
            }
        }

        yield return new WaitForSeconds(0.81f);
        isCasting = false;
        activeSkillCoroutine = null;
    }
    #endregion

    #region R 스킬: [심판] (1.06초 타격 / 반경 5.0m)
    public void UseR()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        rTimer = rMaxCD;
        if (anim != null) anim.SetTrigger("Skill_R");

        activeSkillCoroutine = StartCoroutine(ExecuteSmashRoutine());
    }

    private IEnumerator ExecuteSmashRoutine()
    {
        isCasting = true;

        yield return new WaitForSeconds(1.06f);
        DealAreaDamage(transform.position + transform.forward * 2.0f, 5.0f, rDamage);

        yield return new WaitForSeconds(1.07f);
        isCasting = false;
        activeSkillCoroutine = null;
    }
    #endregion

    #region F 스킬: [폭발하는 신념]
    public void UseF()
    {
        if (IsCasting) return;

        RotateToMouseAndGetDirection();

        fTimer = fMaxCD;
        if (anim != null) anim.SetTrigger("Skill_F");

        activeSkillCoroutine = StartCoroutine(ExecuteFRoutine());
    }

    private IEnumerator ExecuteFRoutine()
    {
        isCasting = true;

        // 1. 신앙심 자원 소비 및 보호막 적용 계산
        float consumedFaith = (stats != null) ? stats.ConsumeAllFaith() : 0f;
        float stunDuration = Mathf.Clamp(0.5f + (consumedFaith / 10f) * 0.5f, 0.5f, 3.0f);

        if (stats != null)
        {
            if (consumedFaith > 0)
            {
                stats.AddFaith(consumedFaith);
            }

            stats.SetShieldState(true);
            StartCoroutine(ShieldTimerRoutine(5.0f));
        }

        // 2. 0.15초 선딜레이 대기 (함성 모션 대기)
        yield return new WaitForSeconds(fSkillDelay);

        // 3. 0.15초 후 신앙심 폭발 이펙트 생성
        if (faithFXPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject fxObj = Instantiate(faithFXPrefab, spawnPos, Quaternion.identity);

            FaithFX faithScript = fxObj.GetComponent<FaithFX>();
            if (faithScript != null)
            {
                faithScript.PlayEffect(fSkillRadius, fSkillDuration);
            }
        }

        // 4. 이펙트 생성 시점에 범위(fSkillRadius) 내 적들에게 CC기(기절/정지) 적용
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, fSkillRadius);
        foreach (Collider col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null) enemy = col.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    enemy.ApplyStasis(stunDuration);
                }
            }
        }

        // 5. 모션 후딜레이 정리 (전체 딜레이에서 이미 소모한 fSkillDelay를 차감)
        float remainingTime = Mathf.Max(0f, 1.11f - fSkillDelay);
        yield return new WaitForSeconds(remainingTime);

        isCasting = false;
        activeSkillCoroutine = null;
    }

    private IEnumerator ShieldTimerRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (stats != null)
        {
            stats.SetShieldState(false);
        }
    }
    #endregion

    #region 자원(신앙심) 연동
    public void AddFaith(float amount)
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (stats != null) stats.AddFaith(amount);
    }

    public void AddFaithByDamage(float damageDealt, float ratio = 0.2f)
    {
        float faithToGain = damageDealt * ratio;
        AddFaith(faithToGain);
    }
    #endregion
}