using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 제어를 위해 필요

public class Enemy : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private float originalAnimSpeed;

    public Transform target;           // 플레이어 타겟
    public int contactDamage = 5;      // 충돌 데미지
    public float damageCooldown = 1.0f; // 데미지 주기
    private float lastDamageTime;
    private bool isStasis = false;     // 혈류 정체 상태 체크


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (anim != null) originalAnimSpeed = anim.speed;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        // 1. 혈류 정체 상태가 아니고, 타겟이 있다면 추격
        if (!isStasis && target != null && agent != null && agent.enabled)
        {
            agent.SetDestination(target.position);
        }
    }

    // --- 혈류 정체(Stasis) 실행 함수 ---
    public void ApplyStasis(float duration)
    {
        StartCoroutine(StasisRoutine(duration));
    }

    private System.Collections.IEnumerator StasisRoutine(float duration)
    {

        isStasis = true;

        // 1. 모든 행동 정지
        if (agent != null)
        {
            agent.isStopped = true; // 이동 멈춤
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.speed = 0; // 애니메이션 정지 (기획하신 '그 상태로 고정' 연출)
        }

        // 2. 혈류 정체 이펙트 생성 (선택 사항)
        // Instantiate(stasisVisualEffect, transform.position, Quaternion.identity, transform);

        // 3. 지속 시간만큼 대기 (1초 또는 궁극기 시 2초)
        yield return new WaitForSeconds(duration);

        // 4. 상태 복구
        if (anim != null) anim.speed = originalAnimSpeed;
        if (agent != null) agent.isStopped = false;

        isStasis = false;
    }
    private void OnTriggerStay(Collider other)
    {
        // 정체 상태가 아닐 때만 공격함
        if (!isStasis && other.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                // 기획자님의 PlayerStats 스크립트 구조에 맞춰 호출 (예시: PlayerStats)
                var playerStats = other.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(contactDamage);
                    lastDamageTime = Time.time;
                    Debug.Log($"플레이어에게 {contactDamage} 피해를 입힘!");
                }
            }
        }
    }

    // 기존의 데미지 처리 함수
    public void TakeDamage(float amount)
    {
        Debug.Log($"{gameObject.name}가 {amount}의 피해를 입었습니다.");
        // 여기서 HP가 0 이하가 되면 Destroy(gameObject) 등의 처리가 필요합니다.
    }
}