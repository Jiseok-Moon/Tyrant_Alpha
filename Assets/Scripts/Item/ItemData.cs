using UnityEngine;

public enum ItemType { Consumable, Accessory, Relic }

public abstract class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemType itemType;
}