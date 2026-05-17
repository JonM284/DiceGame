using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.DataSaving;
using Data.Dice;
using DG.Tweening;
using NUnit.Framework;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Dice;
using Runtime.Dice.Enums;
using Runtime.GameControllers;
using Runtime.Selection;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    public class LocalDiceController: MonoBehaviour
    {

        #region Nested Classes

        [Serializable]
        public class SelectedDiceLocations
        {
            public BaseDie m_lockedDie;
            public Transform m_positionRef;
            public GameObject m_highlightGO, m_lockedGO;
        }

        #endregion

        #region Actions

        public static event Action<float> onOutcomeCalculated; 

        #endregion

        #region Instance

        public static LocalDiceController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private LocalRunController m_localRunController;
        [SerializeField] private Camera m_mainCamera;
        [SerializeField] private int maxSelectedDice = 3, maxRollsAmount = 3;
        [SerializeField] private Transform m_dicePositionThreshold;
        [SerializeField] private List<Transform> m_rollableDiceSpaces = new List<Transform>();
        [SerializeField] private List<SelectedDiceLocations> m_selectedDiceSpaces = new List<SelectedDiceLocations>();
        [SerializeField] private float m_dragBaseYPosition, m_xPosThreshold;
        [SerializeField] private List<GameObject> m_diceSelectors = new List<GameObject>();
        [SerializeField] private LayerMask selectableLayers;
        [SerializeField] private Transform m_diceBagLoc;

        [SerializeField] private float m_calculationSpeed;
        
        #endregion
        
        #region Private Fields

        private List<BaseDie> m_rosterDice = new List<BaseDie>();
        
        private int m_selectedDiceCount, m_currentRollsAmount;

        private bool m_isAddingToSelection, isFreeRoll;

        private float m_timeBetweenSpins = 0.5f, m_timeSinceLastSpin;
        private long m_calculatedOutcome;

        private enum DiceRollState
        {
            BEFORE_ROLL,
            ROLLING,
            SELECTING,
            CALCULATING,
        }

        private DiceRollState m_currentState;
        
        private Player m_player;

        private List<ModifierDice> m_modifiers = new List<ModifierDice>();

        private List<DieWrapperData> savedRosterDice, savedInventoryDice;
        private List<ModDieWrapperData> savedModifierDice;
        
        private float m_mouseDownTime;
        private Plane m_dragPlane = new Plane(Vector3.up, Vector3.zero);
        private bool m_isDrag;
        private Vector3 m_dragStartPos;
        private float m_mouseInputThreshold = 0.25f;
        private SelectedDiceLocations m_currentSpace;

        private int m_maxAmountOfTries = 3;
        private float currentEndValueModValue;

        private CancellationTokenSource cts;

        #endregion

        #region Accessors

        public BaseDie currentDraggingDie { get; private set; }

        public int amountOfTries { get; private set; }

        #endregion

        #region Unity Events

        private void Start()
        {
            m_player = ReInput.players.GetPlayer(0);
            
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
        }
        
        #endregion

        #region Unity Events

        private void Update()
        {
            if (m_currentState != DiceRollState.SELECTING)
            {
                return;
            }
            
            //Check drags
            CheckDrag();

        }

        #endregion

        #region Class Implementation

        private void CreateDice()
        {
            var _initialRosterDice = DiceGameController.Instance.GetRosterDiceData();

            foreach (var savedDie in _initialRosterDice)
            {
                var _dieData = DiceGameController.Instance.GetDieByAmountOfSides(savedDie.faceValues.Count);
                var _newBaseDie = Instantiate(_dieData.GetUsableItem(), m_diceBagLoc.position, Quaternion.identity);

                if (!_newBaseDie.TryGetComponent(out PlayableDice playableDice))
                {
                    Debug.LogError("[Die Creation Error] Created Die does NOT have playable Dice Behaviour attached");
                    return;
                }
                
                playableDice.Initialize();
                m_rosterDice.Add(playableDice);
            }
        }
        
        
        public async UniTask InitializeDice(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (m_rosterDice.IsNull() || m_rosterDice.Count == 0)
            {
                CreateDice();
            }
            
            await DisplayDice(true, token);
            
            m_diceSelectors.ForEach(g => g.SetActive(true));

            m_currentRollsAmount = maxRollsAmount;
            
            amountOfTries = m_maxAmountOfTries;
            
            m_currentState = DiceRollState.BEFORE_ROLL;
        }

        public async UniTask DisplayDice(bool isDisplay, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (m_rosterDice.Count == 0)
            {
                CreateDice();
            }
            
            try
            {
                var tasks = new List<UniTask>();

                for (int i = 0; i < m_rosterDice.Count; i++)
                {
                    tasks.Add(m_rosterDice[i].MoveDieAsync(isDisplay
                        ? m_rollableDiceSpaces[i].position
                        : m_diceBagLoc.position, 0.25f, false, token));
                }

                await tasks;
            }
            catch
            {
                Debug.LogError("[ERROR] Display Dice token cancelled");
                for (int i = 0; i < m_rosterDice.Count; i++)
                {
                    m_rosterDice[i].transform.position = isDisplay
                        ? m_rollableDiceSpaces[i].position
                        : m_diceBagLoc.position;
                }
            }
        }

        [ContextMenu("Roll All Dice")]
        public void RollDice()
        {
            if (m_rosterDice.IsNull() || m_rosterDice.Count == 0)
            {
                Debug.Log("Roster Dice null or empty");
                return;
            }

            if (m_currentState is DiceRollState.ROLLING or DiceRollState.CALCULATING)
            {
                Debug.Log($"Current Rolling State: {m_currentState.ToString()}");
                return;
            }

            if (m_currentRollsAmount <= 0 && !isFreeRoll)
            {
                Debug.Log($"Rolls amount <= 0 and not a free roll");
                return;
            }

            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            RollDiceAsync(cts.Token).Forget();

            m_currentState = DiceRollState.ROLLING;

            if (!isFreeRoll)
            {
                m_currentRollsAmount--;
            }
            
            isFreeRoll = false;
        }

        private async UniTask RollDiceAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var rollTasks = new List<UniTask>();
            m_rosterDice.ForEach(bd =>
            {
                bd.SelectEffects(false);
                bd.EnablePhysics(true);
                rollTasks.Add(bd.DoActionAsync(token));
            });

            await rollTasks;
            
            m_rosterDice.ForEach(bd =>
            {
                bd.SelectEffects(false);
                bd.EnablePhysics(false);
            });
            
            await UpdateDiceLocationsAsync(token);
            
            m_currentState = DiceRollState.SELECTING;
            
        }

        [ContextMenu("Calculate outcome")]
        public void CalculateRolledOutcome()
        {
            if (m_selectedDiceSpaces.IsNull() || m_selectedDiceSpaces.Count == 0)
            {
                return;
            }

            if (m_selectedDiceSpaces.TrueForAll(sdl => sdl.m_lockedDie.IsNull()))
            {
                Debug.Log("<color=red>No Location filled</color>");
                return;
            }
            
            if (amountOfTries <= 0)
            {
                return;
            }

            if (m_currentState == DiceRollState.CALCULATING)
            {
                return;
            }
            
            amountOfTries--;
            m_calculatedOutcome = 1;
            
            m_currentState = DiceRollState.CALCULATING;

            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            CalculationSequenceAsync(cts.Token).Forget();
        }

        
        private async UniTask UpdateDiceLocationsAsync(CancellationToken token)
        {
            await UpdateRosterDiceLocationsAsync(token);
        }
        
        private async UniTask UpdateRosterDiceLocationsAsync(CancellationToken token)
        {
            var tasks = new List<UniTask>();
            
            for (int i = 0; i < m_rosterDice.Count; i++)
            {
                if (VectorUtils.IsApprox(m_rosterDice[i].transform.position, m_rollableDiceSpaces[i].position))
                {
                    continue;
                }
                
                tasks.Add(m_rosterDice[i].MoveDieAsync(m_rollableDiceSpaces[i].position, 0.25f, false, token));
            }

            await tasks;
        }
        
        public async UniTask ResetDiceAfterBattle(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            ResetPlayedDice();
            
            await DisplayDice(false, token);
            isFreeRoll = true;
            m_selectedDiceCount = 0;
        }

        /// <summary>
        /// Reset Dice in Lists not physically
        /// </summary>
        private void ResetPlayedDice()
        {
            foreach (var _selectedDiceSpace in m_selectedDiceSpaces)
            {
                _selectedDiceSpace.m_lockedGO.SetActive(false);
                
                if(_selectedDiceSpace.m_lockedDie.IsNull()) continue;
                
                _selectedDiceSpace.m_lockedDie.SetDraggable(true);
                m_rosterDice.Add(_selectedDiceSpace.m_lockedDie);
                _selectedDiceSpace.m_lockedDie = null;
            }
        }
        
        private async UniTask ClearAllSelectedDiceAsync(CancellationToken token)
        {
            ResetPlayedDice();
            await UpdateDiceLocationsAsync(token);
        }

        private void CheckDrag()
        {
            if (m_player.GetButtonDown("Confirm"))
            {
                CheckMouseDown();
            }
            
            if (m_player.GetButtonUp("Confirm"))
            {
                CheckMouseUp();
            }

            if (m_player.GetButton("Confirm"))
            {
                UpdateDraggablePosition();
            }
        }
        
        #endregion
        
        #region Draggable Interactions

        private void CheckMouseDown()
        {
            if (m_selectedDiceCount >= maxSelectedDice)
            {
                return;
            }

            if (m_currentState != DiceRollState.SELECTING)
            {
                return;
            }
            
            if (!Physics.Raycast(m_mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, selectableLayers))
            {
                return;
            }

            var _die = hit.collider.GetComponent<BaseDie>();

            if (_die.IsNull())
            {
                return;
            }

            if (!_die.canBeDragged)
            {
                return;
            }

            currentDraggingDie = _die;
            currentDraggingDie.OnBeginDrag(GetWorldPosOf(currentDraggingDie.transform));
        }

        private void CheckMouseUp()
        {
            if (currentDraggingDie.IsNull())
            {
                return;
            }
            
            AddDraggedDieToSelected(); 
            
            currentDraggingDie.OnEndDrag(m_isAddingToSelection && !m_currentSpace.IsNull() ? m_currentSpace.m_positionRef.position
                : currentDraggingDie.savedReturnLocation);
            
            m_currentSpace = null;
            currentDraggingDie = null;
            m_isAddingToSelection = false;
        }

        private void UpdateDraggablePosition()
        {
            if (currentDraggingDie.IsNull())
            {
                return;
            }

            CheckDieLocation();
            
            currentDraggingDie.OnUpdateDragPosition(GetWorldPosOf(currentDraggingDie.transform));
        }

        private Vector3 GetWorldPosOf(Transform _objectPos)
        {
            var _screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                m_mainCamera.WorldToScreenPoint(_objectPos.position).z);
            var _worldPos = m_mainCamera.ScreenToWorldPoint(_screenPos);
            return new Vector3(_worldPos.x, m_dragBaseYPosition, _worldPos.z);
        }

        private void CheckDieLocation()
        {
            if (currentDraggingDie.IsNull())
            {
                return;
            }

            if (currentDraggingDie.transform.position.z < m_dicePositionThreshold.position.z
                || currentDraggingDie.transform.position.x < -m_dicePositionThreshold.position.x 
                || currentDraggingDie.transform.position.x > m_dicePositionThreshold.position.x)
            {
                CheckOutsideThreshold();
                return;
            }
            
            m_isAddingToSelection = true;

            for (int i = 0; i < m_selectedDiceSpaces.Count; i++)
            {
                if (!m_selectedDiceSpaces[i].m_lockedDie.IsNull())
                {
                    continue;
                }

                if (currentDraggingDie.transform.position.x < m_selectedDiceSpaces[i].m_positionRef.position.x - m_xPosThreshold
                    || currentDraggingDie.transform.position.x > m_selectedDiceSpaces[i].m_positionRef.position.x + m_xPosThreshold)
                {
                    if (m_selectedDiceSpaces[i].m_highlightGO.activeSelf)
                    {
                        m_selectedDiceSpaces[i].m_highlightGO.SetActive(false);
                    }
                    
                    continue;
                }

                m_selectedDiceSpaces[i].m_highlightGO.SetActive(true);
                m_currentSpace = m_selectedDiceSpaces[i];
            }
            
        }

        private void AddDraggedDieToSelected()
        {
            if (currentDraggingDie.IsNull() || m_currentSpace.IsNull())
            {
                return;
            }
            
            foreach (var _die in m_rosterDice)
            {
                if (_die != currentDraggingDie)
                {
                    continue;
                }

                m_currentSpace.m_lockedDie = _die;
                m_currentSpace.m_highlightGO.SetActive(false);
                m_currentSpace.m_lockedGO.SetActive(true);
            }

            currentDraggingDie.SetDraggable(false);
            m_rosterDice.Remove(currentDraggingDie);
            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            UpdateRosterDiceLocationsAsync(cts.Token).Forget();
            m_selectedDiceCount++;
        }

        private void CheckOutsideThreshold()
        {
            if (!m_isAddingToSelection)
            {
                return;
            }
            
            Debug.Log("Resetting spaces");
            m_selectedDiceSpaces.ForEach(sdl => sdl.m_highlightGO.SetActive(false));
            m_isAddingToSelection = false;
            m_currentSpace = null;
        }
        
        
        #endregion
        
        #region Calculations
        
        /// <summary>
        /// Sequence:
        /// INDIPENDENT = 0, //default -> useless ?
        /// ON_PLAYED = 1, //When dice are played
        /// ON_SCORED = 2, //When dice are scored
        /// ON_HELD = 3, //When dice are held
        /// ON_OTHER_MODIFIER_DIE = 4, //When other dice are activated
        /// PASSIVE = 5, //passive
        /// </summary>

        private async UniTask CalculationSequenceAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            //ToDo: Need to Check Masks during this process

            currentEndValueModValue = 0;
            
            await T_OnPlayModifiers();

            for (int i = 0; i < m_selectedDiceSpaces.Count; i++)
            {
                if (m_selectedDiceSpaces[i].m_lockedDie is not PlayableDice _currentDie)
                {
                    continue;
                }
                
                var _currentCalculatedDieValue = _currentDie.dieValue;

                if (_currentDie.TintType != TintType.NONE)
                {
                    _currentCalculatedDieValue = await ProcessDieTintAsync(i, _currentCalculatedDieValue, _currentDie.TintType, token);
                }
                
                m_calculatedOutcome *= _currentCalculatedDieValue;
                
                m_selectedDiceSpaces[i].m_lockedDie.CalculationEffects();
                
                //ToDo: get rid of this or make it look nice
                UIController.Instance.CreateFloatingTextAtPosition(_currentCalculatedDieValue.ToString(), Color.white,
                    m_selectedDiceSpaces[i].m_lockedDie.transform.position.FlattenVectorToY(
                        m_selectedDiceSpaces[i].m_positionRef.position.y + 0.25f));

                await T_OnScoreModifier(m_selectedDiceSpaces[i].m_lockedDie);
                await UniTask.WaitForSeconds(m_calculationSpeed, cancellationToken: token);
            }

            await T_OnHoldModifier();

            await T_OnPassiveModifier();

            if (currentEndValueModValue == 0)
            {
                currentEndValueModValue = 1;
            }
            
            m_calculatedOutcome = Convert.ToInt64(Math.Round(m_calculatedOutcome * currentEndValueModValue));
            
            if (Math.Abs(currentEndValueModValue) > 1)
            {
                UIController.Instance.CreateFloatingTextAtPosition($"x {currentEndValueModValue}", Color.white,
                    m_selectedDiceSpaces[1].m_lockedDie.transform.position.FlattenVectorToY(
                        m_selectedDiceSpaces[1].m_positionRef.position.y + 0.25f));
                
                await UniTask.WaitForSeconds(m_calculationSpeed, cancellationToken: token);
            }
            
            UIController.Instance.CreateFloatingTextAtPosition($"Total: {m_calculatedOutcome}", Color.white,
                m_selectedDiceSpaces[1].m_lockedDie.transform.position.FlattenVectorToY(
                    m_selectedDiceSpaces[1].m_positionRef.position.y + 0.25f));
                
            await UniTask.WaitForSeconds(m_calculationSpeed, cancellationToken: token);
            
            onOutcomeCalculated?.Invoke(m_calculatedOutcome);
        }

        public async UniTask OnResetDiceAfterPlay(CancellationToken token)
        {
            await ClearAllSelectedDiceAsync(token);

            isFreeRoll = true;
            m_selectedDiceCount = 0;
            
            m_currentState = DiceRollState.BEFORE_ROLL;
        }

        private async UniTask T_OnPlayModifiers()
        {
            
        }

        private async UniTask T_OnScoreModifier(BaseDie _baseDie)
        {
            
        }

        private async UniTask T_OnHoldModifier()
        {
            foreach (var _rosterDie in m_rosterDice)
            {
                

                await UniTask.WaitForSeconds(m_calculationSpeed);
            }
        }

        private async UniTask T_OnPassiveModifier()
        {
            
            foreach (var _modifierDie in m_modifiers)
            {
                if (_modifierDie)
                {
                    
                }
                
                await UniTask.WaitForSeconds(m_calculationSpeed);
            }
            
        }

        private async UniTask<int> ProcessDieTintAsync(int index, int dieValue, TintType tintType, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            float dieValueProxy = dieValue;
            
            switch (tintType)
            {
                case TintType.YELLOW:
                    dieValueProxy *= IsRandomChancePassed(0.25f) ? 6f : 1f;
                    break;
                case TintType.BLUE:
                    dieValueProxy *= 2;
                    break;
                case TintType.RED:
                    dieValueProxy -= 1;
                    currentEndValueModValue += 2f;
                    break;
            }

            return Mathf.RoundToInt(dieValueProxy);
        } 

        public async UniTask T_CacheDice()
        {
            await UniTask.WaitForEndOfFrame();
            //Animate Dice go away
            //Cache dice 
        }

        private bool IsRandomChancePassed(float _chanceAmount)
        {
            return Random.Range(0.0f, 1.0f) <= _chanceAmount;
        }
        
        
        #endregion
        
    }
}