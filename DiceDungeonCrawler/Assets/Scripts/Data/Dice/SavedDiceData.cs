using System;
using System.Collections.Generic;

namespace Data.Dice
{
    [Serializable]
    public class SavedDiceData
    {
        public string guid;
        public List<int> valuesPerSide;
        public List<string> perkGuids;
        
        public SavedDiceData(string _dieGuid)
        {
            this.guid = _dieGuid;
        }
    }
}