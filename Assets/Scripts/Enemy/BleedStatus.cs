using UnityEngine;
using System.Collections;

public class BleedStatus : MonoBehaviour
{
    public int currentStacks = 0;
    private const int maxStacks = 3;
    private float duration = 5f;
    private float timer;
    private bool isTickRunning = false; // 코루틴 중복 실행 방지 플래그

    public GameObject bleedEffectPrefab;


    // [기획 의도] '출혈' 상태 이상을 통해 지속적인 피해 누적뿐만 아니라 플레이어의 '흡혈(Blood Stream)' 매커니즘과 연동되도록 설계.
    // 스택(Stack) 시스템을 도입하여 공격의 유효성을 누적시키고, 시각적 피드백(Scale 조절)을 통해 플레이어에게 출혈의 가시성과 플레이어의 체력 회복에 보상감을 제공함.
    public void AddStack()
    {
        if (currentStacks < maxStacks) currentStacks++;
        timer = duration;

        SpawnBleedEffect(0.8f);

        // 코루틴이 멈춰있다면 다시 실행.
        if (!isTickRunning)
        {
            StartCoroutine(TickDamage());
        }
    }
   
    IEnumerator TickDamage()
    {
        isTickRunning = true; // 실행 중임을 알림

        while (timer > 0)
        {
            // F가 켜져 있으면 타이머를 고정하지만, 틱은 돌아감
            
            int intDamage = Mathf.RoundToInt(1f + currentStacks);
            GetComponent<Enemy>()?.TakeDamage(intDamage);

            float effectScale = 1f + (currentStacks * 0.3f);
            SpawnBleedEffect(effectScale);
            yield return new WaitForSeconds(1.5f);

            // --- F 스킬 연동 부분 ---
            if (PlayerSkills.Instance != null && PlayerSkills.Instance.isF_Active)
            {
                float dist = Vector3.Distance(transform.position, PlayerSkills.Instance.transform.position);
                if (dist <= PlayerSkills.Instance.fRange)
                {
                    var streamRenderer = PlayerSkills.Instance.GetComponent<BloodStreamRenderer>();
                    if (streamRenderer != null)
                    {
                        // 플레이어에게 피를 발사
                        streamRenderer.SpawnStream(this.transform, intDamage, currentStacks);
                    }
                }
            }
        }

        // 타이머가 다 되면 상태 초기화 후 코루틴 종료
        currentStacks = 0;
        isTickRunning = false;
    }

    void SpawnBleedEffect(float scale)
    {
        if (bleedEffectPrefab == null) return;

        // 늑대의 위치보다 약간 위(허리/등 부분)에서 생성
        GameObject effect = Instantiate(bleedEffectPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity, this.transform);

        // 파티클 시스템의 Scaling Mode가 Hierarchy여야 작동함
        effect.transform.localScale = Vector3.one * scale;

        Destroy(effect, 1f);
    }

    // [시스템 매커니즘] F 스킬 활성화 시 출혈 지속시간(Timer)을 고정(Freeze)하여 전략적 스킬 연동 유도.
    void Update()
    {
        // F 스킬 사거리 내에 있으면 타이머 고정
        if (PlayerSkills.Instance != null && PlayerSkills.Instance.isF_Active)
        {
            float dist = Vector3.Distance(transform.position, PlayerSkills.Instance.transform.position);
            if (dist <= PlayerSkills.Instance.fRange && currentStacks > 0)
            {
                timer = duration;
            }
        }

        if (timer > 0) timer -= Time.deltaTime;
    }
}