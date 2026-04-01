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

    void Awake()
    {
        currentHp = maxHp;
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

    // --- [기존 함수 유지] ---
    public void ReduceHp(float amount)
    {
        currentHp = Mathf.Max(currentHp - amount, 0);
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
        currentHp -= damage;
        if (currentHp < 0) currentHp = 0;
    }

    public void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
        }
    }
}