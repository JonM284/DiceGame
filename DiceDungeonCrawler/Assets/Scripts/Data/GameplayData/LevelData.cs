using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    
    [CreateAssetMenu(menuName="DiceGame/Gameplay/Level Data")]
    public class LevelData: ScriptableObject
    {

        #region Nested Classes

        [Serializable]
        public class LevelInfo
        {
            public float levelBasePoints;
        }

        #endregion

        public List<LevelInfo> m_levelInfos = new List<LevelInfo>();
        public float normalFoeMod = 1f, mediumFoeMod = 1.5f, hardFoeMod = 2f;

    }
}