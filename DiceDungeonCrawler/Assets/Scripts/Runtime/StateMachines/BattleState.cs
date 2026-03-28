using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.RunStates;
using TMPro;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Battle State")]
    public class BattleState: StateBase
    {

        #region Serialized Fields

        [Header("Counter")]
        [SerializeField] private TMP_Text m_counterText;
        [SerializeField] private SpriteRenderer m_counterBackground, m_glow;
        [SerializeField] private Transform m_resultViewLocation, m_cameraDefaultLocation;
        [SerializeField] private float m_countFPS, m_textDuration;
        [SerializeField] private Color m_overkillColor;

        #endregion

        #region Private Fields

        private EMapLocationType m_battleType;

        private CancellationTokenSource cts;

        private float cameraMoveDuration = 0.35f;

        #endregion
        
        #region Accessors

        public float m_currentAmountToBeat { get; private set; }

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            LocalDiceController.onOutcomeCalculated += LocalDiceControllerOnOutcomeCalculated;
        }

        private void OnDisable()
        {
            LocalDiceController.onOutcomeCalculated -= LocalDiceControllerOnOutcomeCalculated;
        }

        #endregion
        
        #region Inherited Methods

        public override async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.EnterState(token);
            await LocalCameraController.Instance.MoveCameraToAsync(m_resultViewLocation.position, cameraMoveDuration, token);
            stateManager.ChangeInteractionGuard(true);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            var _modifer = (float)_arguments[0];
            m_battleType = (EMapLocationType)_arguments[1];
            m_currentAmountToBeat =
                EnemyController.Instance.GetBattleScoreByLevel(LocalMapController.Instance.GetCurrentLevel()) * _modifer;
            Debug.Log("Assign Arguement");
            Debug.Log(m_currentAmountToBeat);
            DisplayCounter(true);

            if (!cts.IsNull())
            {
                cts.Cancel();
            }
            
            cts = new CancellationTokenSource();
            EndIntroSequence(cts.Token).Forget();
        }

        public override async UniTask ExitState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            DisplayCounter(false);
            await base.ExitState(token);
        }


        public override void UpdateState(){}

        #endregion


        #region Class Implementation

        private async UniTask EndIntroSequence(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.WaitForSeconds(0.57f);
            await LocalCameraController.Instance.MoveCameraToAsync(m_cameraDefaultLocation.position, cameraMoveDuration, token);
            await LocalDiceController.Instance.InitializeDice(token);
            stateManager.ChangeInteractionGuard(false);
        }

        //Move to battle state manager
        private void DisplayCounter(bool isDisplay)
        {
            m_counterText.text = m_currentAmountToBeat.ToString(); 
            m_counterText.color = Color.black;
            m_counterBackground.color = isDisplay ? Color.white : Color.black;
            m_glow.color = isDisplay ? Color.white : Color.black;
        }
        
        
        public async UniTask T_ShowResults(float _calculatedAmount, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            stateManager.ChangeInteractionGuard(true);

            await LocalCameraController.Instance.MoveCameraToAsync(m_resultViewLocation.position, cameraMoveDuration, token);

            await UniTask.WaitForSeconds(0.1f);

            await T_CountToNumber(m_currentAmountToBeat - _calculatedAmount, token);

            await LocalCameraController.Instance.MoveCameraToAsync(m_cameraDefaultLocation.position,cameraMoveDuration, token);

            await UniTask.WaitForSeconds(0.1f);
            
            stateManager.ChangeInteractionGuard(false);

            if (m_currentAmountToBeat <= 0)
            {
                //Player wins battle
                //1. Remove battle area
                //2. Rewards
                //3. Return to Map
                await LocalDiceController.Instance.ResetDiceAfterBattle(token);
                stateManager.ChangeState(m_battleType == EMapLocationType.E_BATTLE ? ERunState.MAP : ERunState.REWARD);
                return;
            }

            if (LocalDiceController.Instance.amountOfTries > 0)
            {
                //Player didn't win battle, has tries left
                await LocalDiceController.Instance.OnResetDiceAfterPlay(token);
                return;
            }
            
            //LOSE CONDITION REACHED
            //1. Go to map
            //2. Subtract lives or restart run.
            await LocalDiceController.Instance.ResetDiceAfterBattle(token);
            stateManager.ChangeState(ERunState.LOSE);
        }

        private void LocalDiceControllerOnOutcomeCalculated(float _calculatedOutcome)
        {
            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            T_ShowResults(_calculatedOutcome, cts.Token).Forget();
        }
        
        private async UniTask T_CountToNumber(float _newValue, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            float _previousValue = m_currentAmountToBeat;
            int _stepAmount;
            float _waitTime = 1 / m_countFPS;

            _stepAmount = _newValue - _previousValue  < 0 ? 
                Mathf.FloorToInt((_newValue - _previousValue) / (m_countFPS * m_textDuration)) 
                : Mathf.CeilToInt((_newValue - _previousValue) / (m_countFPS * m_textDuration));

            _stepAmount = Mathf.Abs(_stepAmount);
            
            //Going up
            if (_previousValue < _newValue)
            {
                while (_previousValue < _newValue)
                {
                    _previousValue += _stepAmount;
                    if (_previousValue > _newValue)
                    {
                        _previousValue = _newValue;
                    }

                    if (_previousValue < 0)
                    {
                        m_counterText.color = Color.black;
                        m_counterBackground.color = m_overkillColor;
                        m_glow.color = m_overkillColor;
                    }
                    
                    m_counterText.text = _previousValue.ToString();
                    await UniTask.WaitForSeconds(_waitTime);
                }
            }
            else //Going down
            {
                while (_previousValue > _newValue)
                {
                    _previousValue -= _stepAmount;
                    if (_previousValue < _newValue)
                    {
                        _previousValue = _newValue;
                    }

                    if (_previousValue < 0)
                    {
                        m_counterText.color = Color.black;
                        m_counterBackground.color = m_overkillColor;
                        m_glow.color = m_overkillColor;
                    }
                    
                    m_counterText.text = _previousValue.ToString();
                    await UniTask.WaitForSeconds(_waitTime);
                }
            }

            m_currentAmountToBeat = _newValue;
        }


        #endregion
        
    }
}