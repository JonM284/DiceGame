using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.ItemDatas;
using DG.Tweening;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.GameControllers;
using Runtime.Selection;
using UnityEngine;

namespace Runtime.Dice
{
    public class ShopItemBehaviour: MonoBehaviour, ISelectable
    {

        /// <summary>
        /// Shop items can be many different types of items.
        /// However, when they are in the shop, they all act the same.
        /// Similarities: Price, Rarity, Buy
        /// Differences: Dice, Gameplay Items, Modifiers, Tints?, etc.
        /// Therefore, after selecting the shop item.
        /// 1. During the callback, assign the appropriate data to savable memory
        /// 2. give an interactive object to the player using the correct scripts 
        /// </summary>

        #region Read-only

        private readonly string itemObjectPoolName = "{0}_ShopItem";

        #endregion
        
        #region Accessors
        
        public ItemDataBase currentData { get; private set; }

        public List<int> randomDieSides = new();

        #endregion

        #region SerializeFields

        [SerializeField] private Transform visualParent;
        [SerializeField] private GameObject defaultVisual;
        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField] private float scaleOffset = 0.5f, hoverAnimDuration = 0.1f;
        [SerializeField] private float heightOffset = 0.5f;
        
        #endregion

        #region Private Fields
        
        private event Action<ShopItemBehaviour,List<int>,ItemDataBase> onShopItemSelectedCallback;

        private bool IsSelectable;

        private string itemDescription;
        
        private Vector3 restSize;
        private Vector3 restPosition;
        
        #endregion

        #region Class Implementation

        public async UniTask Initialize(ItemDataBase shopItemData, Action<ShopItemBehaviour,List<int>,ItemDataBase> m_onShopItemSelectCallback, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(shopItemData.IsNull()) return;
            SetSelectable(false);
            currentData = shopItemData;
            onShopItemSelectedCallback = m_onShopItemSelectCallback;
            
            var itemVisual = currentData.GetItemVisual();
            
            defaultVisual.SetActive(itemVisual.IsNull());

            if (itemVisual.IsNull())
            {
                return;
            }
            
            await ObjectPoolController.Instance.CreateParentedObjectAsync(string.Format(itemObjectPoolName, currentData.name),
                itemVisual, visualParent, token);

            itemDescription = $"Price: {currentData.itemPrice} \r\n Description: {currentData.itemDescription}";
        }

        public async UniTask Initialize(ItemDataBase shopItemData, List<int> randomDieSides, Action<ShopItemBehaviour,List<int>,ItemDataBase> m_onShopItemSelectCallback, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(shopItemData.IsNull()) return;
            if(randomDieSides.IsNull() || randomDieSides.Count == 0) return;
            
            SetSelectable(false);
            currentData = shopItemData;
            this.randomDieSides = randomDieSides;
            onShopItemSelectedCallback = m_onShopItemSelectCallback;
            
            var itemVisual = currentData.GetItemVisual();
            
            defaultVisual.SetActive(itemVisual.IsNull());

            if (itemVisual.IsNull())
            {
                return;
            }
            
            var visuals = await ObjectPoolController.Instance.CreateParentedObjectAsync(string.Format(itemObjectPoolName, currentData.name),
                itemVisual, visualParent, token);

            if (!visuals.TryGetComponent(out ShopDieVisualBehaviour shopDieVisualBehaviour))
            {
                return;
            }

            await shopDieVisualBehaviour.Initialize(randomDieSides);

            itemDescription = $"Price: {currentData.itemPrice} \r\n {randomDieSides.Count} sided Die: \r\n Sides:";

            for (int i = 0; i < this.randomDieSides.Count; i++)
            {
                itemDescription += i == 0 || i == randomDieSides.Count - 1 ? $" {randomDieSides[i]}" : $" {randomDieSides[i]},";
            }
        }
        

        public void SetSelectable(bool isSelectable)
        {
            if (isSelectable)
            {
                restSize = visualParent.transform.localScale;
                restPosition = visualParent.transform.position;
            }
            
            IsSelectable = isSelectable;
        }
        
        private void HoverEffects(bool _onHover)
        {
            UIController.Instance.ShowInfoText(_onHover, itemDescription);

            visualParent.transform.DOLocalMove(_onHover ? restPosition + new Vector3(0, heightOffset, 0) : restPosition, hoverAnimDuration);
        }

        private void ResetItem()
        {
            visualParent.transform.localScale = restSize;
            visualParent.transform.localPosition = restPosition;
            SetSelectable(false);
        }
        
        #endregion
        
        #region ISelectable Inherited Methods

        public void OnSelect()
        {
            if(!IsSelectable) return;
            if(currentData.IsNull()) return;
            ResetItem();
            onShopItemSelectedCallback?.Invoke(this, randomDieSides, currentData);
        }

        public void OnUnselect()
        {
            
        }

        public void OnHoverStart()
        {
            //show item description
            HoverEffects(true);
        }

        public void OnHoverEnd()
        {
            //hide item description
            HoverEffects(false);
        }

        #endregion
    }
}