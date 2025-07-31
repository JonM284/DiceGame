using UnityEngine;

namespace Data.Dice
{
    [CreateAssetMenu(menuName = "DiceGame/Dice/New Modifier Die")]
    public class ModifierDiceData: ScriptableObject
    {
        public ModifierDieActivationType activationType;
        
        public float multiplier, additionalPoints;
    }
}