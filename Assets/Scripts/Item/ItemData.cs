using UnityEngine;

// [기획 의도] 게임 내 모든 아이템의 공통 속성을 정의하는 최상위 추상 클래스.
// 새로운 아이템 타입(소비, 장착, 유물 등)이 추가되더라도 기초 데이터를 일관되게 관리하도록 설계함.
public enum ItemType { Consumable, Accessory, Relic }

// [시스템 매커니즘] 유니티 ScriptableObject를 상속받아, 기획자가 에셋 형태로 
// 아이템 데이터를 생성하고 인스펙터에서 즉시 편집 가능한 환경을 구축함.
public abstract class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemType itemType;
}