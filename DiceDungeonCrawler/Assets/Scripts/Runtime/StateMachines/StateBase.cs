using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.Character.StateMachines
{
    [Serializable]
    public abstract class StateBase: MonoBehaviour
    {

        #region Public Fields

        public bool isObjectsAlwaysDisplayed, isCurveIn;
        public AnimationCurve animationCurve;
        public Ease easeType = Ease.Linear;
        public float moveDuration = 1f;
        public List<MoverGroups> movables = new List<MoverGroups>();

        #endregion

        #region Protected Fields

        protected StateManager stateManager;

        protected Player rwPlayerRef;

        protected Camera cameraRef;
        
        protected CancellationTokenSource cts;
        
        #endregion

        #region Accessors

        public bool isCompleted { get; protected set; }

        public bool isCancelled { get; protected set; }

        public bool isCurrentState { get; protected set; }

        public ERunState stateEnum { get; protected set; }

        public Player rwPlayer => CommonUtils.GetRequiredComponent(ref rwPlayerRef, () => stateManager.rwPlayer);

        public Camera mainCamera => CommonUtils.GetRequiredComponent(ref cameraRef, () => stateManager.mainCam);

        #endregion
        
        protected CancellationToken GetToken()
        {
            if (!cts.IsNull())
            {
                cts.Cancel();
            }
            
            cts = new CancellationTokenSource();
            
            return cts.Token;
        }

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
            ActivateObjects(isObjectsAlwaysDisplayed);
        }

        /// <summary>
        /// Called when state is changed to this state
        /// </summary>
        public virtual async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            isCurrentState = true;
            if (movables.Count == 0 || movables.IsNull())
            {
                return;
            }

            ActivateObjects(true);
            
            await MoveObjects(true, token);
        }
        
        /// <summary>
        /// Should be called after EnterState for Specific arguments
        /// </summary>
        /// <param name="_arguments">Arguments are passed as objects and casted when reaching the correct function</param>
        public abstract void AssignArgument(params object[] _arguments);

        /// <summary>
        /// Called before state is changed.
        /// </summary>
        public virtual async UniTask ExitState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            isCurrentState = true;

            if (movables.Count == 0 || movables.IsNull())
            {
                return;
            }
            
            await MoveObjects(false, token);

            if (isObjectsAlwaysDisplayed)
            {
                return;
            }

            ActivateObjects(false);
        }
        
        /// <summary>
        /// Used for inputs, and anything that needs to run a check each frame
        /// </summary>
        public abstract void UpdateState();

        public async UniTask SuspendState(bool isMoveIn, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (movables.Count == 0)
            {
                return;
            }
            
            await MoveObjects(isMoveIn, token);
        }

        protected void ActivateObjects(bool _isActive)
        {
            if (movables.IsNull() || movables.Count == 0)
            {
                return;
            }
            
            movables.ForEach(mg => mg.target.gameObject.SetActive(_isActive));
        }

        protected async UniTask MoveObjects(bool _isStart, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Sequence _moveSequence = DOTween.Sequence();
            float _timeOffset = 0.05f;
            float _currentTime = 0f;
            
            foreach (var _movable in movables)
            {
                
                if (_movable.IsNull() || _movable.target.IsNull() ||
                    _movable.onScreenTransform.IsNull() || _movable.offScreenTransform.IsNull() ||
                    _movable.onScreenTransform.position.IsNan() || _movable.offScreenTransform.position.IsNan())
                {
                    continue;
                }

                if (isCurveIn)
                {
                    _moveSequence.Insert(_currentTime,_movable.target.DOMove(_isStart ? 
                            _movable.onScreenTransform.position 
                            : _movable.offScreenTransform.position,
                        moveDuration).SetEase(animationCurve));

                    if (_movable.isRotate)
                    {
                        _moveSequence.Insert(_currentTime, _movable.target.DORotate(_isStart ?
                                _movable.onScreenTransform.rotation.eulerAngles :
                                _movable.offScreenTransform.rotation.eulerAngles,
                            moveDuration)).SetEase(animationCurve);
                    }
                }
                else
                {
                    _moveSequence.Insert(_currentTime,_movable.target.DOMove(_isStart ?
                            _movable.onScreenTransform.position 
                            : _movable.offScreenTransform.position,
                        moveDuration).SetEase(easeType));
                    
                    if (_movable.isRotate)
                    {
                        _moveSequence.Insert(_currentTime, _movable.target.DORotate(_isStart ?
                                _movable.onScreenTransform.rotation.eulerAngles :
                                _movable.offScreenTransform.rotation.eulerAngles,
                            moveDuration)).SetEase(animationCurve);
                    }
                }
                
                _currentTime += _timeOffset;
            }

            await _moveSequence.Play().AsyncWaitForCompletion();
        }


        public void SkipState()
        {
            stateManager.ChangeState(ERunState.MAP);
        }
        
    }
}