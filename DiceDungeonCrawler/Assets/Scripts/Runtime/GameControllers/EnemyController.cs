using Data;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class EnemyController: GameControllerBase
    {

        #region Instance

        public static EnemyController Instance { get; private set; }

        #endregion
        
        #region Serialized Fields

        [SerializeField] private LevelData m_levelData;

        #endregion
        
        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion

        #region Class Implementation

        public LevelData GetLevelData()
        {
            return m_levelData;
        }

        public float GetBattleScoreByLevel(int _index)
        {
            return _index >= m_levelData.m_levelInfos.Count ? -1 : m_levelData.m_levelInfos[_index].levelBasePoints;
        }

        #endregion

    }
}