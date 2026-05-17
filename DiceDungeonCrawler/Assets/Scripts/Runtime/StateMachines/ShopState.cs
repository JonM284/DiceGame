using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.Dice;
using Data.ItemDatas;
using DG.Tweening;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.Dice;
using Runtime.Dice.Enums;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Shop State")]
    public class ShopState: StateBase
    {
        #region Serialized Fields

        [SerializeField] private float objectMoveDuration = 0.5f;
        [SerializeField] private AnimationCurve objectMoveCurve = new AnimationCurve();
        [SerializeField] private Transform selectedDieLocation;
        [SerializeField] private List<Transform> shopItemPositions = new();
        [SerializeField] private List<ItemDataBase> allAvailableShopItems = new();
        [SerializeField] private List<ShopItemBehaviour> shopItemObjects = new();
        
        #endregion

        #region Private Fields

        private PlayableDice selectedPlayableDie;
        private Quaternion selectedDieOriginalRotation;
        private Vector3 selectedDieOriginalPosition;
        private Tweener selectedDieShakeTweener;

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            BaseDie.onDieSelected += OnDieSelected;
        }

        private void OnDisable()
        {
            BaseDie.onDieSelected -= OnDieSelected;
        }

        #endregion
        
        #region StateBase Inherited Methods

        public override async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            selectedPlayableDie = null;
            await base.EnterState(token);
            await CreateShopItemsAsync(token);
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

        private async UniTask CreateShopItemsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            var availableShopItems = allAvailableShopItems.ToNewList();
            var tasks = new List<UniTask>();
            
            //ToDo: Need to add weight when choosing tints
            foreach (var bottle in shopItemObjects)
            {
                var randomItemData = availableShopItems[Random.Range(0, availableShopItems.Count)];

                if (randomItemData is RandomDieShopData randomDieShopData)
                {
                    var dieSides = new List<int>();
                    for (int i = 0; i < randomDieShopData.amountOfSides; i++)
                    {
                        dieSides.Add(Random.Range(1, randomDieShopData.amountOfSides + 1));
                    }
                    await bottle.Initialize(randomItemData, dieSides, OnShopItemSelected, token);
                }
                else
                {
                    await bottle.Initialize(randomItemData, OnShopItemSelected, token);
                }
                
                availableShopItems.Remove(randomItemData);
                Debug.Log($"[Shop Item Created] -> {randomItemData.name}");
            }

            for (int i = 0; i < shopItemObjects.Count; i++)
            {
                tasks.Add(shopItemObjects[i].transform.DOMove(shopItemPositions[i].position, objectMoveDuration)
                    .SetEase(objectMoveCurve)
                    .ToUniTask(cancellationToken: token));
            }

            await tasks;
            
            shopItemObjects.ForEach(sib => sib.SetSelectable(true));
        }
        
        private void OnShopItemSelected(ShopItemBehaviour selectedShopItemObject, List<int> dieSides, ItemDataBase shopItemData)
        {
            if (selectedShopItemObject.IsNull())
            {
                return;
            }

            MoveSelectedItem(selectedShopItemObject, shopItemData, dieSides, GetToken()).Forget();
        }
        
        private async UniTask MoveSelectedItem(ShopItemBehaviour selectedShopItemObject, ItemDataBase shopItemData, List<int> dieSides, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var tasks = new List<UniTask>();

            var finalTask = new UniTask();

            foreach (var shopItem in shopItemObjects)
            {
                var isSelectedObject = shopItem == selectedShopItemObject;
                var EndPosition = shopItem.transform.position + new Vector3(0, 4, 0);
                if (isSelectedObject)
                {
                    finalTask = shopItem.transform.DOMove(EndPosition, objectMoveDuration).SetEase(objectMoveCurve)
                        .ToUniTask(cancellationToken: token);
                }
               
                tasks.Add(shopItem.transform.DOMove(EndPosition, objectMoveDuration).SetEase(objectMoveCurve).ToUniTask(cancellationToken: token));
            }

            await tasks;
            await UniTask.Delay(1000, cancellationToken: token);
            await finalTask;

            //Hide Shop Item Objects
            shopItemObjects.ForEach(sib =>
            {
                sib.SetSelectable(false);
            });
            
            //Create selected object in usable form
            switch (shopItemData.shopItemType)
            {
                case ShopItemType.INSTANT_USE:
                    await ApplyInstantItemAsync(shopItemData, token);
                    break;
                case ShopItemType.CONSUMABLE:
                    await LocalItemController.Instance.AddItemAsync(shopItemData, token);
                    break;
                case ShopItemType.PLAYABLE_DIE:
                    //add playable die
                    await AddDieToInventoryAsync(dieSides);
                    break;
                case ShopItemType.MODIFIER_DIE:
                    //add modifier die
                    
                    break;
            }

            stateManager.ChangeState(ERunState.MAP);
        }

        #region Instant Item Actions

        private async UniTask ApplyInstantItemAsync(ItemDataBase shopItemData, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            if (shopItemData.instantChangeItemType == InstantChangeItemType.NONE)
            {
                return;
            }

            selectedPlayableDie = null;
            
            switch (shopItemData.instantChangeItemType)
            {
                case InstantChangeItemType.PLUS_ONE_EACH_SIDE:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await AddSubtractEachDieFaceAsync(true, token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                case InstantChangeItemType.MINUS_ONE_EACH_SIDE:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await AddSubtractEachDieFaceAsync(false, token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                case InstantChangeItemType.ROUND_UP_UNEVEN_TO_EVEN:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await RoundAllSidesUpAsync(true, token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                case InstantChangeItemType.ROUND_UP_EVEN_TO_UNEVEN:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await RoundAllSidesUpAsync(false, token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                case InstantChangeItemType.SET_ALL_SIDES_EQUAL_RANDOM:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await SetAllSidesEqualRandomAsync(token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                case InstantChangeItemType.CHANGE_ALL_FACES_TO_ONE:
                    await LocalDiceController.Instance.DisplayDice(true, token);
                    await SetAllFacesToOneAsync(token);
                    await LocalDiceController.Instance.DisplayDice(false, token);
                    break;
                default:
                    return;
            }
        }

        private async UniTask ShakeSelectedDieAsync(CancellationToken token)
        {
            selectedDieOriginalPosition = selectedPlayableDie.transform.position;
            selectedDieOriginalRotation = selectedPlayableDie.transform.rotation;
            await selectedPlayableDie.transform.DOMove(selectedDieLocation.position, 0.5f)
                .SetEase(Ease.InOutElastic)
                .ToUniTask(cancellationToken: token);
            
            selectedDieShakeTweener = selectedPlayableDie.transform.DOShakeRotation(1f)
                .SetLoops(-1)
                .SetEase(Ease.InOutElastic);

            await UniTask.Delay(1000, cancellationToken: token);
        }

        private async UniTask StopShakerAsync(CancellationToken token)
        {
            selectedDieShakeTweener.Kill();
            await selectedPlayableDie.transform.DORotateQuaternion(selectedDieOriginalRotation, 0.5f)
                .ToUniTask(cancellationToken: token);
            await selectedPlayableDie.transform.DOMove(selectedDieOriginalPosition, 0.5f).SetEase(Ease.InOutElastic)
                .ToUniTask(cancellationToken: token); 
        }
        

        private async UniTask AddSubtractEachDieFaceAsync(bool isIncrease, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await UniTask.WaitUntil(() => !selectedPlayableDie.IsNull(), cancellationToken: token);

            await ShakeSelectedDieAsync(token);
            
            var faces = selectedPlayableDie.GetFaces();
            
            faces.ForEach(df =>
            {
                df.value += isIncrease ? 1 : -1;
                df.faceValueText.ForEach(tmpText => tmpText.text = df.value.ToString());
            });

            await StopShakerAsync(token);
        }

        private async UniTask SetAllFacesToOneAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await UniTask.WaitUntil(() => !selectedPlayableDie.IsNull(), cancellationToken: token);

            await ShakeSelectedDieAsync(token);
            
            var faces = selectedPlayableDie.GetFaces();
            
            faces.ForEach(df =>
            {
                df.value = 1;
                df.faceValueText.ForEach(tmpText => tmpText.text = df.value.ToString());
            });

            await StopShakerAsync(token);
        }
        
        private async UniTask SetAllSidesEqualRandomAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await UniTask.WaitUntil(() => !selectedPlayableDie.IsNull(), cancellationToken: token);

            await ShakeSelectedDieAsync(token);
            
            var faces = selectedPlayableDie.GetFaces();

            var randomValue = faces[Random.Range(0, faces.Count)].value;
            
            faces.ForEach(df =>
            {
                df.value = randomValue;
                df.faceValueText.ForEach(tmpText => tmpText.text = df.value.ToString());
            });

            await StopShakerAsync(token);
        }
        
        private async UniTask RoundAllSidesUpAsync(bool isToEven, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await UniTask.WaitUntil(() => !selectedPlayableDie.IsNull(), cancellationToken: token);

            await ShakeSelectedDieAsync(token);
            
            var faces = selectedPlayableDie.GetFaces();
            
            faces.ForEach(df =>
            {
                df.value = isToEven ? df.value % 2 > 0 ? df.value + 1 : df.value
                    : df.value % 2 == 0 ? df.value + 1 : df.value;
                df.faceValueText.ForEach(tmpText => tmpText.text = df.value.ToString());
            });

            await StopShakerAsync(token);
        }
        
        private void OnDieSelected(BaseDie selectedDie)
        {
            if (!isCurrentState)
            {
                return;
            }
            
            if (selectedDie.IsNull())
            {
                return;
            }

            if (!selectedPlayableDie.IsNull())
            {
                return;
            }

            if (selectedDie is not PlayableDice playableDie)
            {
                return;
            }

            selectedPlayableDie = playableDie;
        }

        #endregion

        #region Playable Dice Actions

        private async UniTask CreateNewDieAsync()
        {
            
        }

        private async UniTask AddDieToInventoryAsync(List<int> dieSides)
        {
            // Add Die to Die Inventory
        }

        #endregion
        

        #endregion
        
        
        
    }
}