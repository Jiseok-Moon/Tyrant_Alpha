using UnityEngine;

public class PetalMove : MonoBehaviour
{
    // PlayerSkills에서 이 speed 값을 덮어씌움.
    public float speed = 10f;
    // [기획 의도] W 스킬(꽃봉오리 폭발)의 파편 연출.
    // 폭발 시 생성되는 다수의 꽃잎이 사방으로 퍼져나가며 시각적 화려함을 더함.

    // [매커니즘] 초기화(Init) 함수를 통한 동적 수치 적용.
    // 생성 시점(PlayerSkills)에서 결정된 반지름(radius) 데이터를 받아 
    // 각 파편의 생존 시간을 결정함으로써 연출의 통일성을 확보함.
    public void Init(float radius)
    {
        transform.localScale = Vector3.one * 0.5f; // 필요 시 크기 조절

        // 거리 / 속도 = 생존 시간
        float lifeTime = radius / (speed * 0.5f);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 정해진 속도로 전진
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}