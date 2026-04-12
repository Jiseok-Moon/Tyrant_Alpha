using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Items/Consumable Item")]
public class ConsumableItem : ItemData
{
    // [기획 의도] 도핑 아이템의 세부 스펙을 정의하는 데이터 클래스.
    // 단순히 사용 즉시 효과가 나타나는 것이 아니라, '사용 시간(castTime)'과 '이동 속도 보정(speedMultiplier)' 
    // 변수를 두어 아이템 사용 시점의 리스크와 리턴을 전략적으로 조절함.
    [Header("아이템 수치 설정")]
    public float castTime;       // 사용 시간
    public float speedMultiplier; // 이동형 캐스팅 속도
    public float cooldown;       // 쿨타임

    // [데이터 설계] 회복(Heal), 이동속도(Speed), 공격력(Damage) 등의 효과를 
    // 하나의 클래스 내에서 조합할 수 있게 하여 복합 효과 아이템(예: 속도 증가+공격력 증가 물약) 제작을 용이하게 함.
    [Header("도핑 아이템 수치 설정")]
    public float healAmount;     // 회복량
    public float moveSpeedBoost; // 이동속도 증가량
    public float duration;       // 지속시간
    public float damageBoost;    // 공격력 증가량
    public ConsumableItem() { itemType = ItemType.Consumable; }
}