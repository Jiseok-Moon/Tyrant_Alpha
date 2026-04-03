using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{

    private static List<Enemy> allEnemies = new List<Enemy>();

    [Header("무리 설정")]
    public string enemyID = "Wolf"; // 인스펙터에서 늑대는 "Wolf", 트롤은 "Troll"로 설정

    protected NavMeshAgent agent;
    protected Animator anim;
    private float originalAnimSpeed;
    protected string attackAnimName = "attack01";

    [Header("이펙트")]
    public GameObject bleedParticlePrefab;

    [Header("Stats")]
    public float hp = 1500f;
    public int contactDamage = 5;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    [Header("Animation Settings")]
    public float hitAnimCooldown = 0.5f;
    public float minDamageForAnim = 10f;
    protected float lastHitAnimTime;

    [Header("AI 설정 (인식/정찰)")]
    public float detectionRange = 10f;      // 평상시 인식 범위
    public float enragedDetectionRange = 25f; // 피격 시 확장될 범위
    public float attackRange = 2.5f;
    public float patrolRange = 8f;          // 정찰 범위
    private Vector3 startPosition;          // 정찰 기준점
    private bool isPlayerDetected = false;  // 인식 여부 플래그

    public Transform target;
    private bool isStasis = false;
    protected bool isDead = false;
    private bool isAttacking = false;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        startPosition = transform.position; // 시작 위치 저장


        if (!allEnemies.Contains(this)) allEnemies.Add(this);

        if (anim != null) originalAnimSpeed = anim.speed;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }
    protected void OnDestroy()
    {
        if (allEnemies.Contains(this)) allEnemies.Remove(this);
    }

    void Update()
    {
        if (isDead || isStasis) return;

        float distance = Vector3.Distance(transform.position, target.position);

        // 1. 인식 로직: 거리 안에 들어오면 인식 시작
        if (distance <= detectionRange)
        {
            // 내가 발견하면 무리 전체에게 알림
            AlertPack();
        }
        // 2. 행동 분기
        if (isPlayerDetected)
        {
            if (distance <= attackRange)
            {
                StopAndAttack();
            }
            else if (!isAttacking)
            {
                ChaseTarget();
            }
        }
        else
        {
            Patrol(); // 인식 전에는 정찰
        }

        HandleMovementAnimation();
    }

    // 무리 전체를 인식 상태로 만드는 함수
    public void AlertPack()
    {
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && !enemy.isDead && enemy.enemyID == this.enemyID)
            {
                enemy.isPlayerDetected = true;
                enemy.detectionRange = enemy.enragedDetectionRange;
            }
        }
    }

    private void Patrol()
    {
        // 목적지에 거의 도착했거나 경로 계산이 끝났을 때 새로운 지점 설정
        if (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRange;
            Vector3 nextDest = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            agent.SetDestination(nextDest);
        }
    }

    private void ChaseTarget()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }

    private void StopAndAttack()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (Time.time >= lastDamageTime + damageCooldown && !isAttacking)
        {
            StartCoroutine(AttackAnimationRoutine());
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        hp -= amount;

        // 한 마리라도 맞으면 무리 전체가 공격
        AlertPack();

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

    // --- 아래의 Stasis, HitAnimation, Die, MovementAnimation은 기존과 동일 ---
    // (코드 중복 방지를 위해 내용은 동일하게 유지하시면 됩니다)

    private void HandleMovementAnimation()
    {
        if (anim == null) return;
        float velocity = agent.velocity.magnitude;
        if (isAttacking || velocity < 0.1f)
        {
            anim.SetBool("run", false); anim.SetBool("walk", false); anim.SetBool("idle01", true);
        }
        else if (velocity > 3.5f)
        {
            anim.SetBool("run", true); anim.SetBool("walk", false); anim.SetBool("idle01", false);
        }
        else
        {
            anim.SetBool("run", false); anim.SetBool("walk", true); anim.SetBool("idle01", false);
        }
    }

    private IEnumerator AttackAnimationRoutine()
    {
        isAttacking = true;
        lastDamageTime = Time.time;
        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        if (anim != null) anim.SetBool(attackAnimName, true);
        target.GetComponent<PlayerStats>()?.TakeDamage(contactDamage);
        yield return new WaitForSeconds(0.8f);
        if (anim != null) anim.SetBool(attackAnimName, false);
        isAttacking = false;
    }

    public void ApplyStasis(float duration) { StartCoroutine(StasisRoutine(duration)); }
    private IEnumerator StasisRoutine(float duration)
    {
        isStasis = true;
        if (agent != null && agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        if (anim != null) anim.speed = 0;
        yield return new WaitForSeconds(duration);
        if (!isDead)
        {
            if (anim != null) anim.speed = originalAnimSpeed;
            if (agent != null && agent.enabled) agent.isStopped = false;
            isStasis = false;
        }
    }

    protected IEnumerator HitAnimationRoutine()
    {
        anim.SetBool("damage", true);
        yield return new WaitForSeconds(0.3f);
        anim.SetBool("damage", false);
    }

    protected void Die()
    {
        isDead = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetBool("run", false);
            anim.SetBool("walk", false);
            anim.SetBool("idle01", false);
            anim.SetBool("dead", true);
        }

        if (allEnemies.Contains(this))
        {
            allEnemies.Remove(this);
        }

        Destroy(gameObject, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, patrolRange);
    }
}