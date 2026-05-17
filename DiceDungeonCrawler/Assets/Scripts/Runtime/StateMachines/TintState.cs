using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.TintData;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.Dice;
using Runtime.Dice.Enums;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Tint State")]
    public class TintState: StateBase
    {

        #region Serialized Fields

        [SerializeField] private Transform dieTintPosition;
        [SerializeField] private float objectMoveDuration = 0.5f;
        [SerializeField] private AnimationCurve objectMoveCurve = new AnimationCurve();
        [SerializeField] private Transform selectedTintLocation;
        [SerializeField] private List<Transform> tintVisualPositions = new();
        [SerializeField] private List<TintDataBase> allAvailableTints = new();
        [SerializeField] private List<TintBehaviour> tintBottles = new();
        
        #endregion

        #region Private Fields
        
        private Vector3 dieOriginalPosition;

        private bool hasSelected, isMovingDie, canSelectDie;

        private TintBehaviour selectedTintBottle;

        #endregion

        #region Accessors

        public PlayableDice selectedPlayableDie { get; private set; }

        public TintDataBase SelectedTint { get; private set; }

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
            hasSelected = false;
            selectedPlayableDie = null;
            
            await base.EnterState(token);
            await CreateTintsAsync(token);
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
            
        }

        #endregion

        #region Class Implementation
        
        private void OnTintSelected(TintBehaviour selectedTintObject, TintDataBase newTint)
        {
            if (selectedTintObject.IsNull())
            {
                return;
            }

            MoveSelectedTintType(selectedTintObject, newTint, GetToken()).Forget();
        }

        private async UniTask MoveSelectedTintType(TintBehaviour selectedTintObject, TintDataBase selectedTintType, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SelectedTint = selectedTintType;
            selectedTintBottle = selectedTintObject;

            var tasks = new List<UniTask>();

            foreach (var tintBottle in tintBottles)
            {
                var isSelectedBottle = tintBottle == selectedTintObject;
                var EndPosition = isSelectedBottle
                    ? selectedTintLocation.position
                    : tintBottle.transform.position + new Vector3(0, 4, 0);
                tasks.Add(tintBottle.transform.DOMove(EndPosition, objectMoveDuration).SetEase(objectMoveCurve).ToUniTask(cancellationToken: token));
            }

            await tasks;
            
            await LocalDiceController.Instance.DisplayDice(true, token);

            canSelectDie = true;
        }


        private async UniTask CreateTintsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            canSelectDie = false;
            
            var availableTintTypes = allAvailableTints.ToNewList();
            var tasks = new List<UniTask>();
            
            //ToDo: Need to add weight when choosing tints
            foreach (var bottle in tintBottles)
            {
                var randomTint = availableTintTypes[Random.Range(0, availableTintTypes.Count)];
                bottle.Initialize(randomTint, OnTintSelected);
                availableTintTypes.Remove(randomTint);
            }

            for (int i = 0; i < tintBottles.Count; i++)
            {
                tasks.Add(tintBottles[i].transform.DOMove(tintVisualPositions[i].position, objectMoveDuration).SetEase(objectMoveCurve).ToUniTask(cancellationToken: token));
            }

            await tasks;
            
            tintBottles.ForEach(tb => tb.SetSelectable(true));
        }

        private void OnDieSelected(BaseDie selectedDie)
        {
            if (!isCurrentState || isMovingDie || !canSelectDie)
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

            canSelectDie = false;
            MoveDieAsync(playableDie, GetToken()).Forget();
        }

        private async UniTask MoveDieAsync(PlayableDice newSelectedDie, CancellationToken token)
        {
            isMovingDie = true;
            
            if (!selectedPlayableDie.IsNull())
            {
                await MoveDieBack(selectedPlayableDie, token);
            }

            //User clicked the same die again, deselect
            if (selectedPlayableDie == newSelectedDie)
            {
                selectedPlayableDie = null;
                isMovingDie = false;
                return;
            }
            
            selectedPlayableDie = newSelectedDie;
            dieOriginalPosition = newSelectedDie.transform.position;
            
            await selectedPlayableDie.transform.DOMove(dieTintPosition.position, objectMoveDuration).ToUniTask(cancellationToken: token);

            isMovingDie = false;
            
            await ApplyTintAsync(token);
            
            if (!selectedTintBottle.IsNull())
            {
                var endPosition = selectedTintBottle.transform.position + new Vector3(0, 4, 0);
                await selectedTintBottle.transform.DOMove(endPosition, moveDuration).SetEase(objectMoveCurve).ToUniTask(cancellationToken: token);
            } 
            
            await LocalDiceController.Instance.DisplayDice(false, token);
            
            stateManager.ChangeState(ERunState.MAP);
        }

        private async UniTask MoveDieBack(PlayableDice selectedDie, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await selectedDie.transform.DOMove(dieOriginalPosition, objectMoveDuration).ToUniTask(cancellationToken: token);
        }
        
        private async UniTask ApplyTintAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (selectedPlayableDie.IsNull())
            {
                return;
            }
            
            //Do Animation
            
            selectedPlayableDie.ChangeTintType(SelectedTint);
        }

        #endregion
        
        
    }
}