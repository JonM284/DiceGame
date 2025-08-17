using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.Character.StateMachines
{
    public class StateManager: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField]
        protected List<StateListItem> m_states = new List<StateListItem>();

        #endregion

        #region Protected Fields

        protected StateListItem m_foundState, m_previousState, m_currentSubState;
        protected bool m_isRunning;
        
        #endregion

        #region Accessors
        
        public StateListItem currentState { get; private set; }

        public bool isTransitioning { get; private set; }

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

        #endregion

        #region Class Implementation

        private void OnLocationSelected(MapLocationAction mapLocationAction)
        {
            if (mapLocationAction.IsNull())
            {
                return;
            }

            T_OnLocationSelected(mapLocationAction);
        }

        private async UniTask T_OnLocationSelected(MapLocationAction mapLocationAction)
        {
            LocalMapController.Instance.IncreaseCurrentMapLevel();
            
            //Level Point Visuals + moving player piece
            await LocalMapController.Instance.T_PointSelectedAsync(mapLocationAction);

            await UniTask.WaitForSeconds(0.5f);
            
            //After Everything Switch states and assign arguments
            switch (mapLocationAction.locationType)
            {
                case EMapLocationType.E_BATTLE: case EMapLocationType.M_BATTLE: case EMapLocationType.H_BATTLE:
                    await T_TransitionState(ERunState.BATTLE);
                    object[] arg = {EnemyController.Instance.GetBattleMod(mapLocationAction.locationType), mapLocationAction.locationType};
                    AssignValuesToCurrentState(arg);
                    break;
                case EMapLocationType.TINT:
                    await T_TransitionState(ERunState.TINT);
                    break;
                case EMapLocationType.SHOP:
                    await T_TransitionState(ERunState.SHOP);
                    break;
                case EMapLocationType.ITEM:
                    await T_TransitionState(ERunState.ITEM_DROP);
                    break;
            }
        }
        
        public async UniTask InitStateMachine(ERunState _startingState)
        {

            m_foundState = m_states.FirstOrDefault(sli => sli.stateType == _startingState);

            if (m_foundState.IsNull())
            {
                return;
            }
            
            foreach (var _state in m_states)
            {
                _state.stateBehavior.InitState(this, _state.stateType);
                await UniTask.WaitForEndOfFrame();
            }
            
            currentState = m_foundState;
            currentState.stateBehavior?.EnterState();

            m_foundState = null;
            m_isRunning = true;
        }

        public void UninitStateMachine()
        {
            m_isRunning = false;
            currentState.stateBehavior.ExitState();
            currentState = null;
        }
        
        /*public void AddState(ECharacterStates _state, StateBase _stateBehavior)
        {
            if (m_states.Contains(_state))
            {
                Debug.LogError($"Already contains state: {_state.ToString()}");
                return;
            }

            if (_stateBehavior.IsNull())
            {
                return;
            }
            
            m_states.Add(_state, _stateBehavior);
        }*/
        
        public void ChangeState(ERunState _newState)
        {
            T_TransitionState(_newState);
        }

        private async UniTask T_TransitionState(ERunState _newState)
        {
            isTransitioning = true;
            m_foundState = m_states.FirstOrDefault(c => c.stateType == _newState);
            
            if (m_foundState.IsNull())
            {
                Debug.LogError($"Doesn't contain definition for state: {_newState.ToString()}");
                return;
            }

            if (currentState.IsNull() || currentState.stateBehavior.IsNull())
            {
                return;
            }
            
            Debug.Log($"Exiting State: {currentState.stateType.ToString()}");
            await currentState.stateBehavior.ExitState();
            m_previousState = currentState;
            Debug.Log($"Previous State saved: {m_previousState.stateType.ToString()}");
            currentState = m_foundState;
            await currentState.stateBehavior.EnterState();
            m_foundState = null;
            Debug.Log($"Entered State: {currentState.stateType.ToString()}");
            isTransitioning = false;
        }

        public void ReturnToPreviousState()
        {
            if (m_previousState.IsNull())
            {
                return;
            }
            
            ChangeState(m_previousState.stateType);
        }

        private void AssignValuesToCurrentState(params object[] _arguments)
        {
            currentState.stateBehavior.AssignArgument(_arguments);
        }


        /*public StateBase GetState(ECharacterStates _states)
        {
            if (!m_states.ContainsKey(_states))
            {
                Debug.LogError($"Doesn't contain definition for state: {_states.ToString()}");
                return default;
            }

            m_states.TryGetValue(_states, out m_foundState);

            return m_foundState;
        }*/

        public ERunState GetCurrentStateEnum()
        {
            return currentState.stateType;
        }

        #endregion
        
        
        
    }
}