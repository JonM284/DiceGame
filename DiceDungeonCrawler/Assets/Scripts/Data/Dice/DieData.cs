using System;
using System.Collections.Generic;
using Runtime.Dice;
using UnityEngine;

namespace Data.Dice
{
    [CreateAssetMenu(menuName = "DiceGame/Dice/New Die")]
    public class DieData: ScriptableObject
    {
        public string dieGuid = "";
        public List<int> valuesPerSide;
        public List<string> upgradeGuids;
        //Change with addressable later
        public BaseDie diePrefab;
        
        private void OnValidate()
        {
            GenerateID();
        }

        [ContextMenu("Generate GUID")]
        private void GenerateID()
        {
            if (!string.IsNullOrEmpty(dieGuid))
            {
                return;
            }
            
            dieGuid = System.Guid.NewGuid().ToString();
        }
        
    }
}