using UnityEngine;
using System.Collections.Generic;

public class BloodStreamRenderer : MonoBehaviour
{
    public Material lineMaterial;
    private PlayerSkills playerSkills;

    // 현재 공중에 떠서 날아오고 있는 핏줄기들의 정보(F스킬)
    private List<BloodStreamInfo> activeStreams = new List<BloodStreamInfo>();

    // 핏줄기 하나의 정보를 담는 내부 클래스
    class BloodStreamInfo
    {
        public LineRenderer line;
        public Transform startTarget;
        public Transform endTarget;
        public Vector3 controlPoint; // 곡선의 휘어짐 정도
        public float currentT = 0f; // 이동 경과 시간 (0 ~ 1)
        public float healAmount;
    }

    void Start() => playerSkills = GetComponent<PlayerSkills>();

    void Update()
    {
        // F 스킬이 켜져 있을 때만 핏줄기들을 업데이트
        if (playerSkills.isF_Active) UpdateActiveStreams();
        else ClearAllStreams();
    }

    // BleedStatus에서 정해진 시간마다 호출하여 핏줄기 발사
    public void SpawnStream(Transform enemy, float amount, int stackIndex)
    {
        // 1. 새로운 LineRenderer 오브젝트 생성
        GameObject go = new GameObject("BloodStream");
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();

        // 2. 시각 설정 (물줄기처럼 보이게 굵기 조절)
        lr.startWidth = 0.1f;  // 머리 부분
        lr.endWidth = 0.02f;   // 꼬리 부분
        lr.material = lineMaterial;
        lr.positionCount = 10; // 곡선의 부드러움

        Vector3 startPos = enemy.position + Vector3.up * 1f;
        for (int i = 0; i < lr.positionCount; i++) lr.SetPosition(i, startPos);
        // 3. 곡선 궤적 생성 (스택별로 다른 방향으로 휘어지게)
        Vector3 randomOffset = Vector3.up * 2f + Random.insideUnitSphere * 1f;


        // 4. 핏줄기 정보 등록
        activeStreams.Add(new BloodStreamInfo
        {
            line = lr,
            startTarget = enemy,
            endTarget = transform,
            controlPoint = Vector3.Lerp(enemy.position, transform.position, 0.5f) + randomOffset,
            healAmount = amount
        });
    }

    void UpdateActiveStreams()
    {
        for (int i = activeStreams.Count - 1; i >= 0; i--)
        {
            BloodStreamInfo stream = activeStreams[i];

            // 이동 속도 조절 (0.5초 만에 도착)
            stream.currentT += Time.deltaTime * 2f;

            // 곡선의 시작점과 끝점을 시간에 따라 유동적으로 계산
            DrawFlowingCurve(stream);

            // 핏줄기가 내 몸에 완전히 닿으면 흡수
            if (stream.currentT >= 1f)
            {
                playerSkills.stats.Heal(Mathf.RoundToInt(stream.healAmount));
                Destroy(stream.line.gameObject);
                activeStreams.RemoveAt(i);
            }
        }
    }

    // [기획 의도] '흡혈'의 시각적 극대화. 
    // 단순한 UI 수치 변화가 아닌, 적에게서 플레이어에게 직접 에너지가 전달되는 과정을 
    // 2차 베지어 곡선(Bezier Curve) 기반의 핏줄기 시스템으로 시각화하여 플레이 만족도를 높임.
    // 정적인 곡선이 아니라, 시간에 따라 곡선 위를 이동하는 [핏줄기]를 그림
    void DrawFlowingCurve(BloodStreamInfo stream)
    {
        LineRenderer lr = stream.line;
        int points = lr.positionCount;

        Vector3 p0 = stream.startTarget.position + Vector3.up * 1f; // 적
        Vector3 p1 = stream.controlPoint; // 곡점
        Vector3 p2 = stream.endTarget.position + Vector3.up * 0.05f; // 나

        // [수학적 설계] 각 핏줄기에 미세한 시간차(t)와 랜덤 오프셋을 부여하여 
        // 여러 적에게서 동시에 흡혈할 때 자연스러운 유동체 느낌이 나도록 구현함.
        for (int i = 0; i < points; i++)
        {
            // 각 점마다 다른 시간차(`t`)를 주어 물줄기의 길이감을 만듦
            float headT = stream.currentT; // 머리 부분의 t
            float pointT = headT - (i / (float)(points - 1)) * 0.3f; // 꼬리로 갈수록 과거의 t를 사용 (길이 0.3)

            // t값의 범위를 0~1로 안전하게 제한
            pointT = Mathf.Clamp01(pointT);

            // 2차 베지어 곡선 공식으로 점 위치 계산
            Vector3 m1 = Vector3.Lerp(p0, p1, pointT);
            Vector3 m2 = Vector3.Lerp(p1, p2, pointT);
            Vector3 point = Vector3.Lerp(m1, m2, pointT);

            lr.SetPosition(i, point);
        }
    }

    void ClearAllStreams()
    {
        foreach (var stream in activeStreams) if (stream.line != null) Destroy(stream.line.gameObject);
        activeStreams.Clear();
    }
}