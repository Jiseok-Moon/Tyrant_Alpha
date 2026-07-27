using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainProjectile : MonoBehaviour
{
    [Header("데미지 및 시전자 정보")]
    public float damage;
    private GameObject ownerPlayer;
    private CharacterController playerController;

    [Header("사슬 에셋 참조")]
    public GameObject chainSegmentPrefab;
    [Header("위치/간격 세팅")]
    public float segmentSpacing = 0.25f;
    public Vector3 segmentOffset = Vector3.zero;

    private Vector3 spawnOriginPos; // 손 위치
    private Vector3 handLocalOffset; // 플레이어 기준 손의 상대 위치

    [Header("스킬 옵션")]
    public float flySpeed = 20f;
    public float pullSpeed = 18f;
    public float maxDistance = 7f;

    private Transform casterTransform;
    private Vector3 startPosition;
    private bool isReturning = false;
    private Transform hitEnemy = null;
    private CharacterController enemyController = null;

    // 대형 몹 특수 기믹 변수
    private bool isHeavyEnemy = false;

    private List<GameObject> spawnedSegments = new List<GameObject>();
    private Vector3 lastSegmentPos;
    private bool rotateSegment = false;

    public void Initialize(Transform caster)
    {
        casterTransform = caster;
        ownerPlayer = caster.gameObject;
        playerController = caster.GetComponent<CharacterController>();

        PlayerSkills_Inquisitor player = caster.GetComponent<PlayerSkills_Inquisitor>();
        if (player != null && player.chainSpawnPoint != null)
        {
            spawnOriginPos = player.chainSpawnPoint.position;
            handLocalOffset = caster.InverseTransformPoint(player.chainSpawnPoint.position);
        }
        else
        {
            spawnOriginPos = caster.position + Vector3.up * 1f;
            handLocalOffset = Vector3.up * 1f;
        }

        startPosition = transform.position;
        lastSegmentPos = spawnOriginPos;
    }

    private void Update()
    {
        if (casterTransform == null) return;

        // 실시간 복귀/도착 목표점
        Vector3 currentHandPos = casterTransform.TransformPoint(handLocalOffset);

        if (isReturning)
        {
            if (hitEnemy != null)
            {
                if (isHeavyEnemy)
                {
                    // [대형 몹] 플레이어가 대형 적의 위치(앞쪽)로 끌려감!
                    Vector3 targetPos = hitEnemy.position - (hitEnemy.position - casterTransform.position).normalized * 1.5f;
                    targetPos.y = casterTransform.position.y; // Y축 고정

                    Vector3 pullDir = (targetPos - casterTransform.position).normalized;

                    if (playerController != null && playerController.enabled)
                    {
                        playerController.Move(pullDir * pullSpeed * Time.deltaTime);
                    }
                    else
                    {
                        casterTransform.position = Vector3.MoveTowards(casterTransform.position, targetPos, pullSpeed * Time.deltaTime);
                    }

                    // 헤드도 적 위치에 정지된 상태로 플레이어를 가리킴
                    transform.position = hitEnemy.position;

                    // 플레이어가 적 근처에 도달했는지 확인
                    if (Vector3.Distance(casterTransform.position, targetPos) < 0.8f)
                    {
                        DestroyAllSegments();
                        Destroy(gameObject);
                    }
                }
                else
                {
                    // [일반 몹] 적이 플레이어의 손 위치로 끌려옴
                    if (enemyController != null && enemyController.enabled)
                    {
                        Vector3 pullDir = (currentHandPos - hitEnemy.position).normalized;
                        enemyController.Move(pullDir * pullSpeed * Time.deltaTime);
                    }
                    else
                    {
                        hitEnemy.position = Vector3.MoveTowards(hitEnemy.position, currentHandPos, pullSpeed * Time.deltaTime);
                    }

                    // 갈고리 헤드 복귀
                    transform.position = Vector3.MoveTowards(transform.position, currentHandPos, pullSpeed * Time.deltaTime);

                    CleanUpSegments(currentHandPos);

                    if (Vector3.Distance(transform.position, currentHandPos) < 0.4f)
                    {
                        DestroyAllSegments();
                        Destroy(gameObject);
                    }
                }
            }
            else
            {
                // 적에게 맞지 않고 빗나갔을 때 복귀
                transform.position = Vector3.MoveTowards(transform.position, currentHandPos, pullSpeed * Time.deltaTime);
                CleanUpSegments(currentHandPos);

                if (Vector3.Distance(transform.position, currentHandPos) < 0.4f)
                {
                    DestroyAllSegments();
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // 전방 날아가기
            transform.Translate(Vector3.forward * flySpeed * Time.deltaTime, Space.Self);

            SpawnChainSegment();

            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            {
                isReturning = true;
            }
        }
    }

    private void SpawnChainSegment()
    {
        if (chainSegmentPrefab == null) return;

        float distFromStart = Vector3.Distance(spawnOriginPos, transform.position);
        if (distFromStart < segmentSpacing) return;

        Vector3 direction = (transform.position - spawnOriginPos).normalized;
        float lastSegmentDist = Vector3.Distance(spawnOriginPos, lastSegmentPos);

        while (distFromStart - lastSegmentDist >= segmentSpacing)
        {
            lastSegmentDist += segmentSpacing;
            Vector3 spawnPos = spawnOriginPos + direction * lastSegmentDist;
            spawnPos += transform.TransformDirection(segmentOffset);

            Quaternion segmentRot = transform.rotation;
            if (rotateSegment)
            {
                segmentRot *= Quaternion.Euler(0f, 0f, 90f);
            }
            rotateSegment = !rotateSegment;

            GameObject segment = Instantiate(chainSegmentPrefab, spawnPos, segmentRot);
            spawnedSegments.Add(segment);

            lastSegmentPos = spawnPos;
        }
    }

    private void CleanUpSegments(Vector3 currentHandPos)
    {
        for (int i = spawnedSegments.Count - 1; i >= 0; i--)
        {
            if (spawnedSegments[i] != null)
            {
                float distHeadToHand = Vector3.Distance(transform.position, currentHandPos);
                float distSegmentToHand = Vector3.Distance(spawnedSegments[i].transform.position, currentHandPos);

                if (distSegmentToHand >= distHeadToHand)
                {
                    Destroy(spawnedSegments[i]);
                    spawnedSegments.RemoveAt(i);
                }
            }
        }
    }

    private void DestroyAllSegments()
    {
        foreach (var seg in spawnedSegments)
        {
            if (seg != null) Destroy(seg);
        }
        spawnedSegments.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isReturning) return;

        if (other.CompareTag("Enemy") || other.GetComponentInParent<Enemy>() != null)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null) enemy = other.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                hitEnemy = enemy.transform;
                enemyController = enemy.GetComponent<CharacterController>();

                // 맞은 적이 HeavyEnemy(대형 몹)인지 체크
                HeavyEnemy heavy = enemy.GetComponent<HeavyEnemy>();
                if (heavy != null)
                {
                    isHeavyEnemy = true;
                    Debug.Log($"[이단 구속] 대형 적({hitEnemy.name}) 적중! 플레이어가 끌려갑니다.");
                }
                else
                {
                    isHeavyEnemy = false;
                    Debug.Log($"[이단 구속] 일반 적({hitEnemy.name}) 적중! 적을 끌어옵니다.");
                }

                isReturning = true;
                enemy.TakeDamage(damage, ownerPlayer);
            }
        }
    }
}