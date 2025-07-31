using System;
using Cysharp.Threading.Tasks;
using Runtime.Character.StateMachines;
using Runtime.GameControllers;
using Runtime.Gameplay;
using TMPro;
using UnityEngine;

namespace Runtime.StateMachines
{
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

        #region Accessors

        public float m_currentAmountToBeat { get; private set; }

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            LocalDiceController.onOutcomeCalculated += LocalDiceControllerOnonOutcomeCalculated;
        }

        private void OnDisable()
        {
            LocalDiceController.onOutcomeCalculated -= LocalDiceControllerOnonOutcomeCalculated;
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
            m_currentAmountToBeat = (float)_arguments[1];
        }

        public override void SetupState()
        {
            
        }

        public override async UniTask ExitState()
        {
            await base.ExitState();
        }
        

        #endregion


        #region Class Implementation

        //Move to battle state manager
        public void OnEnterBattleState()
        {
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

        }

        private void LocalDiceControllerOnonOutcomeCalculated(float _calculatedOutcome)
        {
            T_ShowResults(_calculatedOutcome);
        }
        
        private async UniTask T_CountToNumber(float _newValue)
        {
            
            Debug.Log(_newValue);
            float _previousValue = m_currentAmountToBeat;
            int _stepAmount;
            float _waitTime = 1 / m_countFPS;

            _stepAmount = _newValue - _previousValue  < 0 ? 
                Mathf.FloorToInt((_newValue - _previousValue) / (m_countFPS * m_textDuration)) 
                : Mathf.CeilToInt((_newValue - _previousValue) / (m_countFPS * m_textDuration));

            _stepAmount = Mathf.Abs(_stepAmount);
            
            Debug.Log(_stepAmount);

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
            else
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