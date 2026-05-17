using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using Data.ItemDatas;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.GameplayItems;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Item Drop State")]
    public class ItemDropState: StateBase
    {

        #region Serialize Fields

        [SerializeField] private int amountOfItems = 3;
        [SerializeField] private Transform creationPoint;
        [SerializeField] private List<ItemDataBase> itemDatas = new List<ItemDataBase>();

        #endregion
        
        #region Private Fields

        private List<GameplayItemsBase> availableItems = new List<GameplayItemsBase>();

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            GameplayItemsBase.onItemSelected += OnItemSelected;
        }

        private void OnDisable()
        {
            GameplayItemsBase.onItemSelected -= OnItemSelected;
        }

        #endregion
        
        #region StateBase Inherited Methods
        
        public override async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await SelectRandomItemDatas();
            await base.EnterState(token);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override async UniTask ExitState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.ExitState(token);
        } 
        
        public override void UpdateState()
        {
            //Do interaction checks here
        }

        #endregion

        #region Class Implementation

        public void OpenBox()
        {
            //Move 3 items from box to in front of player
        }

        private async UniTask SelectRandomItemDatas()
        {
            if (itemDatas.Count == 0)
            {
                return;
            }
            
            for (int i = 0; i < amountOfItems; i++)
            {
                availableItems.Add(GetItem(itemDatas.Count == 1 ? 0 : Random.Range(0, itemDatas.Count)));
            }

            await UniTask.WaitForEndOfFrame();
        }

        private GameplayItemsBase GetItem(int _index)
        {
            var go = Instantiate(itemDatas[_index].GetItemVisual());
            go.transform.ResetPRS(creationPoint);

            go.TryGetComponent(out GameplayItemsBase _gameplayItemsBase);
            
            return _gameplayItemsBase;
        }

        private void OnItemSelected(GameplayItemsBase _gameplayItemsBase)
        {
            if (_gameplayItemsBase.IsNull())
            {
                return;
            }
            
            //Add item to inventory, send all items away
            
            
            stateManager.ChangeState(ERunState.MAP);
        }

        #endregion
        
        
    }
}