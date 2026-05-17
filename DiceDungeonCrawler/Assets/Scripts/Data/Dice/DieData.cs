using System.Collections.Generic;
using Data.ItemDatas;
using Runtime.Dice;
using UnityEngine;

namespace Data.Dice
{
    [CreateAssetMenu(menuName = "DiceGame/Dice/New Die")]
    public class DieData: ItemDataBase
    {
        public List<int> valuesPerSide;
        public List<string> upgradeGuids;
    }
}