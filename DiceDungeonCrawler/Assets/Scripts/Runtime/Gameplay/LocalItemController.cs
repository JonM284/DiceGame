using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.ItemDatas;
using DG.Tweening;
using Runtime.GameControllers;
using Runtime.GameplayItems;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class LocalItemController: MonoBehaviour
    {

        /// <summary>
        /// Used to keep track of saved usable items
        /// </summary>

        #region Read-Only

        private static readonly string itemPoolName = "gameplay_items";

        #endregion
        
        #region Serialized Fields

        [SerializeField] private List<Transform> itemPositions = new();
        [SerializeField] private float itemCreateMoveDuration = 0.5f;

        #endregion
        
        #region Instance

        public static LocalItemController Instance { get; private set; }

        #endregion

        #region Private Fields

        private List<GameplayItemWrapperInstance> currentSavedItems = new();
        
        #endregion

        #region Accessors

        public int maxItemAmount => itemPositions.Count;

        public int currentStoredItemAmount => currentSavedItems.Count;
        
        #endregion

        #region Class Implementation

        /// <summary>
        /// Whenever creating new items/
        /// </summary>
        public async UniTask AddItemAsync(ItemDataBase newItem, CancellationToken token)
        {
            if (currentStoredItemAmount >= maxItemAmount) return;
            
            //create new wrapper data for saving
            var newWrapperData = new GameplayItemWrapperInstance(newItem.itemGuid);

            var itemPosition = itemPositions[currentStoredItemAmount].position;
            var startPosition = itemPosition + new Vector3(0, 4, 0);
            
            //create new runtime usable item
            var gameplayItem = await ObjectPoolController.Instance.CreateObjectAsync(itemPoolName, newItem.GetUsableItem(), startPosition, token);
            
            currentSavedItems.Add(newWrapperData);

            await gameplayItem.transform.DOMove(itemPosition, itemCreateMoveDuration)
                .SetEase(Ease.InOutElastic)
                .WithCancellation(token);

            if (!gameplayItem.TryGetComponent(out GameplayItemsBase gameplayItemComp))
            {
                return;
            }
            
            
        }

        /// <summary>
        /// When Loading Saved Items
        /// </summary>
        private async UniTask RestoreSavedItems()
        {
            
        }
        
        #endregion


    }
}