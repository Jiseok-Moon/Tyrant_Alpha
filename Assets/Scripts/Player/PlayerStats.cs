using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public float maxHp = 100f;
    public float currentHp;

    [Header("UI 연결")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public float lerpSpeed = 5f;
    private bool isDead = false;

    void Awake()
    {
        currentHp = maxHp;
        isDead = false; // 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
        }
        UpdateHpText();
    }

    void Update()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            // 1. 슬라이더를 목표 체력(currentHp)으로 부드럽게 이동시킴
            hpSlider.value = Mathf.Lerp(hpSlider.value, currentHp, Time.deltaTime * lerpSpeed);

            // 2. 슬라이더의 '실제 움직이는 값'을 텍스트에 표시
            if (hpText != null)
            {
                hpText.text = $"{(int)hpSlider.value} / {(int)maxHp}";
            }
        }
    }

    void UpdateHpText()
    {
        if (hpText != null)
        {
            hpText.text = $"{(int)currentHp} / {(int)maxHp}";
        }
    }

    /// <summary>
    /// PlayerSkills에서 호출하는 정확한 수치 기반 체력 소모 함수.
    /// </summary>
    public void ReduceHpRaw(float amount)
    {
        // 기존의 안전장치 로직(UseSkillHp)을 그대로 활용하여 체력을 깎음.
        UseSkillHp(amount);
    }

    /// <summary>
    /// 스킬 사용 시 체력을 소모. 5% 이하일 경우 소모 없이 발동.
    /// </summary>
    /// 
    // [기획 의도] '하이리스크 하이리턴' 자원 관리 시스템.
    // 방어력 수치를 없애는 대신, 자신의 체력을 자원으로 사용하는 시스템을 구축.
    // [혈액 한계(Safety Limit)] 체력이 5% 미만일 때 사망 리스크를 감수하고 
    // 최후의 일격을 가할 수 있는 '역전의 기회'를 시스템적으로 보장함.

    public bool UseSkillHp(float amount)
    {
        if (isDead) return false;
        float safetyLimit = maxHp * 0.05f; // 최대 체력의 5%

        // 1. 현재 체력이 5% 이하라면 소모 없이 즉시 통과
        if (currentHp <= safetyLimit)
        {
            return true;
        }

        // 2. 체력이 5%보다 많다면 소모 진행 (단, 소모 후에도 최소 0.1이라도 남게 하려면 0 대신 소량을 넣을 수 있음)
        currentHp = Mathf.Max(currentHp - amount, 0);
        return true;
    }

    /// <summary>
    /// R 스킬처럼 지속적으로 체력을 깎을 때 사용하는 함수.
    /// 마지노선(5%) 이하로는 절대 내려가지 않게 방어.
    /// </summary>
    public void ReduceHpContinuous(float amount)
    {
        if (isDead) return;

        float safetyLimit = maxHp * 0.05f;

        // 현재 체력이 이미 5% 이하라면 더 이상 깎지 않고 리턴
        if (currentHp <= safetyLimit) return;

        // 체력을 깎되, safetyLimit(5%) 아래로 떨어지지 않게 Clamp
        currentHp = Mathf.Clamp(currentHp - amount, safetyLimit, maxHp);
    }

    // 기존에 사용하던 ReduceHp도 안전하게 변경
    public void ReduceHp(float amount)
    {
        float safetyLimit = maxHp * 0.05f;
        currentHp = Mathf.Clamp(currentHp - amount, safetyLimit, maxHp);
    }


    public void ReduceHpPercent(float percent)
    {
        ReduceHp(maxHp * (percent / 100f));
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHp -= damage;
        if (currentHp < 0)
        {
            currentHp = 0;
            Die();
        }
    }

    public void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        GetComponent<PlayerController>().enabled = false;
        if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = true;

        // 즉시 게임 매니저 호출
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}