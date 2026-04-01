using UnityEngine;
using System.Collections;

public class BleedStatus : MonoBehaviour
{
    public int currentStacks = 0;
    private const int maxStacks = 3;
    private float duration = 5f;
    public float timer; // [확인용 public]
    private bool isTickRunning = false; // 코루틴 중복 실행 방지 플래그

    public void AddStack()
    {
        if (currentStacks < maxStacks) currentStacks++;
        timer = duration;

        // [중요] 코루틴이 멈춰있다면 다시 돌립니다.
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
            // [기획 반영] F가 켜져 있으면 타이머를 고정하지만, 틱은 돌아야 함
            
            int intDamage = Mathf.RoundToInt(1f + currentStacks);
            GetComponent<Enemy>()?.TakeDamage(intDamage);
            yield return new WaitForSeconds(2.0f);

            // --- F 스킬 연동 부분 ---
            if (PlayerSkills.Instance != null && PlayerSkills.Instance.isF_Active)
            {
                float dist = Vector3.Distance(transform.position, PlayerSkills.Instance.transform.position);
                if (dist <= PlayerSkills.Instance.fRange)
                {
                    var streamRenderer = PlayerSkills.Instance.GetComponent<BloodStreamRenderer>();
                    if (streamRenderer != null)
                    {
                        // 플레이어에게 피를 쏩니다!
                        streamRenderer.SpawnStream(this.transform, intDamage, currentStacks);
                    }
                }
            }
        }

        // 타이머가 다 되면 상태 초기화 후 코루틴 종료
        currentStacks = 0;
        isTickRunning = false;
    }

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