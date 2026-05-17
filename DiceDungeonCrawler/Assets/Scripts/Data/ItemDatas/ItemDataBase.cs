using Runtime.Dice.Enums;
using UnityEngine;

namespace Data.ItemDatas
{
    public class ItemDataBase: ScriptableObject
    {
        public ShopItemType shopItemType;
        public InstantChangeItemType instantChangeItemType;
        public int itemDropProbabilityWeight;
        public int itemPrice;
        public string itemDescription;
        public string itemGuid = "";
        //Change with addressable later
        [SerializeField] private GameObject usableItemPrefab;
        [SerializeField] private GameObject itemVisualPrefab;
        
        public GameObject GetUsableItem() => usableItemPrefab;

        public GameObject GetItemVisual() => itemVisualPrefab;
        
        private void OnValidate()
        {
            GenerateID();
        }

        [ContextMenu("Generate GUID")]
        private void GenerateID()
        {
            if (!string.IsNullOrEmpty(itemGuid))
            {
                return;
            }
            
            itemGuid = System.Guid.NewGuid().ToString();
        }
    }
}