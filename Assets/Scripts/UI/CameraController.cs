using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;      // 따라갈 대상 (Player 캐릭터)
    public Vector3 offset;        // 캐릭터와 카메라 사이의 거리 (현재 쿼터뷰 각도 유지용)
    public float smoothSpeed = 5f; // 카메라가 따라오는 부드러움 정도 (값이 클수록 빠름)

    void Start()
    {
        // 게임 시작 시, 현재 카메라와 캐릭터의 거리 차이를 자동으로 계산해서 저장합니다.
        // 이렇게 하면 인스펙터에서 맞춘 쿼터뷰 각도가 그대로 유지됩니다.
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    // 캐릭터 이동이 끝난 후 카메라가 움직여야 떨림(Jitter)이 없습니다.
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