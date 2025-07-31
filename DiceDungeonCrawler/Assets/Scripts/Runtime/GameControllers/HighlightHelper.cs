using System;
using Project.Scripts.Utils;
using Runtime.GameControllers;
using Runtime.Selection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runtime.Submodules
{
    public class HighlightHelper: MonoBehaviour
    {

        #region Actions

        public static event Action<ISelectable> onHoverChanged;
        
        #endregion
        
        #region Serialized Fields

        [SerializeField] private LayerMask selectableLayers;

        #endregion

        #region Private Fields

        private UnityEngine.Camera m_mainCamera;

        #endregion

        #region Accessors
        
        public ISelectable currentHovered { get; private set; }

        public UnityEngine.Camera mainCamera => CommonUtils.GetRequiredComponent(ref m_mainCamera, () =>
        {
            var c = UnityEngine.Camera.main;
            return c;
        });

        #endregion

        #region Unity Events

        private void Update()
        {
            CheckHover();
        }

        #endregion

        #region Class Implementation
        
        private void CheckHover()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                CheckHoveredObject();
                return;
            }
            
            if (mainCamera.IsNull())
            {
                return;
            }

            if (!Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, selectableLayers))
            {
                CheckHoveredObject();
                return;
            }

            hit.collider.TryGetComponent(out ISelectable selectable);

            if (selectable.IsNull())
            {
                CheckHoveredObject();
                return;
            }

            if (currentHovered == selectable)
            {
                return;
            }
            
            CheckHoveredObject();
            currentHovered = selectable;
            selectable.OnHoverStart();
                
            //Set action on player
            onHoverChanged?.Invoke(selectable);

        }

        private void CheckHoveredObject()
        {
            if (currentHovered.IsNull())
            {
                return;
            }
            
            currentHovered.OnHoverEnd();
            currentHovered = null;
        }

        #endregion
        
        
    }
}