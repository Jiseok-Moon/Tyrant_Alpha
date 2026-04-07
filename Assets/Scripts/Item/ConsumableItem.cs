using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Items/Consumable Item")]
public class ConsumableItem : ItemData
{
    [Header("아이템 수치 설정")]
    public float castTime;       // 사용 시간
    public float speedMultiplier; // 이동형 캐스팅 속도
    public float cooldown;       // 쿨타임

    [Header("도핑 아이템 수치 설정")]
    public float healAmount;     // 회복량
    public float moveSpeedBoost; // 이동속도 증가량
    public float duration;       // 지속시간
    public float damageBoost;    // 공격력 증가량
    public ConsumableItem() { itemType = ItemType.Consumable; }
}