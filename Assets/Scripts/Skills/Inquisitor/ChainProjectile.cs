using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainProjectile : MonoBehaviour
{
    [Header("사슬 에셋 참조")]
    public GameObject chainSegmentPrefab; // 3D 사슬 고리 프리팹
    public float segmentSpacing = 0.35f;   // 사슬 고리 간격 (0.3~0.4 권장)

    [Header("위치/회전 세팅")]
    public Vector3 segmentOffset = Vector3.zero; // 오프셋 미세조정용

    [Header("스킬 옵션")]
    public float flySpeed = 20f;          // 뻗어나가는 속도
    public float pullSpeed = 18f;         // 당겨오는 속도
    public float maxDistance = 7f;        // 짧은 사거리 (7m)

    private Transform casterTransform;
    private bool isReturning = false;
    private Transform hitEnemy = null;

    private List<GameObject> spawnedSegments = new List<GameObject>();
    private Vector3 lastSegmentPos;
    private bool rotateSegment = false;

    // 고정해둘 위치 데이터
    private Vector3 spawnOriginPos; // 발사 시점 좌표
    private float fixedYHeight;     // 발사될 때 고정된 Y 높이

    private Vector3 flyDirection;

    public void Initialize(Transform caster)
    {
        casterTransform = caster;

        PlayerSkills_Inquisitor player = caster.GetComponent<PlayerSkills_Inquisitor>();
        if (player != null && player.chainSpawnPoint != null)
        {
            spawnOriginPos = player.chainSpawnPoint.position;
        }
        else
        {
            spawnOriginPos = caster.position + Vector3.up * 1.0f;
        }

        fixedYHeight = spawnOriginPos.y;

        // 시작 위치 높이 강제 수평 고정
        Vector3 startPos = transform.position;
        startPos.y = fixedYHeight;
        transform.position = startPos;

        // 발사 방향을 3D 상의 완전한 수평(Y = 0) 정면으로 고정!
        flyDirection = transform.forward;
        flyDirection.y = 0;
        flyDirection.Normalize();

        lastSegmentPos = transform.position;
    }

    private void Update()
    {
        if (casterTransform == null) return;

        if (isReturning)
        {
            // [핵심] 캐릭터나 손의 흔들리는 실시간 위치를 추적하지 않고,
            // 사슬 고리들이 생성되었던 최초 시작 지점(spawnOriginPos)으로 일직선 복귀!
            Vector3 returnTarget = spawnOriginPos;

            // 적이 잡혔다면 최초 발사 지점으로 당김
            if (hitEnemy != null)
            {
                hitEnemy.position = Vector3.MoveTowards(hitEnemy.position, returnTarget, pullSpeed * Time.deltaTime);
            }

            // 갈고리 헤드가 사슬 고리 라인을 '역방향 일직선'으로 따라 복귀!
            transform.position = Vector3.MoveTowards(transform.position, returnTarget, pullSpeed * Time.deltaTime);

            // 복귀하면서 지나친 사슬 고리 제거 (이전과 동일)
            CleanUpSegments();

            // 최초 발사 위치(고리 시작점)에 도달하면 소멸
            if (Vector3.Distance(transform.position, returnTarget) < 0.3f)
            {
                DestroyAllSegments();
                Destroy(gameObject);
            }
        }
        else
        {
            // [발사 단계] 고정된 수평 방향(flyDirection)으로 날아감
            transform.position += flyDirection * flySpeed * Time.deltaTime;

            Vector3 currentPos = transform.position;
            currentPos.y = fixedYHeight;
            transform.position = currentPos;

            SpawnChainSegment();

            if (Vector3.Distance(spawnOriginPos, transform.position) >= maxDistance)
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

        float lastSegmentDist = Vector3.Distance(spawnOriginPos, lastSegmentPos);
        Vector3 direction = (transform.position - spawnOriginPos).normalized;

        while (distFromStart - lastSegmentDist >= segmentSpacing)
        {
            lastSegmentDist += segmentSpacing;

            // Y 높이는 무조건 fixedYHeight로 일정하게 생성!
            Vector3 spawnPos = spawnOriginPos + direction * lastSegmentDist;
            spawnPos.y = fixedYHeight;
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

    private void CleanUpSegments()
    {
        for (int i = spawnedSegments.Count - 1; i >= 0; i--)
        {
            if (spawnedSegments[i] != null)
            {
                float distHeadToCaster = Vector3.Distance(transform.position, casterTransform.position);
                float distSegmentToCaster = Vector3.Distance(spawnedSegments[i].transform.position, casterTransform.position);

                if (distSegmentToCaster >= distHeadToCaster)
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

        if (other.CompareTag("Enemy"))
        {
            hitEnemy = other.transform;
            isReturning = true;
        }
    }
}