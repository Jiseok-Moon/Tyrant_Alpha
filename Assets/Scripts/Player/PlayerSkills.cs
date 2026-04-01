using System.Collections;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance;
    public PlayerStats stats;
    public GameObject qPrefab, wBudPrefab, wPetalPrefab;
    public Transform qSocket;
    public enum SkillType { Q, W, E }
    private NavMeshAgent agent;
    private Coroutine activeSkillCoroutine;

    [Header("F 스킬 전용 UI")]
    public GameObject fActiveEffect;

    [Header("커서 설정")]
    public Texture2D normalCursor;  // 기본 커서 (평상시)
    public Texture2D targetCursor;  // 공격 커서 (적 위를 지날 때)
    public Vector2 cursorHotspot = new Vector2(16, 16); // 커서의 클릭 중심점

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


    void Awake() => Instance = this;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        HandleTimers();

        // 적 위에 마우스 올라갔는지 확인
        bool hasTarget = GetTarget(10000f, out RaycastHit hit);
        GameObject targetEnemy = hasTarget ? hit.collider.gameObject : null;
        UpdateCursorVisual(hasTarget);

        if (Input.GetMouseButtonDown(1))
        {
            if (activeSkillCoroutine != null)
            {
                StopCoroutine(activeSkillCoroutine);
                activeSkillCoroutine = null;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) && qTimer <= 0 && targetEnemy != null)
        {
            if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = StartCoroutine(MoveAndCast(targetEnemy.transform.position, targetEnemy, SkillType.Q));
        }
        if (Input.GetKeyDown(KeyCode.W) && wTimer <= 0)
        {
            if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = StartCoroutine(MoveAndCast(GetGroundPos(), null, SkillType.W));
        }
        if (Input.GetKeyDown(KeyCode.E) && eTimer <= 0)
        {
            if (targetEnemy != null)
            {
                // 기존에 돌고 있던 스킬 이동 코루틴이 있다면 취소 (새 명령 우선)
                if (activeSkillCoroutine != null) StopCoroutine(activeSkillCoroutine);
                // [E 스킬 예약] 적의 위치로 이동 후 E를 시전하라!
                activeSkillCoroutine = StartCoroutine(MoveAndCast(targetEnemy.transform.position, targetEnemy, SkillType.E));
            }
        }
        if (Input.GetKeyDown(KeyCode.R) && rTimer <= 0) StartCoroutine(CastR());
        if (Input.GetKeyDown(KeyCode.F) && fTimer <= 0)
        {
            // [보완] 켤 때만 조건을 체크합니다. (이미 켜져 있다면 그냥 꺼지도록)
            if (!isF_Active)
            {
                // 주변에 출혈 중인 적이 있을 때만 ToggleF 실행
                if (CanAbsorbCondition())
                {
                    ToggleF();
                }
                else
                {
                    Debug.Log("주변에 출혈 중인 적이 없어 스킬을 켤 수 없습니다.");
                }
            }
            else
            {
                ToggleF(); // 켜져 있는 상태라면 조건 없이 끔
            }
        }
        if (isF_Active)
        {
            CheckFAutoTermination();
        }

    }

    // --- Q 스킬: targetPos 통일 ---
    void LaunchQ(GameObject target)
    {
        qTimer = qMaxCD;
        float cost = isMonarch ? 0.12f : 0.05f;
        stats.UseSkillHp(Mathf.RoundToInt(stats.maxHp * cost));

        Vector3 targetPos = target.GetComponent<Collider>().bounds.center;
        LookAtTarget(targetPos);

        // 3. 발사 로직
        if (isMonarch)
        {
            // 군주 모드: 타겟을 중심으로 3갈래 확산 사격 (유도 기능 넣기 전 단계)
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
            // 일반 모드: 타겟에게 직선으로 정확히 발사
            Quaternion rot = Quaternion.LookRotation(targetPos - qSocket.position);
            GameObject go = Instantiate(qPrefab, qSocket.position, rot);
            go.GetComponent<SpikeProjectile>().InitNonTarget(10f, qRange);
        }
}

    // --- W 스킬: 차징 후 30개 꽃잎 랜덤 비산 ---
    IEnumerator CastW(Vector3 targetPos)
    {
        if (Vector3.Distance(transform.position, targetPos) > wRange) yield break;

        LookAtTarget(targetPos);

        GameObject bud = Instantiate(wBudPrefab, targetPos + Vector3.up * 0.1f, Quaternion.identity);
        Transform petalGroup = bud.transform.Find("PetalGroup");

        float charge = 0;
        float maxCharge = 2f;
        float accCost = 0;

        while (Input.GetKey(KeyCode.W) && charge < maxCharge)
        {
            charge += Time.deltaTime;
            float progress = charge / maxCharge;
            bud.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.0f, progress);

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
            if (accCost >= 1f)
            {
                int burn = Mathf.FloorToInt(accCost);
                stats.UseSkillHp(burn);
                accCost -= burn;
            }
            yield return null;
        }

        wTimer = wMaxCD;
        Destroy(bud);

        int petalCount = 30;
        float explosionRadius = 3f;

        for (int i = 0; i < petalCount; i++)
        {
            // 꽃잎 시인성을 위해 높이 보정 (+0.8f)
            Quaternion randRot = Quaternion.Euler(Random.Range(-40f, 0f), Random.Range(0f, 360f), 0f);
            GameObject pObj = Instantiate(wPetalPrefab, targetPos + Vector3.up * 0.8f, randRot);

            PetalMove pm = pObj.GetComponent<PetalMove>();
            if (pm != null)
            {
                pm.speed = Random.Range(15f, 30f);
                pm.Init(explosionRadius);
            }
        }

        int finalDmg = Mathf.RoundToInt(isMonarch ? Mathf.Lerp(30, 60, charge / 2) : Mathf.Lerp(15, 45, charge / 2));
        Collider[] enemies = Physics.OverlapSphere(targetPos, explosionRadius);
        foreach (var e in enemies)
        {
            if (e.CompareTag("Enemy"))
            {
                e.GetComponent<Enemy>()?.TakeDamage(finalDmg);
                e.GetComponent<BleedStatus>()?.AddStack();
            }
        }
    }

    // --- 유틸리티 로직 ---
    IEnumerator CastE(GameObject target)
    {
        if (target == null) yield break;
        eTimer = eMaxCD;
        yield return new WaitForSeconds(0.1f);
        if (target != null)
        {
            target.GetComponent<Enemy>()?.ApplyStasis(isMonarch ? 2f : 1f);
        }
    }
    IEnumerator CastR()
    {

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        rTimer = rMaxCD;
        Vector3 magicCirclePos = new Vector3(transform.position.x, 0.1f, transform.position.z);
        // 주변 적 감지 및 피 흡수 (rRange 사용)
        Collider[] targets = Physics.OverlapSphere(transform.position, rRange);
        int enemyCount = 0;

        if (magicCirclePrefab != null)
        {
            GameObject mc = Instantiate(magicCirclePrefab, magicCirclePos, Quaternion.identity);
        }

        foreach (var col in targets)
        {
            if (col.CompareTag("Enemy"))
            {
                enemyCount++; // 피 흡수 로직 추가?
            }
        }

        stats.Heal(Mathf.RoundToInt(stats.maxHp * 0.1f * enemyCount));

        yield return new WaitForSeconds(1.0f);

        if (agent != null) agent.isStopped = false;

        if (bloodNovaPrefab != null)
        {
            Instantiate(bloodNovaPrefab, transform.position, Quaternion.identity);
        }

        int rDamage = 30;
        foreach (var col in targets)
        {
            if (col != null && col.CompareTag("Enemy"))
            {
                col.GetComponent<Enemy>()?.TakeDamage(rDamage);
            }
            BleedStatus bleed = col.GetComponent<BleedStatus>();
            if (bleed != null)
            {
                bleed.AddStack();
            }
        }

        // 최대 체력 1.5배 증가 및 현재 체력 보정
        float originalMaxHp = stats.maxHp;
        stats.maxHp = Mathf.RoundToInt(originalMaxHp * 1.5f);
        stats.currentHp = Mathf.Min(stats.currentHp, stats.maxHp);

        isMonarch = true;
        agent.speed *= 1.5f;

        float duration = 10f;
        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            // 1초마다 최대 체력의 6% 소모
            if (tickTimer >= 1f)
            {
                stats.UseSkillHp(Mathf.RoundToInt(stats.maxHp * 0.06f));
                tickTimer = 0f;
            }
            yield return null;
        }

        // 4. [종료] 원래 상태로 복귀
        isMonarch = false;
        agent.speed /= 1.5f;
        stats.maxHp = originalMaxHp; // 원래 최대 체력으로 복구

        // 현재 체력이 줄어든 최대치보다 많으면 깎기
        if (stats.currentHp > stats.maxHp)
        {
            stats.currentHp = stats.maxHp;
        }
    }
    void HandleTimers() { if (qTimer > 0) qTimer -= Time.deltaTime; if (wTimer > 0) wTimer -= Time.deltaTime; if (eTimer > 0) eTimer -= Time.deltaTime; if (rTimer > 0) rTimer -= Time.deltaTime; if (fTimer > 0) fTimer -= Time.deltaTime; }
    Vector3 GetGroundPos() { Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); return Physics.Raycast(ray, out RaycastHit hit, 100f) ? hit.point : transform.position; }
    bool GetTarget(float range, out RaycastHit hit) { Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out hit, 100f)) return (hit.collider.CompareTag("Enemy") && Vector3.Distance(transform.position, hit.collider.transform.position) <= range); return false; }
    void ToggleF()
    {
        isF_Active = !isF_Active;

        // [추가] 하얀 테두리 이펙트(F_ActiveEffect) 제어
        // 인스펙터에서 fActiveEffect 변수를 만들거나 직접 찾아서 연결해야 합니다.
        if (fActiveEffect != null)
        {
            fActiveEffect.SetActive(isF_Active);
        }

        if (!isF_Active)
        {
            fTimer = fMaxCD;
        }
    }

    void LookAtTarget(Vector3 targetPos)
    {
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0; // 발밑을 보지 않도록 수평 고정
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    bool CanAbsorbCondition()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, fRange);
        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                var bleed = col.GetComponent<BleedStatus>();
                if (bleed != null && bleed.currentStacks > 0) return true;
            }
        }
        return false;
    }
    void CheckFAutoTermination()
    {
        if (!CanAbsorbCondition()) // 위에서 만든 공용 조건 함수 사용
        {
            isF_Active = false;
            fTimer = fMaxCD;

            // [추가] 자동 종료될 때도 이펙트를 확실히 꺼줍니다.
            if (fActiveEffect != null) fActiveEffect.SetActive(false);
        }
    }
    void UpdateCursorVisual(bool canTarget)
    {
        if (canTarget)
        {
            // 준비하신 타겟 커서로 변경
            Cursor.SetCursor(targetCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            // 준비하신 기본 커서로 복구
            Cursor.SetCursor(normalCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    IEnumerator MoveAndCast(Vector3 targetPos, GameObject targetObj, SkillType type)
    {
        float range = GetSkillRange(type); // 스킬별 사거리 가져오기

        // [핵심] 사거리 밖이라면 도착할 때까지 계속 갱신하며 이동
        while (Vector3.Distance(transform.position, targetPos) > range)
        {
            // 우클릭 취소 등으로 인해 타겟이 사라지면 중단
            if (type != SkillType.W && (targetObj == null || !targetObj.activeInHierarchy))
            {
                agent.ResetPath(); // 이동 중지
                activeSkillCoroutine = null;
                yield break;
            }

            // 적이 움직이면 실시간으로 목적지 갱신
            if (targetObj != null) targetPos = targetObj.transform.position;

            // NavMesh에게 목적지 설정
            agent.SetDestination(targetPos);

            yield return null; // 다음 프레임까지 대기
        }

        // 사거리 진입 완료! 이동 즉시 중지
        agent.ResetPath();
        LookAtTarget(targetPos); // 시전 방향으로 몸 돌리기

        // 스킬 시전
        if (type == SkillType.Q) LaunchQ(targetObj);
        else if (type == SkillType.W) StartCoroutine(CastW(targetPos));
        else if (type == SkillType.E) StartCoroutine(CastE(targetObj));

        activeSkillCoroutine = null;
    }
    float GetSkillRange(SkillType type)
    {
        switch (type)
        {
            case SkillType.Q: return qRange; // 15f
            case SkillType.W: return wRange; // 10f
            case SkillType.E: return eRange; // 20f
            default: return 0f;
        }
    }
}