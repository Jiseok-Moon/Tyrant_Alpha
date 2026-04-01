using UnityEngine;

public class SpikeProjectile : MonoBehaviour
{
    private float moveSpeed = 20f; // 논타겟이므로 속도감 있게!
    private float damage;

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