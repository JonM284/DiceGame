using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Runtime.Selection;
using UnityEngine;

namespace Runtime.GameplayItems
{
    public abstract class GameplayItemsBase: MonoBehaviour, ISelectable
    {


        #region Actions

        public static event Action<GameplayItemsBase> onItemSelected;
        public static event Action<GameplayItemsBase> onItemDeselected;
        public static event Action<GameplayItemsBase> onItemHovered;
        public static event Action<GameplayItemsBase> onItemUnhovered;

        #endregion

        #region Serialize Fields

        [SerializeField] private float scaleOffset = 0.5f, resizeDuration = 0.1f;

        #endregion

        #region Private Fields

        private Vector3 originalSize;

        #endregion

        #region Unity Events

        private void Awake()
        {
            originalSize = transform.localScale;
        }

        #endregion

        #region Class Implementation

        private void HoverEffects(bool _onHover)
        {
            transform.DOScale(_onHover ? originalSize + (Vector3.one * scaleOffset)
                : originalSize, resizeDuration).SetEase(Ease.InOutElastic);
        }

        public virtual async UniTask DoItemAbility(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

        #endregion


        public void OnSelect()
        {
            onItemSelected?.Invoke(this);
        }

        public void OnUnselect()
        {
            onItemDeselected?.Invoke(this);
        }

        public void OnHoverStart()
        {
            
            onItemHovered?.Invoke(this);
            HoverEffects(true);
        }

        public void OnHoverEnd()
        {
            onItemUnhovered?.Invoke(this);
            HoverEffects(false);
        }
    }
}