using System;
using Data.Dice;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(ModifierDiceData))]
    public class ModifierDiceEditor: UnityEditor.Editor
    {
        private ModifierDiceData m_diceData;
        
        private void OnEnable()
        {
            m_diceData = target as ModifierDiceData;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            m_diceData.activationType = (ModifierDieActivationType)EditorGUILayout.EnumPopup("Activation Type", m_diceData.activationType);
        }
    }
}