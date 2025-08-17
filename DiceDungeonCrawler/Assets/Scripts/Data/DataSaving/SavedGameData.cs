using System.Collections.Generic;
using Data.Dice;
using Runtime.Gameplay;
using UnityEngine;

namespace Data.DataSaving
{
    
    [System.Serializable]
    public class SavedGameData
    {
        
        public int levelIndex;
        public MapPointData lastSelectedPoint;
        public List<SavedDiceData> m_savedDiceRoster, m_savedInventory, m_savedPerkDice;
        public List<LevelItem> savedRunLevels;
        
        public SavedGameData()
        {
            this.levelIndex = 0;
            m_savedDiceRoster = new List<SavedDiceData>();
            m_savedInventory = new List<SavedDiceData>();
            m_savedPerkDice = new List<SavedDiceData>();
            savedRunLevels = new List<LevelItem>();
        }
    }
}