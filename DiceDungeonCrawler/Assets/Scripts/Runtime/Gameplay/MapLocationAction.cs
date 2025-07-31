using System;
using Data;
using Project.Scripts.Utils;
using Runtime.Selection;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class MapLocationAction: MonoBehaviour, ISelectable
    {

        #region Actions

        public static event Action<EMapLocationType> OnLocationSelected;

        #endregion

        #region Serialized Fields

        [SerializeField] private GameObject m_highlightGO;

        [SerializeField] private SpriteRenderer m_iconSR;

        [SerializeField] private Collider m_collider;

        #endregion

        #region Accessors

        public GameplayEventType m_assignedEventData { get; private set; }

        public EMapLocationType locationType { get; private set; }

        public bool canBeSelected { get; private set; }

        public Vector3 savedLocation { get; private set; }

        #endregion

        #region Class Implementation

        public void Initialize(GameplayEventType _eventType)
        {
            if (_eventType.IsNull())
            {
                return;
            }

            m_assignedEventData = _eventType;
            locationType = _eventType.locationType;
            m_iconSR.sprite = _eventType.eventSprite;
            savedLocation = transform.localPosition;
        }

        public void SetSelectable(bool _isSelectable)
        {
            canBeSelected = _isSelectable;
            m_collider.enabled = _isSelectable;
        }

        #endregion

        #region ISelectable Inherited Methods

        public void OnSelect()
        {
            if (!canBeSelected)
            {
                return;
            }
            
            OnLocationSelected?.Invoke(locationType);
        }

        public void OnUnselect()
        {
            
        }

        public void OnHoverStart()
        {
            if (!canBeSelected)
            {
                return;
            }
            
            m_highlightGO.SetActive(true);
        }

        public void OnHoverEnd()
        {
            if (!canBeSelected)
            {
                return;
            }

            m_highlightGO.SetActive(false);
        }

        #endregion
        
        
    }
}