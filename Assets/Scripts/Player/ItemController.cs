using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[System.Serializable]
public class ItemSlot
{
    public ConsumableItem itemData;
    public Image iconDisplay;
    public int currentCount = 3;
}

public class ItemController : MonoBehaviour
{
    public ItemSlot slot1, slot2, slot3;

    [Header("버프 슬롯 부모 오브젝트 (Slot 연결)")]
    public GameObject healBuffSlot;   // HealBuff_Slot 연결
    public GameObject speedBuffSlot;  // SpeedBuff_Slot 연결
    public GameObject damageBuffSlot; // DamageBuff_Slot 연결

    private NavMeshAgent agent;
    private bool isUsing = false;
    private float baseSpeed;
    public float damageMultiplier = 1.0f;

    private PlayerStats playerStats; // PlayerStats를 담을 변수

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
        playerStats = GetComponent<PlayerStats>();

        // 아이템 슬롯 초기화
        InitSlot(slot1); InitSlot(slot2); InitSlot(slot3);

        // 시작할 때 버프 창은 모두 꺼둡니다.
        if (healBuffSlot) healBuffSlot.SetActive(false);
        if (speedBuffSlot) speedBuffSlot.SetActive(false);
        if (damageBuffSlot) damageBuffSlot.SetActive(false);
    }

    void InitSlot(ItemSlot slot)
    {
        if (slot.itemData != null && slot.iconDisplay != null)
            slot.iconDisplay.sprite = slot.itemData.icon;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryUse(slot1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryUse(slot2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryUse(slot3);
    }

    void TryUse(ItemSlot slot)
    {
        if (!isUsing && slot.itemData != null && slot.currentCount > 0)
        {
            slot.currentCount--;
            StartCoroutine(UseItemRoutine(slot));

            // 아이템 소모 시 하단 슬롯 비우기
            if (slot.currentCount <= 0)
                slot.iconDisplay.gameObject.SetActive(false);
        }
    }

    IEnumerator UseItemRoutine(ItemSlot slot)
    {
        isUsing = true;
        ConsumableItem item = slot.itemData;

        float speedBeforeCast = agent.speed;

        if (agent.hasPath) agent.isStopped = false;

        agent.speed = speedBeforeCast * item.speedMultiplier;

        yield return new WaitForSeconds(item.castTime);

        ApplyItemEffect(item);

        if (item.moveSpeedBoost <= 0) agent.speed = speedBeforeCast;
        isUsing = false;
    }

    void ApplyItemEffect(ConsumableItem item)
    {
        // 1. 봉합 도구 (체력)
        if (item.healAmount > 0 && healBuffSlot != null)
        {
            // UI 연출과 실제 회복 로직을 담은 새 코루틴을 실행합니다.
            StartCoroutine(HealWithDelayRoutine(item.healAmount, 2.0f));
        }

        // 2. 벨라돈나 (속도)
        if (item.moveSpeedBoost > 0 && speedBuffSlot != null)
        {
            StartCoroutine(SpeedBoostRoutine(item.moveSpeedBoost, item.duration));
            StartCoroutine(BuffUIRoutine(speedBuffSlot, item.duration));
        }

        // 3. 투구꽃 (공격력)
        if (item.damageBoost > 0 && damageBuffSlot != null)
        {
            StartCoroutine(DamageBoostRoutine(item.duration));
            StartCoroutine(BuffUIRoutine(damageBuffSlot, item.duration));
        }
    }
    IEnumerator HealWithDelayRoutine(float amount, float delay)
    {
        // 1. UI 연출(시계방향 깎기)이 끝날 때까지 2초간 기다림
        yield return StartCoroutine(BuffUIRoutine(healBuffSlot, delay));

        // 2. playerStats 변수를 사용하여 Heal 함수 호출
        if (playerStats != null)
        {
            playerStats.Heal(amount);
        }
    }

    // [UI/UX] 아이템 사용 시 시계 방향으로 줄어드는 Fill Amount 연출을 통해 
    // 직관적인 버프 지속 시간 피드백을 제공함.
    // 테두리는 나타나서 고정되고, 아이콘만 시계 방향으로 소멸
    IEnumerator BuffUIRoutine(GameObject slotObj, float duration)
    {
        // 슬롯(테두리 포함) 켜기.
        slotObj.SetActive(true);

        // 자식 중에서 'BuffIcon'이라는 이름의 이미지를 찾기.
        Transform iconTransform = slotObj.transform.Find("BuffIcon");
        if (iconTransform == null)
        {
            yield break;
        }

        Image iconImage = iconTransform.GetComponent<Image>();
        iconImage.fillAmount = 1f;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 1에서 0으로 줄어듬.
            iconImage.fillAmount = 1f - (elapsed / duration);
            yield return null;
        }

        // 지속시간 종료 후 슬롯 전체(테두리 포함)를 다시 끔.
        slotObj.SetActive(false);
    }

    IEnumerator SpeedBoostRoutine(float boost, float duration)
    {
        agent.speed = baseSpeed + boost;
        yield return new WaitForSeconds(duration);
        agent.speed = baseSpeed;
    }

    IEnumerator DamageBoostRoutine(float duration)
    {
        damageMultiplier = 1.1f;
        yield return new WaitForSeconds(duration);
        damageMultiplier = 1.0f;
    }
}