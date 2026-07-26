using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("HP 스탯 및 UI")]
    public float maxHp = 100f;
    public float currentHp;
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("이단심판관 전용: 신앙심(보호막) 스탯 및 UI")]
    public float maxFaith = 50f;
    public float currentFaith = 0f;
    public Slider faithSlider;
    public TextMeshProUGUI faithText;

    [Header("공통 UI 설정")]
    public float lerpSpeed = 5f;
    private bool isDead = false;

    void Awake()
    {
        currentHp = maxHp;
        currentFaith = 0f; // 신앙심 초기화
        isDead = false;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
        }

        // 신앙심 슬라이더 초기화 (등록되어 있을 경우만)
        if (faithSlider != null)
        {
            faithSlider.maxValue = maxFaith;
            faithSlider.value = currentFaith;
        }

        UpdateHpText();
        UpdateFaithText();
    }

    void Update()
    {
        // 1. HP UI 부드러운 갱신
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = Mathf.Lerp(hpSlider.value, currentHp, Time.deltaTime * lerpSpeed);

            if (hpText != null)
            {
                hpText.text = $"{(int)hpSlider.value} / {(int)maxHp}";
            }
        }

        // 2. 신앙심(Faith) UI 부드러운 갱신 (이단심판관 전용)
        if (faithSlider != null)
        {
            faithSlider.maxValue = maxFaith;
            faithSlider.value = Mathf.Lerp(faithSlider.value, currentFaith, Time.deltaTime * lerpSpeed);

            if (faithText != null)
            {
                faithText.text = $"{(int)faithSlider.value} / {(int)maxFaith}";
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

    void UpdateFaithText()
    {
        if (faithText != null)
        {
            faithText.text = $"{(int)currentFaith} / {(int)maxFaith}";
        }
    }

    #region 피격 및 데미지 로직 (신앙심 보호막 연동)
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // 1. 신앙심(보호막)이 남아있다면 신앙심부터 차감
        if (currentFaith > 0)
        {
            if (currentFaith >= damage)
            {
                currentFaith -= damage;
                damage = 0;
            }
            else
            {
                damage -= currentFaith;
                currentFaith = 0;
            }
        }

        // 2. 보호막을 뚫고 남은 데미지가 있다면 HP 차감
        if (damage > 0)
        {
            currentHp -= damage;
            if (currentHp <= 0)
            {
                currentHp = 0;
                Die();
            }
        }
    }

    // int 매개변수 호환성 유지
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }
    #endregion

    #region 이단심판관: 신앙심(Faith) 제어 함수
    public void AddFaith(float amount)
    {
        currentFaith = Mathf.Clamp(currentFaith + amount, 0f, maxFaith);
    }

    public bool UseFaith(float amount)
    {
        if (currentFaith >= amount)
        {
            currentFaith -= amount;
            return true;
        }
        return false;
    }

    // F스킬처럼 보유한 모든 신앙심을 소모하고 그 수치를 반환할 때 사용
    public float ConsumeAllFaith()
    {
        float consumed = currentFaith;
        currentFaith = 0f;
        return consumed;
    }
    #endregion

    #region 혈마법사 & 공통 HP 함수
    public void ReduceHpRaw(float amount)
    {
        UseSkillHp(amount);
    }

    public bool UseSkillHp(float amount)
    {
        if (isDead) return false;
        float safetyLimit = maxHp * 0.05f;

        if (currentHp <= safetyLimit)
        {
            return true;
        }

        currentHp = Mathf.Max(currentHp - amount, 0);
        return true;
    }

    public void ReduceHpContinuous(float amount)
    {
        if (isDead) return;

        float safetyLimit = maxHp * 0.05f;
        if (currentHp <= safetyLimit) return;

        currentHp = Mathf.Clamp(currentHp - amount, safetyLimit, maxHp);
    }

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

    public void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
        }
    }
    #endregion

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (GetComponent<PlayerController>() != null)
            GetComponent<PlayerController>().enabled = false;

        if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}