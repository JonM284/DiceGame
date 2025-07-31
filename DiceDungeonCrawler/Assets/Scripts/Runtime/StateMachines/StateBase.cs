using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.Character.StateMachines
{
    [Serializable]
    public abstract class StateBase: MonoBehaviour
    {

        public Ease easeType = Ease.Linear;
        public float moveDuration = 1f;
        public List<MoverGroups> movables = new List<MoverGroups>();
        
        public bool isCompleted { get; protected set; }
        protected float startTime;
        protected float currentTime => Time.time - startTime;
        protected StateManager stateManager;

        public ERunState stateEnum { get; protected set; }

        /// <summary>
        /// When character and stateMachine are initialized. Get All necessary managers for the state at this time
        /// </summary>
        /// <param name="_manager">Inject Manager</param>
        public virtual void InitState(StateManager _manager, ERunState _stateEnum)
        {
            if (!_manager.IsNull())
            {
                stateManager = _manager;
            }

            stateEnum = _stateEnum;
        }

        /// <summary>
        /// Called when state is changed to this state
        /// </summary>
        public virtual async UniTask EnterState()
        {
            if (movables.Count == 0 || movables.IsNull())
            {
                return;
            }

            ActivateObjects(true);
            
            await MoveObjects(true);
        }
        
        /// <summary>
        /// Should be called after EnterState for Specific arguments
        /// </summary>
        /// <param name="_arguments">Arguments are passed as objects and casted when reaching the correct function</param>
        public abstract void AssignArgument(params object[] _arguments);
        
        /// <summary>
        /// Update this state every frame. NOTE: currently not necessary
        /// </summary>
        public abstract void SetupState();

        /// <summary>
        /// Called before state is changed.
        /// </summary>
        public virtual async UniTask ExitState()
        {
            if (movables.Count == 0 || movables.IsNull())
            {
                return;
            }
            
            await MoveObjects(false);

            ActivateObjects(false);
        }

        protected void ActivateObjects(bool _isActive)
        {
            movables.ForEach(mg => mg.target.gameObject.SetActive(_isActive));
        }

        protected async UniTask MoveObjects(bool _isStart)
        {
            Sequence _moveSequence = DOTween.Sequence();
            float _timeOffset = 0.05f;
            float _currentTime = 0f;
            
            foreach (var _movable in movables)
            {
                
                if (_movable.IsNull() || _movable.target.IsNull() ||
                    _movable.onScreenPosition.IsNan() || _movable.offScreenPosition.IsNan())
                {
                    continue;
                }
                
                _moveSequence.Insert(_currentTime,_movable.target.DOMove(_isStart ? _movable.onScreenPosition : _movable.offScreenPosition,
                    moveDuration).SetEase(easeType));
                _currentTime += _timeOffset;
            }

            await _moveSequence.Play().AsyncWaitForCompletion();
        }
        
    }
}