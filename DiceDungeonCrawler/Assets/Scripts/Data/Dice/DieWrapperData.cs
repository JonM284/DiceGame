using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Dice.Enums;

namespace Data.Dice
{
    [Serializable]
    public class DieWrapperData
    {
        public List<int> faceValues = new();
        public TintType tintType;
        
        public DieWrapperData(List<int> faceValues, TintType tintType)
        {
            this.faceValues = faceValues.ToList();
            this.tintType = tintType;
        }
    }
}