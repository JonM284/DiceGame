using Runtime.Dice.Enums;
using UnityEngine;

namespace Data.TintData
{
    [CreateAssetMenu(menuName = "DiceGame/Tints/New Tint")]
    public class TintDataBase: ScriptableObject
    {
        public int tintWeight;
        public TintType tintType;
        public Color tintColor = Color.white;
    }
}