using System;
using System.Threading;
using Data.TintData;
using Project.Scripts.Utils;
using Runtime.Dice.Enums;
using Runtime.Selection;
using Unity.VisualScripting;
using UnityEngine;

namespace Runtime.Dice
{
    public class TintBehaviour: MonoBehaviour, ISelectable
    {
        
        #region Accessors

        public bool hasActiveTint { get; private set; }
        
        public TintDataBase currentTintData { get; private set; }

        #endregion

        #region SerializeFields

        [SerializeField] private MeshRenderer meshRenderer;

        #endregion

        #region Private Fields
        
        private event Action<TintBehaviour,TintDataBase> onTintSelectedCallback;

        private bool IsSelectable;
        
        #endregion

        #region Class Implementation

        public void Initialize(TintDataBase tintData, Action<TintBehaviour,TintDataBase> onTintSelectCallback)
        {
            SetSelectable(false);
            currentTintData = tintData;
            SetMeshColor();
            onTintSelectedCallback = onTintSelectCallback;
        }

        private void SetMeshColor()
        {
            if (meshRenderer.IsNull() || currentTintData.IsNull())
            {
                return;
            }

            var mat = meshRenderer.materials[0];
            mat.color = currentTintData.tintColor;
        }

        public void SetSelectable(bool isSelectable)
        {
            IsSelectable = isSelectable;
        }

        #endregion
        
        #region ISelectable Inherited Methods

        public void OnSelect()
        {
            if(!IsSelectable) return;
            onTintSelectedCallback?.Invoke(this, currentTintData);
        }

        public void OnUnselect()
        {
            
        }

        public void OnHoverStart()
        {
            //float object
        }

        public void OnHoverEnd()
        {
            //stationary object
        }

        #endregion
       
    }
}