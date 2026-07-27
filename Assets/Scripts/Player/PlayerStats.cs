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

    [Header("이단심판관 전용: 방어막 UI 색상 설정")]
    public Image faithSliderFill;                                // FaithSlider의 'Fill' Image 컴포넌트
    private Color originalFaithColor;                            // Awake에서 기존 색상(연한 회색) 자동 기록
    public Color shieldColor = new Color(0.5f, 0.8f, 1f, 1f);    // 방어막 전환 시 연한 하늘색
    private bool isShieldActive = false;                         // 방어막 활성화 여부

    [Header("공통 UI 설정")]
    public float lerpSpeed = 5f;
    private bool isDead = false;

    void Awake()
    {
        currentHp = maxHp;
        currentFaith = 0f; // 신앙심 초기화
        isDead = false;

        // 기존 Fill 이미지의 기본 색상(연한 회색) 자동 저장
        if (faithSliderFill != null)
        {
            originalFaithColor = faithSliderFill.color;
        }

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

        // [수정] 단순히 currentFaith > 0 인 것만 체크하는 것이 아니라,
        // F 스킬 사용 후 '방어막 상태(isShieldActive == true)'일 때만 신앙심을 차감합니다!
        if (isShieldActive && currentFaith > 0)
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

            // 보호막을 모두 소모했으면 방어막 상태 해제 및 원래 신앙심 색상(연한 회색)으로 복구
            if (currentFaith <= 0)
            {
                SetShieldState(false);
            }
        }

        // 2. 방어막 상태가 아니거나, 보호막을 뚫고 남은 데미지가 있다면 HP 차감
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

    #region 이단심판관: 신앙심(Faith) 및 방어막 UI 제어 함수
    // 방어막 상태 변경 시 UI 색상 전환 (연회색 <-> 연하늘색)
    public void SetShieldState(bool active)
    {
        isShieldActive = active;

        if (faithSliderFill != null)
        {
            // 방어막 활성화 시: 연한 하늘색 (R: 128, G: 200, B: 255, A: 255)
            // 비활성화 시: 기존 연한 회색 (R: 200, G: 200, B: 200, A: 255)
            Color targetColor = isShieldActive ? new Color(0.5f, 0.8f, 1f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);

            faithSliderFill.color = targetColor;

            // Canvas/UI 강제 갱신
            faithSliderFill.SetVerticesDirty();
        }
        else
        {
            Debug.LogWarning("[PlayerStats] faithSliderFill 이 연결되어 있지 않습니다!");
        }
    }

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