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

        public static event Action<MapLocationAction> OnLocationSelected;

        #endregion

        #region Serialized Fields

        [SerializeField] private GameObject m_highlightGO;

        [SerializeField] private SpriteRenderer m_iconSR;

        [SerializeField] private Collider m_collider;

        [SerializeField] private Color normalColor = Color.grey,
            hoverColor = Color.green;

        #endregion

        #region Accessors

        public GameplayEventType m_assignedEventData { get; private set; }

        public EMapLocationType locationType { get; private set; }

        public bool canBeSelected { get; private set; }

        public Vector3 savedLocation { get; private set; }

        public MapPointData assignedData { get; private set; }

        #endregion

        #region Class Implementation

        public void Initialize(GameplayEventType _eventType, MapPointData _assignedData)
        {
            if (_eventType.IsNull() || _assignedData.IsNull())
            {
                return;
            }
            
            assignedData = _assignedData;
            m_assignedEventData = _eventType;
            locationType = _eventType.locationType;
            m_iconSR.sprite = _eventType.eventSprite;
            m_iconSR.color = normalColor;
            savedLocation = transform.localPosition;
            
            m_collider.enabled = false;
            canBeSelected = false;
        }

        public void SetSelectable(bool _isSelectable)
        {
            canBeSelected = _isSelectable;
            m_collider.enabled = _isSelectable;
        }

        public void SetPassed()
        {
            canBeSelected = false;
            m_collider.enabled = false;
        }
        
        #endregion

        #region ISelectable Inherited Methods

        public void OnSelect()
        {
            if (!canBeSelected)
            {
                return;
            }
            
            OnLocationSelected?.Invoke(this);
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

            m_iconSR.color = hoverColor;
            m_highlightGO.SetActive(true);
        }

        public void OnHoverEnd()
        {
            if (!canBeSelected)
            {
                return;
            }

            m_highlightGO.SetActive(false);
            m_iconSR.color = normalColor;
        }

        #endregion
        
        
    }
}