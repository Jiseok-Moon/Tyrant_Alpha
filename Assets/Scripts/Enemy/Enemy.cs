using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private float originalAnimSpeed;

    [Header("Stats")]
    public float hp = 500f;
    public int contactDamage = 5;
    public float damageCooldown = 1.0f;
    private float lastDamageTime;

    [Header("Animation Settings")]
    public float hitAnimCooldown = 0.5f;
    public float minDamageForAnim = 9.0f;
    private float lastHitAnimTime;

    public Transform target;
    private bool isStasis = false;
    private bool isDead = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (anim != null) originalAnimSpeed = anim.speed;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        HandleMovementAnimation();

        if (!isStasis && target != null && agent != null && agent.enabled)
        {
            agent.SetDestination(target.position);
        }
    }

    private void HandleMovementAnimation()
    {
        if (isStasis || anim == null) return;

        float velocity = agent.velocity.magnitude;

        // Bool 방식: SetBool을 사용하여 하나를 켜면 나머지는 명시적으로 꺼줘야 합니다.
        if (velocity > 3.5f) // run
        {
            anim.SetBool("run", true);
            anim.SetBool("walk", false);
            anim.SetBool("idle01", false);
        }
        else if (velocity > 0.1f) // walk
        {
            anim.SetBool("run", false);
            anim.SetBool("walk", true);
            anim.SetBool("idle01", false);
        }
        else // idle
        {
            anim.SetBool("run", false);
            anim.SetBool("walk", false);
            anim.SetBool("idle01", true);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        hp -= amount;

        // 피격 애니메이션 (Bool 방식)
        if (anim != null && Time.time >= lastHitAnimTime + hitAnimCooldown)
        {
            if (amount >= minDamageForAnim)
            {
                StartCoroutine(HitAnimationRoutine());
                lastHitAnimTime = Time.time;
            }
        }

        if (hp <= 0) Die();
    }

    private IEnumerator HitAnimationRoutine()
    {
        anim.SetBool("damage", true);
        yield return new WaitForSeconds(0.3f); // 아파하는 동작 유지 시간
        anim.SetBool("damage", false); // 반드시 다시 꺼줘야 합니다!
    }

    public void ApplyStasis(float duration)
    {
        if (isDead) return;
        StartCoroutine(StasisRoutine(duration));
    }

    private IEnumerator StasisRoutine(float duration)
    {
        isStasis = true;
        if (agent != null) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        if (anim != null) anim.speed = 0;

        yield return new WaitForSeconds(duration);

        if (!isDead)
        {
            if (anim != null) anim.speed = originalAnimSpeed;
            if (agent != null) agent.isStopped = false;
            isStasis = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isDead || isStasis) return;
        if (other.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
        {
            var playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(contactDamage);
                lastDamageTime = Time.time;
                StartCoroutine(AttackAnimationRoutine());
            }
        }
    }

    private IEnumerator AttackAnimationRoutine()
    {
        anim.SetBool("attack01", true);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool("attack01", false);
    }

    void Die()
    {
        isDead = true;
        if (agent != null) agent.isStopped = true;

        // 사망 시 모든 이동 Bool을 끄고 dead만 켭니다.
        anim.SetBool("run", false);
        anim.SetBool("walk", false);
        anim.SetBool("idle01", false);
        anim.SetBool("dead", true);

        Destroy(gameObject, 3f);
    }
}