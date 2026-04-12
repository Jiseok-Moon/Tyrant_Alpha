using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;


public class Enemy : MonoBehaviour
{

    private static List<Enemy> allEnemies = new List<Enemy>();

    [Header("무리 설정")]
    public string enemyID = "Wolf"; // 늑대는 "Wolf", 트롤은 "Troll"로 설정

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


    // [데이터 관리] Inspector에서 기획자가 애니메이션 딜레이와 실제 데미지 판정 시점(Hit Delay)을 
    // 프레임 단위로 미세하게 조정할 수 있도록 변수 노출.

    [Header("공격 타이밍 설정")]
    [Tooltip("애니메이션 시작 후 실제 데미지가 들어가는 시점(초)")]
    public float hitDelay = 0.3f;

    [Tooltip("데미지 판정 후 애니메이션이 마무리될 때까지 기다리는 시간(초)")]
    public float postAttackDelay = 0.7f;

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


    // [기획 의도] 무리 지어 행동하는 적(Pack AI)의 특성을 구현. 
    // 한 개체만 피격되어도 근처의 동일 ID 적들이 인지 범위(Detection Range)를 확장하며 협동 공격을 하도록 설계함.
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

    // [확장성] virtual 메서드(TakeDamage, HitAnimationRoutine)를 통해 
    // 다양한 적(Heavy, Fast, Boss 등)을 상속만으로 빠르게 생성할 수 있는 구조 구축.
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

    protected virtual IEnumerator AttackAnimationRoutine()
    {
        isAttacking = true;
        lastDamageTime = Time.time;

        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        if (anim != null) anim.SetBool(attackAnimName, true);

        yield return new WaitForSeconds(hitDelay);

        float currentDist = Vector3.Distance(transform.position, target.position);
        if (currentDist <= attackRange + 0.5f)
        {
            target.GetComponent<PlayerStats>()?.TakeDamage(contactDamage);
        }
        yield return new WaitForSeconds(postAttackDelay);
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

    protected virtual IEnumerator HitAnimationRoutine()
    {
        // 1. 공격받는 순간 멈춤
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;   // AI 경로 추적 중지
            agent.velocity = Vector3.zero; // 물리적인 미끄러짐 방지
        }

        // 2. 피격 애니메이션 재생
        anim.SetBool("damage", true);

        // 경직 시간
        yield return new WaitForSeconds(0.4f);

        anim.SetBool("damage", false);

        // 3. 경직이 끝난 후 다시 움직이게 함 (죽지 않았을 때만)
        if (!isDead && agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }
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