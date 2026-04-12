using UnityEngine;

public class SpikeProjectile : MonoBehaviour
{
    // [기획 의도] 논타겟팅 스킬(Q)의 명중 쾌감을 극대화하기 위한 발사체 로직.
    // 타겟팅 방식보다 높은 투사체 속도(20f)를 부여하여 박진감 있는 액션을 유도함.
    private float moveSpeed = 20f; // 논타겟이므로 속도감 있게
    private float damage;


    // [시스템 매커니즘] 사거리 기반 자동 소멸 시스템.
    // 기획서상의 '사거리(range)' 수치를 유지하기 위해 (사거리 / 속도) 공식을 사용하여 
    // 투사체가 정확한 거리만큼 이동 후 소멸하도록 계산함.
    public void InitNonTarget(float dmg, float range)
    {
        damage = dmg;
        // 사거리(range) / 속도(speed) = 생존 시간
        Destroy(gameObject, range / moveSpeed);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Enemy>()?.TakeDamage(damage);
            other.GetComponent<BleedStatus>()?.AddStack();
            Destroy(gameObject); 
        }
    }
}