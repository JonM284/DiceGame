using System.Collections.Generic;
using UnityEngine;

namespace Data.Dice
{
    [CreateAssetMenu(menuName = "DiceGame/Dice/Starting Dice Set")]
    public class StartingDiceSets: ScriptableObject
    {
        public List<DieData> m_startingDice = new List<DieData>();
    }
}