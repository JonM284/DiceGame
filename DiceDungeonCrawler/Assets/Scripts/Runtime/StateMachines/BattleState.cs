using System;
using Cysharp.Threading.Tasks;
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

        public override async UniTask EnterState()
        {
            await base.EnterState();
            LocalDiceController.Instance.InitializeDice();
        }

        public override void AssignArgument(params object[] _arguments)
        {
            var _modifer = (float)_arguments[0];
            m_battleType = (EMapLocationType)_arguments[1];
            m_currentAmountToBeat =
                EnemyController.Instance.GetBattleScoreByLevel(LocalMapController.Instance.GetCurrentLevel()) * _modifer;
            Debug.Log("Assign Arguement");
            Debug.Log(m_currentAmountToBeat);
            SetupCounter();
        }

        public override async UniTask ExitState()
        {
            await base.ExitState();
        }
        

        #endregion


        #region Class Implementation

        //Move to battle state manager
        public void SetupCounter()
        {
            m_counterText.text = m_currentAmountToBeat.ToString(); 
            m_counterText.color = Color.black;
            m_counterBackground.color = Color.white;
            m_glow.color = Color.white;
        }
        
        
        public async UniTask T_ShowResults(float _calculatedAmount)
        {

            LocalCameraController.Instance.MoveCameraTo(m_resultViewLocation.position, 0.2f);

            await UniTask.WaitForSeconds(0.2f);

            await T_CountToNumber(m_currentAmountToBeat - _calculatedAmount);

            LocalCameraController.Instance.MoveCameraTo(m_cameraDefaultLocation.position, 0.2f);

            await UniTask.WaitForSeconds(0.2f);

            if (m_currentAmountToBeat <= 0)
            {
                //Player beat Enemy
                //1. Remove battle area
                //2. Rewards
                //3. Return to Map
                LocalDiceController.Instance.DisplayDice(false);
                stateManager.ChangeState(m_battleType == EMapLocationType.E_BATTLE ? ERunState.MAP : ERunState.REWARD);
                return;
            }

            if (LocalDiceController.Instance.amountOfTries > 0)
            {
                LocalDiceController.Instance.OnResetDiceAfterPlay();
                return;
            }
            
            //LOSE CONDITION REACHED
            //1. Go to map
            //2. Subtract lives or restart run.
            LocalDiceController.Instance.DisplayDice(false);
            stateManager.ChangeState(ERunState.LOSE);
        }

        private void LocalDiceControllerOnOutcomeCalculated(float _calculatedOutcome)
        {
            T_ShowResults(_calculatedOutcome);
        }
        
        private async UniTask T_CountToNumber(float _newValue)
        {
            
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