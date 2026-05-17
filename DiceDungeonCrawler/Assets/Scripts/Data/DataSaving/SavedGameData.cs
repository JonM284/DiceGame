using System.Collections.Generic;
using Data.Dice;
using Runtime.Gameplay;

namespace Data.DataSaving
{
    
    [System.Serializable]
    public class SavedGameData
    {
        
        public int levelIndex;
        public MapPointData lastSelectedPoint;
        public List<DieWrapperData> m_savedDiceRoster, m_savedInventory;
        public List<ModDieWrapperData> m_savedModDice;
        public SerializableDictionary<int,LevelItem> savedRunLevels;
        
        public SavedGameData()
        {
            this.levelIndex = 0;
            m_savedDiceRoster = new List<DieWrapperData>();
            m_savedInventory = new List<DieWrapperData>();
            m_savedModDice = new List<ModDieWrapperData>();
            savedRunLevels = new SerializableDictionary<int,LevelItem>();
        }
    }
}