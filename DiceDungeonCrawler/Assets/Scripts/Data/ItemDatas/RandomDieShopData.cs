using UnityEngine;

namespace Data.ItemDatas
{
    [CreateAssetMenu(menuName = "DiceGame/Items/Random Playable Die Item")]
    public class RandomDieShopData: ItemDataBase
    {
        public int amountOfSides;
    }
}