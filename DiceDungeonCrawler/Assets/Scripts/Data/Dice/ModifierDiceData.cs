using UnityEngine;

namespace Data.Dice
{
    [CreateAssetMenu(menuName = "DiceGame/Dice/New Modifier Die")]
    public class ModifierDiceData: ScriptableObject
    {
        public ModifierDieActivationType activationType = ModifierDieActivationType.ON_PLAYED;
        public ModifierDieActionType actionType = ModifierDieActionType.ADD_POINTS;
        public ModifierDieConditionType conditionType = ModifierDieConditionType.NONE;
        public float multiplier = 1f, additionalPoints;
        public GameObject modifierDiePrefab;
        public Sprite dieIcon;
    }
}