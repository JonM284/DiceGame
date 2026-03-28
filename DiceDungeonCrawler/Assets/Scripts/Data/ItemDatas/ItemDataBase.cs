using UnityEngine;

namespace Data.ItemDatas
{
    public class ItemDataBase: ScriptableObject
    {
        public string itemGuid = "";
        //Change with addressable later
        public GameObject itemPrefab;
        
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