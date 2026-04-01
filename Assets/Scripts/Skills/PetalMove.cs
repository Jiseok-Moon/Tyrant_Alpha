using UnityEngine;

public class PetalMove : MonoBehaviour
{
    // PlayerSkills에서 이 speed 값을 덮어씌웁니다.
    public float speed = 10f;

    public void Init(float radius)
    {
        transform.localScale = Vector3.one * 0.5f; // 필요시 크기 조절

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