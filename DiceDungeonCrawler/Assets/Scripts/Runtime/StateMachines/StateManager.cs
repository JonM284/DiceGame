using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Project.Scripts.Utils;
using Rewired;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.Character.StateMachines
{
    public class StateManager: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private Camera m_mainCamera;
        
        [SerializeField]
        protected List<StateListItem> m_states = new List<StateListItem>();

        #endregion

        #region Protected Fields

        protected StateListItem foundState, previousState, currentSubState;
        
        protected bool interactionGuard;
        
        protected CancellationTokenSource cts;
        
        #endregion

        #region Accessors
        
        public StateListItem currentState { get; private set; }

        public bool isTransitioning { get; private set; }

        public Player rwPlayer { get; private set; }

        public Camera mainCam => m_mainCamera;

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            MapLocationAction.OnLocationSelected += OnLocationSelected;
        }

        private void OnDisable()
        {
            MapLocationAction.OnLocationSelected -= OnLocationSelected;
        }

        private void Awake()
        {
            rwPlayer = ReInput.players.GetPlayer(0);
        }

        private void Update()
        {
            if (currentState.IsNull())
            {
                return;
            }
            
            currentState.stateBehavior.UpdateState();
        }

        #endregion

        #region Class Implementation

        private void OnLocationSelected(MapLocationAction mapLocationAction)
        {
            if (mapLocationAction.IsNull())
            {
                return;
            }

            T_OnLocationSelected(mapLocationAction).Forget();
        }

        private async UniTask T_OnLocationSelected(MapLocationAction mapLocationAction)
        {
            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            cts.Token.ThrowIfCancellationRequested();
            
            LocalMapController.Instance.IncreaseCurrentMapLevel();
            
            //Level Point Visuals + moving player piece
            await LocalMapController.Instance.T_PointSelectedAsync(mapLocationAction);

            await UniTask.WaitForSeconds(0.5f);
            
            //After Everything Switch states and assign arguments
            switch (mapLocationAction.locationType)
            {
                case EMapLocationType.E_BATTLE: case EMapLocationType.M_BATTLE: case EMapLocationType.H_BATTLE:
                    await T_TransitionState(ERunState.BATTLE, cts.Token);
                    object[] arg = {EnemyController.Instance.GetBattleMod(mapLocationAction.locationType), mapLocationAction.locationType};
                    AssignValuesToCurrentState(arg);
                    break;
                case EMapLocationType.TINT:
                    await T_TransitionState(ERunState.TINT, cts.Token);
                    break;
                case EMapLocationType.SHOP:
                    await T_TransitionState(ERunState.SHOP, cts.Token);
                    break;
                case EMapLocationType.ITEM:
                    await T_TransitionState(ERunState.ITEM_DROP, cts.Token);
                    break;
            }
        }
        
        public async UniTask InitStateMachine(ERunState _startingState, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foundState = m_states.FirstOrDefault(sli => sli.stateType == _startingState);

            if (foundState.IsNull())
            {
                return;
            }
            
            foreach (var _state in m_states)
            {
                _state.stateBehavior.InitState(this, _state.stateType);
                await UniTask.WaitForEndOfFrame(token);
            }
            
            currentState = foundState;
            currentState.stateBehavior?.EnterState(token).Forget();

            foundState = null;
        }

        public async UniTask UninitStateMachine()
        {
            cts = new CancellationTokenSource();
            await currentState.stateBehavior.ExitState(cts.Token);
            currentState = null;
            cts.Cancel();
        }

        public void DoSubstateProcess(bool isEnter, ERunState _substate)
        {
            if (isTransitioning || interactionGuard)
            {
                return;
            }

            if (isEnter)
            {
                currentSubState = m_states.FirstOrDefault(c => c.stateType == _substate);

                if (currentSubState.IsNull())
                {
                    isTransitioning = false;
                    return;
                }
            }
            else
            {
                if (currentSubState.IsNull())
                {
                    return;
                }
            }

            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            T_RunSubstateProcessAsync(isEnter, cts.Token).Forget();
        }

        private async UniTask T_RunSubstateProcessAsync(bool isIn, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            isTransitioning = true;
            
            if (currentState.IsNull())
            {
                isTransitioning = false;
                return;
            }

            if (isIn)
            {
                //Take away current state, but don't transition to another state
                await currentState.stateBehavior.SuspendState(false,token);
                //Bring in current substate
                await currentSubState.stateBehavior.EnterState(token);
            }
            else
            {
                //Take away supstate
                await currentSubState.stateBehavior.ExitState(token);
                //bring back suspended main state
                await currentState.stateBehavior.SuspendState(true, token);

                currentSubState = null;
            }

            isTransitioning = false;
        }

        public void ChangeState(ERunState _newState)
        {
            if (isTransitioning || interactionGuard)
            {
                return;
            }

            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            T_TransitionState(_newState, cts.Token).Forget();
        }

        private async UniTask T_TransitionState(ERunState _newState, CancellationToken token)
        {
            isTransitioning = true;
            foundState = m_states.FirstOrDefault(c => c.stateType == _newState);
            
            if (foundState.IsNull())
            {
                Debug.LogError($"Doesn't contain definition for state: {_newState.ToString()}");
                return;
            }

            if (currentState.IsNull() || currentState.stateBehavior.IsNull())
            {
                return;
            }
            
            Debug.Log($"Exiting State: {currentState.stateType.ToString()}");
            
            await currentState.stateBehavior.ExitState(token);
            previousState = currentState;
            
            Debug.Log($"Previous State saved: {previousState.stateType.ToString()}");

            currentState = foundState;
            await currentState.stateBehavior.EnterState(token);
            
            foundState = null;
            Debug.Log($"Entered State: {currentState.stateType.ToString()}");
            isTransitioning = false;
        }

        public void ReturnToPreviousState()
        {
            if (previousState.IsNull())
            {
                return;
            }
            
            ChangeState(previousState.stateType);
        }

        public void ChangeInteractionGuard(bool isActive)
        {
            interactionGuard = isActive;
        }

        private void AssignValuesToCurrentState(params object[] _arguments)
        {
            currentState.stateBehavior.AssignArgument(_arguments);
        }

        public ERunState GetCurrentStateEnum()
        {
            return currentState.stateType;
        }

        #endregion
        
        
        
    }
}