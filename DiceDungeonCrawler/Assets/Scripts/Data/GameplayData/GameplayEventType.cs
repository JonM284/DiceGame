using Runtime.Gameplay;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName="DiceGame/Gameplay/New Event")]
    public class GameplayEventType : ScriptableObject
    {

        #region Serialized Fields

        [SerializeField] private Sprite m_eventSprite;

        [SerializeField] private EMapLocationType m_mapLocationType;

        #endregion

        #region Public Fields

        public string eventGUID;

        #endregion
        
        #region Accessors

        public Sprite eventSprite => m_eventSprite;

        public EMapLocationType locationType => m_mapLocationType;

        #endregion

        [ContextMenu("Generate GUID")]
        private void GenerateID()
        {
            if (eventGUID != string.Empty)
            {
                return;
            }
            
            eventGUID = System.Guid.NewGuid().ToString();
        }

    }
}