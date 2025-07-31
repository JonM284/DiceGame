using System.Collections.Generic;
using Data.Dice;
using Runtime.Gameplay;
using UnityEngine;

namespace Data.DataSaving
{
    
    [System.Serializable]
    public class SavedGameData
    {
        
        public int levelIndex, chosenEventIndex, savedMapSelectionLevel;
        public string m_currentEventIdetifier;
        public Vector3 m_lastPressedMapPoisiton;
        public List<SavedDiceData> m_savedDiceRoster, m_savedInventory, m_savedPerkDice;
        public List<LocalMapController.RowData> savedMap = new List<LocalMapController.RowData>();
        
        public SavedGameData()
        {
            this.levelIndex = 0;
            this.chosenEventIndex = 0;
            this.m_currentEventIdetifier = "";
            this.m_lastPressedMapPoisiton = Vector3.zero;
        }
    }
}