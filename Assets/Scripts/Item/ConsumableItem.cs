using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Items/Consumable Item")]
public class ConsumableItem : ItemData
{
    [Header("소모품 수치 설정")]
    public float castTime;       // 사용 시간
    public float speedMultiplier; // 이동형 캐스팅 속도
    public float healAmount;     // 회복량
    public float cooldown;       // 쿨타임

    public ConsumableItem() { itemType = ItemType.Consumable; }
}