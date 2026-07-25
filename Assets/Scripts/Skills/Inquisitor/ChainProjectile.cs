using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainProjectile : MonoBehaviour
{
    [Header("사슬 에셋 참조")]
    public GameObject chainSegmentPrefab; // 3D 사슬 고리 프리팹
    [Header("위치/간격 세팅")]
    public float segmentSpacing = 0.25f; // 사슬 고리 간격 (0.3~0.4 추천)
    public Vector3 segmentOffset = Vector3.zero; // 높이 미세조정용

    private Vector3 spawnOriginPos; // 발사할 때의 손 위치 저장

    [Header("스킬 옵션")]
    public float flySpeed = 20f;          // 뻗어나가는 속도
    public float pullSpeed = 18f;         // 당겨오는 속도
    public float maxDistance = 7f;        // 짧은 사거리 (약 7m)

    private Transform casterTransform;
    private Vector3 startPosition;
    private bool isReturning = false;
    private Transform hitEnemy = null;

    // 생성된 사슬 고리 마디들을 담아둘 리스트
    private List<GameObject> spawnedSegments = new List<GameObject>();
    private Vector3 lastSegmentPos;

    public void Initialize(Transform caster)
    {
        casterTransform = caster;

        // 중요: 플레이어 스킬 스크립트에서 손 위치(chainSpawnPoint)를 넘겨주는 것이 좋습니다.
        // 만약 안 넘겨주면, 현재 캐릭터 가슴 높이를 시작점으로 잡습니다.
        PlayerSkills_Inquisitor player = caster.GetComponent<PlayerSkills_Inquisitor>();
        if (player != null && player.chainSpawnPoint != null)
        {
            spawnOriginPos = player.chainSpawnPoint.position;
        }
        else
        {
            spawnOriginPos = caster.position + Vector3.up * 1f; // 가슴 높이 기본값
        }

        startPosition = transform.position; // 갈고리의 현재 위치
        lastSegmentPos = spawnOriginPos; // 첫 마디 기준은 손 위치!
    }

    private void Update()
    {
        if (casterTransform == null) return;

        if (isReturning)
        {
            // 복귀 목표점 좌표 계산 (캐릭터 위치 기반)
            // 만약 지정된 spawnOriginPos의 Y 높이가 있다면, 그 높이를 유지하면서 복귀하도록 처리!
            Vector3 returnTarget = casterTransform.position;
            returnTarget.y = startPosition.y; // 발사될 때와 동일한 Y 높이로 고정!

            // 잡힌 적이 있다면 동일 높이 수준으로 당김
            if (hitEnemy != null)
            {
                Vector3 enemyTarget = returnTarget;
                hitEnemy.position = Vector3.MoveTowards(hitEnemy.position, enemyTarget, pullSpeed * Time.deltaTime);
            }

            // 갈고리 헤드 복귀 (Y 높이 변화 없이 수평으로 촤르륵 돌아옴)
            transform.position = Vector3.MoveTowards(transform.position, returnTarget, pullSpeed * Time.deltaTime);

            // 지나온 사슬 마디 정제
            CleanUpSegments();

            // 목표 지점에 도달하면 파괴
            if (Vector3.Distance(transform.position, returnTarget) < 0.5f)
            {
                DestroyAllSegments();
                Destroy(gameObject);
            }
        }
        else
        {
            // [발사 단계] 전방 이동
            transform.Translate(Vector3.forward * flySpeed * Time.deltaTime, Space.Self);

            // 사슬 마디 생성 체크
            SpawnChainSegment();

            // 최대 사거리 도달 시 회수
            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            {
                isReturning = true;
            }
        }
    }

    // 홀수/짝수 번째마다 회전을 바꿔주기 위한 플래그 변수 (클래스 상단 변수에 추가하거나 함수 내부 처리)
    private bool rotateSegment = false;

    // 3D 사슬 고리 생성 함수
    private void SpawnChainSegment()
    {
        if (chainSegmentPrefab == null) return;

        // 1. 갈고리가 손(spawnOriginPos)으로부터 얼마나 멀어졌는지 계산
        float distFromStart = Vector3.Distance(spawnOriginPos, transform.position);

        // 2. 만약 거리가 segmentSpacing보다 짧다면, 아직 첫 고리를 생성할 때가 아님
        if (distFromStart < segmentSpacing) return;

        // 3. 발사 방향(Vector) 계산
        Vector3 direction = (transform.position - spawnOriginPos).normalized;

        // 4. [보간 채우기] 마지막 생성 위치부터 현재 갈고리 직전까지를 채웁니다.
        // 마지막 생성 마디가 갈고리 뒤에 생성되도록 보정
        float lastSegmentDist = Vector3.Distance(spawnOriginPos, lastSegmentPos);

        // 마지막 생성 마디와 갈고리 사이 거리가 spacing보다 크면, 그 사이에 생성
        while (distFromStart - lastSegmentDist >= segmentSpacing)
        {
            // 손에서부터 정확한 Spacing 거리만큼 떨어진 좌표 계산!
            lastSegmentDist += segmentSpacing; // 거리를 갱신
            Vector3 spawnPos = spawnOriginPos + direction * lastSegmentDist;

            // 높이 오프셋 적용
            spawnPos += transform.TransformDirection(segmentOffset);

            // Z축 90도 교차 회전 적용
            Quaternion segmentRot = transform.rotation;
            if (rotateSegment)
            {
                segmentRot *= Quaternion.Euler(0f, 0f, 90f);
            }
            rotateSegment = !rotateSegment;

            // 생성
            GameObject segment = Instantiate(chainSegmentPrefab, spawnPos, segmentRot);
            spawnedSegments.Add(segment);

            // 마지막 생성 위치 업데이트 (방향성 유지를 위해 dist 업데이트)
            lastSegmentPos = spawnPos;
        }
    }

    // 복귀할 때 헤드보다 뒤에 있는(플레이어와 가까운) 마디부터 순차 삭제
    private void CleanUpSegments()
    {
        for (int i = spawnedSegments.Count - 1; i >= 0; i--)
        {
            if (spawnedSegments[i] != null)
            {
                // 헤드가 플레이어 쪽으로 들어오면서 마디 위치를 지나치면 삭제
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
            isReturning = true; // 적중 즉시 당기기 시작
            Debug.Log($"[이단 구속] {other.name} 낚아채기 성공!");
        }
    }
}