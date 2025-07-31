using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Selection
{
    public class SelectableBase: MonoBehaviour, ISelectable
    {
        
        #region Events

        public UnityEvent onSelect;
        
        public UnityEvent onUnselect;
        
        public UnityEvent onHoverStart;

        public UnityEvent onHoverEnd;

        #endregion

        #region Class Implementation

        public void OnSelect()
        {
            onSelect?.Invoke();
        }

        public void OnUnselect()
        {
            onUnselect?.Invoke();
        }

        public void OnHoverStart()
        {
            onHoverStart?.Invoke();
        }

        public void OnHoverEnd()
        {
            onHoverEnd?.Invoke();
        }

        #endregion
        
        
    }
}