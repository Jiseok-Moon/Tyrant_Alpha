using UnityEngine;

// [기획 의도] 고정 쿼터뷰(Quarter View) 시점 유지 및 부드러운 카메라 워크 구현.
// 액션이 격렬한 게임 특성상 카메라가 캐릭터를 너무 딱딱하게 따라가면 멀미를 유발할 수 있으므로,
// Lerp(선형 보간)를 적용하여 부드럽게 추적하도록 설계함.

public class CameraController : MonoBehaviour
{
    // [편의성 설계] 오프셋(Offset) 자동 계산.
    // 기획자가 인스펙터 창에서 세팅한 초기 구도를 자동으로 유지하게 하여 
    // 매번 수치를 입력하지 않아도 개발 효율성을 확보함.

    public Transform target;      // 따라갈 대상 (Player 캐릭터)
    public Vector3 offset;        // 캐릭터와 카메라 사이의 거리 (현재 쿼터뷰 각도 유지용)
    public float smoothSpeed = 5f; // 카메라가 따라오는 부드러움 정도 (값이 클수록 빠름)

    void Start()
    {
        // 게임 시작 시, 현재 카메라와 캐릭터의 거리 차이를 자동으로 계산해서 저장.
        // 인스펙터에서 맞춘 쿼터뷰 각도가 그대로 유지.
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    // 캐릭터 이동이 끝난 후 카메라가 움직임.
    void LateUpdate()
    {
        if (target == null) return;

        // 1. 목표 위치 계산 (현재 캐릭터 위치 + 처음에 설정한 간격)
        Vector3 desiredPosition = target.position + offset;

        // 2. 부드러운 보간 (Lerp) 처리
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. 카메라 위치 업데이트
        transform.position = smoothedPosition;
    }
}