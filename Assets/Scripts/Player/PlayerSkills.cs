using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance;
    public PlayerStats stats;
    public GameObject qPrefab, wBudPrefab, wPetalPrefab;
    public Transform qSocket;
    public enum SkillType { Q, W, E, R }
    private NavMeshAgent agent;
    private Animator anim;
    private Coroutine activeSkillCoroutine;

    [Header("F 스킬 전용 UI")]
    public GameObject fActiveEffect;

    [Header("커서 설정")]
    public Texture2D normalCursor;
    public Texture2D targetCursor;
    public Vector2 cursorHotspot = new Vector2(16, 16);

    [Header("스킬 쿨타임")]
    public float qMaxCD = 1f;
    public float wMaxCD = 4f;
    public float eMaxCD = 15f;
    public float rMaxCD = 30f;
    public float fMaxCD = 3f;
    public float qTimer, wTimer, eTimer, rTimer, fTimer;

    private bool isMonarch = false;
    public bool isF_Active = false;

    [Header("스킬 사거리 설정")]
    public float qRange = 10f;
    public float wRange = 8f;
    public float eRange = 12f;
    public float fRange = 10f;
    public float rRange = 10f;

    [Header("R 스킬 이펙트 프리팹")]
    public GameObject magicCirclePrefab;
    public GameObject bloodNovaPrefab;

    public bool IsCasting => activeSkillCoroutine != null;

    void Awake() => Instance = this;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        HandleTimers();

        bool hasTarget = GetTarget(10000f, out RaycastHit hit);
        GameObject targetEnemy = hasTarget ? hit.collider.gameObject : null;
        UpdateCursorVisual(hasTarget);

        // 마우스 우클릭 시 스킬 이동
        if (Input.GetMouseButtonDown(1))
        {
            if (activeSkillCoroutine != null)
            {
                StopCoroutine(activeSkillCoroutine);
                activeSkillCoroutine = null;
            }
        }

        // Q 스킬
        if (Input.GetKeyDown(KeyCode.Q) && qTimer <= 0 && targetEnemy != null)
        {
            if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = StartCoroutine(MoveAndCast(targetEnemy.transform.position, targetEnemy, SkillType.Q));
        }
        // W 스킬
        if (Input.GetKeyDown(KeyCode.W) && wTimer <= 0)
        {
            if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = StartCoroutine(MoveAndCast(GetGroundPos(), null, SkillType.W));
        }
        // E 스킬
        if (Input.GetKeyDown(KeyCode.E) && eTimer <= 0 && targetEnemy != null)
        {
            if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = StartCoroutine(MoveAndCast(targetEnemy.transform.position, targetEnemy, SkillType.E));
        }

        // R, F 스킬 로직
        if (Input.GetKeyDown(KeyCode.R) && rTimer <= 0) StartCoroutine(CastR());
        if (Input.GetKeyDown(KeyCode.F) && fTimer <= 0)
        {
            if (!isF_Active) { if (CanAbsorbCondition()) ToggleF(); }
            else ToggleF();
        }
        if (isF_Active) CheckFAutoTermination();
    }

    // --- Q 스킬 (코루틴화 하여 시선 고정) ---
    IEnumerator CoLaunchQ(GameObject target)
    {
        qTimer = qMaxCD;
        anim.SetTrigger("Skill_Q");

        float cost = isMonarch ? 0.12f : 0.05f;
        stats.UseSkillHp(Mathf.RoundToInt(stats.maxHp * cost));

        float lookDuration = 0.6f; // 애니메이션 재생 중 시선 고정 시간
        float elapsed = 0f;

        while (elapsed < lookDuration)
        {
            if (target != null)
            {
                Vector3 targetPos = target.GetComponent<Collider>().bounds.center;
                LookAtTarget(targetPos, true);

                // 최초 1회만 발사체 생성 (elapsed가 아주 작을 때)
                if (elapsed == 0f)
                {
                    if (isMonarch)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 spawnPos = qSocket.position + qSocket.right * (i - 1) * 0.3f;
                            Quaternion rot = Quaternion.LookRotation(targetPos - spawnPos);
                            GameObject go = Instantiate(qPrefab, spawnPos, rot);
                            go.GetComponent<SpikeProjectile>().InitNonTarget(10f, qRange);
                        }
                    }
                    else
                    {
                        Quaternion rot = Quaternion.LookRotation(targetPos - qSocket.position);
                        GameObject go = Instantiate(qPrefab, qSocket.position, rot);
                        go.GetComponent<SpikeProjectile>().InitNonTarget(10f, qRange);
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        activeSkillCoroutine = null;
    }

    // --- W 스킬 (꽃봉오리 지점 고정) ---
    IEnumerator CoCastW(Vector3 targetPos)
    {
        anim.SetTrigger("Skill_W");

        // 차징 시작 전 꽃봉오리 생성
        GameObject bud = Instantiate(wBudPrefab, targetPos + Vector3.up * 0.1f, Quaternion.identity);
        Transform petalGroup = bud.transform.Find("PetalGroup");

        float charge = 0;
        float maxCharge = 2f;
        float accCost = 0;

        // 차징 중 시선은 계속 '꽃봉오리(targetPos)' 고정
        while (Input.GetKey(KeyCode.W) && charge < maxCharge)
        {
            LookAtTarget(targetPos, true); // 꽃봉오리 응시

            charge += Time.deltaTime;
            float progress = charge / maxCharge;
            bud.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.0f, progress);

            // 꽃잎 애니메이션 로직 (기존 유지)
            if (petalGroup != null)
            {
                for (int i = 0; i < petalGroup.childCount; i++)
                {
                    Transform p = petalGroup.GetChild(i);
                    float startX = 0, addX = 0;
                    if (i >= 0 && i <= 3) { startX = -75f; addX = 15f; }
                    else if (i >= 4 && i <= 7) { startX = -65f; addX = 20f; }
                    else if (i >= 8 && i <= 11) { startX = -55f; addX = 25f; }
                    p.localEulerAngles = new Vector3(startX + (addX * progress), p.localEulerAngles.y, p.localEulerAngles.z);
                }
            }

            float costRate = isMonarch ? 0.15f : 0.1f;
            accCost += stats.maxHp * (0.05f + (costRate * progress)) * Time.deltaTime;
            if (accCost >= 1f) { int burn = Mathf.FloorToInt(accCost); stats.UseSkillHp(burn); accCost -= burn; }
            yield return null;
        }

        wTimer = wMaxCD;
        Destroy(bud);

        // 발사 로직
        int petalCount = 30;
        for (int i = 0; i < petalCount; i++)
        {
            Quaternion randRot = Quaternion.Euler(Random.Range(-40f, 0f), Random.Range(0f, 360f), 0f);
            GameObject pObj = Instantiate(wPetalPrefab, targetPos + Vector3.up * 0.8f, randRot);
            pObj.GetComponent<PetalMove>()?.Init(3f);
        }

        // 데미지 처리
        int finalDmg = Mathf.RoundToInt(isMonarch ? Mathf.Lerp(30, 60, charge / 2) : Mathf.Lerp(15, 45, charge / 2));
        Collider[] enemies = Physics.OverlapSphere(targetPos, 3f);
        foreach (var e in enemies)
        {
            if (e.CompareTag("Enemy")) { e.GetComponent<Enemy>()?.TakeDamage(finalDmg); e.GetComponent<BleedStatus>()?.AddStack(); }
        }

        activeSkillCoroutine = null;
    }

    // --- E 스킬 (적 방향 고정) ---
    IEnumerator CoCastE(GameObject target)
    {
        if (target == null) yield break;

        eTimer = eMaxCD;
        anim.SetTrigger("Skill_E");

        float duration = 0.7f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target != null)
            {
                LookAtTarget(target.transform.position, true);
                if (elapsed == 0f) target.GetComponent<Enemy>()?.ApplyStasis(isMonarch ? 2f : 1f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        activeSkillCoroutine = null;
    }

    // --- R 스킬  ---
    // [매커니즘] 'Monarch(군주)' 상태 변환 시스템.
    // 궁극기(R) 사용 시 스킬의 메커니즘이 강화(예: Q탄환수 증가, E스태시스 시간 증가)되는 
    // 2단계 스킬 구조를 통해 전투의 완급조절을 기획함.
    IEnumerator CastR()
    {
        if (agent != null) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        rTimer = rMaxCD;
        anim.SetTrigger("Skill_R");

        if (magicCirclePrefab != null) Instantiate(magicCirclePrefab, new Vector3(transform.position.x, 0.1f, transform.position.z), Quaternion.identity);

        Collider[] targets = Physics.OverlapSphere(transform.position, rRange);
        int enemyCount = 0;
        foreach (var col in targets) if (col.CompareTag("Enemy")) enemyCount++;
        stats.Heal(Mathf.RoundToInt(stats.maxHp * 0.1f * enemyCount));

        yield return new WaitForSeconds(1.0f);
        if (agent != null) agent.isStopped = false;
        if (bloodNovaPrefab != null) Instantiate(bloodNovaPrefab, transform.position, Quaternion.identity);

        foreach (var col in targets)
        {
            if (col != null && col.CompareTag("Enemy")) { col.GetComponent<Enemy>()?.TakeDamage(30); col.GetComponent<BleedStatus>()?.AddStack(); }
        }

        float originalMaxHp = stats.maxHp;
        stats.maxHp = Mathf.RoundToInt(originalMaxHp * 1.5f);
        isMonarch = true;
        agent.speed *= 1.5f;

        float duration = 10f;
        float elapsed = 0f;
        float tickTimer = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= 1f)
            {
                float drainAmount = stats.maxHp * 0.06f;
                stats.ReduceHpContinuous(drainAmount);

                tickTimer = 0f;
            }
            yield return null;
        }

        isMonarch = false;
        agent.speed /= 1.5f;
        stats.maxHp = originalMaxHp;
        if (stats.currentHp > stats.maxHp) stats.currentHp = stats.maxHp;
    }

    // --- 유틸리티 및 보조 함수 ---
    void HandleTimers() { if (qTimer > 0) qTimer -= Time.deltaTime; if (wTimer > 0) wTimer -= Time.deltaTime; if (eTimer > 0) eTimer -= Time.deltaTime; if (rTimer > 0) rTimer -= Time.deltaTime; if (fTimer > 0) fTimer -= Time.deltaTime; }
    Vector3 GetGroundPos() { Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); return Physics.Raycast(ray, out RaycastHit hit, 100f) ? hit.point : transform.position; }
    bool GetTarget(float range, out RaycastHit hit) { Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out hit, 100f)) return (hit.collider.CompareTag("Enemy") && Vector3.Distance(transform.position, hit.collider.transform.position) <= range); return false; }
    void ToggleF() { isF_Active = !isF_Active; if (fActiveEffect != null) fActiveEffect.SetActive(isF_Active); if (!isF_Active) fTimer = fMaxCD; }

    void LookAtTarget(Vector3 targetPos, bool forceRotate = false)
    {
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            if (forceRotate) agent.updateRotation = false; // 에이전트 간섭 방지
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    bool CanAbsorbCondition() { Collider[] enemies = Physics.OverlapSphere(transform.position, fRange); foreach (var col in enemies) { if (col.CompareTag("Enemy")) { var bleed = col.GetComponent<BleedStatus>(); if (bleed != null && bleed.currentStacks > 0) return true; } } return false; }
    void CheckFAutoTermination() { if (!CanAbsorbCondition()) { isF_Active = false; fTimer = fMaxCD; if (fActiveEffect != null) fActiveEffect.SetActive(false); } }
    void UpdateCursorVisual(bool canTarget) { Cursor.SetCursor(canTarget ? targetCursor : normalCursor, cursorHotspot, CursorMode.Auto); }

    IEnumerator MoveAndCast(Vector3 targetPos, GameObject targetObj, SkillType type)
    {
        float range = GetSkillRange(type);
        while (Vector3.Distance(transform.position, targetPos) > range)
        {
            if (type != SkillType.W && (targetObj == null || !targetObj.activeInHierarchy))
            {
                agent.ResetPath(); activeSkillCoroutine = null; yield break;
            }
            if (targetObj != null) targetPos = targetObj.transform.position;
            agent.SetDestination(targetPos);
            LookAtTarget(targetPos, true); // 이동 중에도 시선 고정
            yield return null;
        }

        // 도착 시 시전 애니메이션 코루틴으로 교체
        if (type == SkillType.Q) yield return StartCoroutine(CoLaunchQ(targetObj));
        else if (type == SkillType.W) yield return StartCoroutine(CoCastW(targetPos));
        else if (type == SkillType.E) yield return StartCoroutine(CoCastE(targetObj));

        activeSkillCoroutine = null;
    }

    float GetSkillRange(SkillType type)
    {
        switch (type)
        {
            case SkillType.Q: return qRange;
            case SkillType.W: return wRange;
            case SkillType.E: return eRange;
            default: return 0f;
        }
    }
}