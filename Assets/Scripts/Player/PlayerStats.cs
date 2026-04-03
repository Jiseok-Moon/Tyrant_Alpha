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

            // 2. [수정] 슬라이더의 '실제 움직이는 값'을 텍스트에 표시
            // 이렇게 하면 체력바가 줄어드는 속도에 맞춰 숫자도 같이 줄어듭니다!
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
            // 소수점 없이 깔끔하게 표시하려면 (int) 형변환
            // 기획자님이 원하신 "현재 / 최대" 형식
            hpText.text = $"{(int)currentHp} / {(int)maxHp}";
        }
    }
    // --- [기획자님 요청 로직: 수치 기반 체력 소모] ---

    /// <summary>
    /// PlayerSkills에서 호출하는 정확한 수치 기반 체력 소모 함수입니다.
    /// </summary>
    public void ReduceHpRaw(float amount)
    {
        // 기존의 안전장치 로직(UseSkillHp)을 그대로 활용하여 체력을 깎습니다.
        UseSkillHp(amount);
    }

    /// <summary>
    /// 스킬 사용 시 체력을 소모합니다. 5% 이하일 경우 소모 없이 발동합니다.
    /// </summary>
    public bool UseSkillHp(float amount)
    {
        if (isDead) return false;
        float safetyLimit = maxHp * 0.05f; // 최대 체력의 5%

        // 1. 현재 체력이 5% 이하라면 소모 없이 즉시 통과
        if (currentHp <= safetyLimit)
        {
            Debug.Log("<color=cyan>[혈액 한계] 체력 소모 없이 스킬 발동!</color>");
            return true;
        }

        // 2. 체력이 5%보다 많다면 소모 진행 (단, 소모 후에도 최소 0.1이라도 남게 하려면 0 대신 소량을 넣을 수 있습니다)
        currentHp = Mathf.Max(currentHp - amount, 0);
        Debug.Log($"스킬 사용 - HP {amount} 소모 | 남은 HP: {currentHp}");
        return true;
    }

    /// <summary>
    /// R 스킬처럼 지속적으로 체력을 깎을 때 사용하는 함수입니다.
    /// 마지노선(5%) 이하로는 절대 내려가지 않게 방어합니다.
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

    // 기존에 사용하던 ReduceHp도 안전하게 변경 (선택 사항)
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
        Debug.Log("<color=red>캐릭터가 사망하였습니다.</color>");
        Time.timeScale = 0f;

    }
}