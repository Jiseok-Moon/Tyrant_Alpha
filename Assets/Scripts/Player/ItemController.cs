using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ItemController : MonoBehaviour
{
    public ConsumableItem slot1Item;
    public Image slot1IconDisplay;
    public ConsumableItem slot2Item;
    public Image slot2IconDisplay;
    public ConsumableItem slot3Item;
    public Image slot3IconDisplay;

    private NavMeshAgent agent;
    private bool isUsing = false;

    // [중요] 캐릭터의 '진짜 기본 속도'를 보관할 변수
    private float baseSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed; // 게임 시작 시의 속도(예: 3.5)를 딱 한 번 저장

        if (slot1Item != null && slot1IconDisplay != null) slot1IconDisplay.sprite = slot1Item.icon;
        if (slot2Item != null && slot2IconDisplay != null) slot2IconDisplay.sprite = slot2Item.icon;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isUsing && slot1Item != null)
            StartCoroutine(UseItemRoutine(slot1Item));

        if (Input.GetKeyDown(KeyCode.Alpha2) && !isUsing && slot2Item != null)
            StartCoroutine(UseItemRoutine(slot2Item));
    }

    IEnumerator UseItemRoutine(ConsumableItem item)
    {
        isUsing = true;

        // 1. 캐스팅 시작: 현재 적용 중인 속도(기본+도핑 등)에서 배율 적용
        float speedBeforeCast = agent.speed;
        agent.speed = speedBeforeCast * item.speedMultiplier;
        Debug.Log($"{item.itemName} 사용 중... 속도: {agent.speed}");

        // 2. 캐스팅 시간 대기
        yield return new WaitForSeconds(item.castTime);

        // 3. 아이템 효과 발동 (여기서 도핑 루틴이 시작됨)
        ApplyItemEffect(item);

        // 4. [수정 포인트] 캐스팅 종료 후 속도 복구
        // 도핑 효과가 즉시 적용되었다면 그 값을 반영해야 하므로, 
        // 단순히 이전 속도로 돌리는 게 아니라 '현재 상태'에 맞게 계산합니다.
        if (item.moveSpeedBoost > 0)
        {
            // 도핑템은 이미 SpeedBoostRoutine에서 속도를 올렸으므로 건드리지 않음
        }
        else
        {
            agent.speed = speedBeforeCast; // 일반 아이템은 캐스팅 전 속도로 복구
        }

        isUsing = false;
        Debug.Log($"{item.itemName} 사용 완료. 속도: {agent.speed}");
    }

    void ApplyItemEffect(ConsumableItem item)
    {
        if (item.healAmount > 0) Debug.Log($"{item.itemName} 체력 회복!");

        if (item.moveSpeedBoost > 0)
        {
            // 도핑 루틴 시작
            StartCoroutine(SpeedBoostRoutine(item.moveSpeedBoost, item.duration));
        }
    }

    IEnumerator SpeedBoostRoutine(float boostAmount, float duration)
    {
        // 도핑 시작: baseSpeed에 더해줍니다.
        agent.speed = baseSpeed + boostAmount;
        Debug.Log($"도핑 유지 시작! 속도: {agent.speed}");

        yield return new WaitForSeconds(duration);

        // 도핑 종료: 다시 baseSpeed로 정확히 복구
        agent.speed = baseSpeed;
        Debug.Log($"도핑 종료! 속도: {agent.speed}");
    }
}