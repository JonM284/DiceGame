using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Dice;
using Runtime.GameControllers;
using Runtime.Selection;
using UnityEngine;
using Utils;

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

        [SerializeField] private float m_calculationSpeed;
        
        #endregion
        
        #region Private Fields

        private List<BaseDie> m_rosterDice = new List<BaseDie>();
        //private List<BaseDie> m_selectedDice = new List<BaseDie>();
        
        private int m_selectedDiceCount, m_currentRollsAmount;

        private bool m_isAddingToSelection;

        private float m_timeBetweenSpins = 0.5f, m_timeSinceLastSpin;
        private float m_calculatedOutcome;

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
        
        private float m_mouseDownTime;
        private Plane m_dragPlane = new Plane(Vector3.up, Vector3.zero);
        private bool m_isDrag;
        private Vector3 m_dragStartPos;
        private float m_mouseInputThreshold = 0.25f;
        private SelectedDiceLocations m_currentSpace;

        #endregion

        #region Accessors

        public BaseDie currentDraggingDie { get; private set; }
        
        #endregion

        #region Unity Events

        private void OnEnable()
        {
            PlayableDice.onDieRollFinished += OnDieRollFinished;
            BaseDie.onDieSelected += OnDieSelected;
            BaseDie.onDieUnselected += OnDieUnSelected;
            BaseDie.onDieHovered += OnDieHovered;
            BaseDie.onDieUnhovered += OnDieUnHovered;
        }

        private void OnDisable()
        {
            PlayableDice.onDieRollFinished -= OnDieRollFinished;
            BaseDie.onDieSelected -= OnDieSelected;
            BaseDie.onDieUnselected -= OnDieUnSelected;
            BaseDie.onDieHovered -= OnDieHovered;
            BaseDie.onDieUnhovered -= OnDieUnHovered;
        }

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

        public void InitializeDice()
        {

            var _initialRosterDice = DiceGameController.Instance.GetRosterDiceData();

            for(int i = 0; i < _initialRosterDice.Count; i++)
            {
                var _dieData = DiceGameController.Instance.GetDieByGUID(_initialRosterDice[i].guid);
                var _newBaseDie = Instantiate(_dieData.diePrefab, m_rollableDiceSpaces[i].position, Quaternion.identity);
                _newBaseDie.Initialize();
                m_rosterDice.Add(_newBaseDie);
            }

            if (m_rosterDice.IsNull() || m_rosterDice.Count == 0)
            {
                Debug.Log("<color=red>Problem with Roster Dice</color>");
                return;
            }
            
            m_diceSelectors.ForEach(g => g.SetActive(true));

            m_currentRollsAmount = maxRollsAmount;

            m_currentState = DiceRollState.BEFORE_ROLL;
        }

        void OnDieRollFinished(int obj)
        {
            if (!m_rosterDice.TrueForAll(bd => !bd.isRolling))
            {
                return;
            }

            Debug.Log(m_rosterDice.Count);
            
            m_rosterDice.ForEach(bd =>
            {
                bd.SelectEffects(true);
                bd.EnablePhysics(false);
            });
            
            UpdateDiceLocations();
            
            m_currentState = DiceRollState.SELECTING;
        }
        
        /// <summary>
        /// Player Selects Die during selection phase of dice roll
        /// </summary>
        /// <param name="_selectedDie">Die to be added or subtracted</param>
        void OnDieSelected(BaseDie _selectedDie)
        {
            if (_selectedDie.IsNull())
            {
                return;
            }

            if (_selectedDie is PlayableDice)
            {
                SelectPlayableDie(_selectedDie);
            }
            
        }

        private void SelectPlayableDie(BaseDie _selectedDie)
        {
            /*if (m_currentState != DiceRollState.SELECTING)
            {
                return;
            }

            if (m_selectedDice.Contains(_selectedDie))
            {
                m_rosterDice.Add(_selectedDie);
                m_selectedDice.Remove(_selectedDie);
                UpdateDiceLocations();
                return;
            }

            if (m_selectedDice.Count >= maxSelectedDice)
            {
                return;
            }
            
            m_selectedDice.Add(_selectedDie);
            m_rosterDice.Remove(_selectedDie);

            UpdateSelectedDiceLocations();*/
        }

        [ContextMenu("Calculate outcome")]
        public void CalculateRolledOutcome()
        {
            if (m_selectedDiceSpaces.IsNull() || m_selectedDiceSpaces.Count == 0)
            {
                return;
            }

            m_calculatedOutcome = 1;

            m_currentState = DiceRollState.CALCULATING;

            T_RunCalculationSequence();

        }

        [ContextMenu("Roll All Dice")]
        public void RollDice()
        {
            if (m_rosterDice.IsNull() || m_rosterDice.Count == 0)
            {
                return;
            }

            if (m_currentState is DiceRollState.ROLLING or DiceRollState.CALCULATING)
            {
                return;
            }

            if (m_currentRollsAmount <= 0)
            {
                return;
            }
            
            m_rosterDice.ForEach(bd =>
            {
                bd.SelectEffects(false);
                bd.EnablePhysics(true);
                bd.DoAction();
            });

            m_currentState = DiceRollState.ROLLING;
            m_currentRollsAmount--;
        }

        private void UpdateDiceLocations()
        {
            UpdateRosterDiceLocations();
            UpdateSelectedDiceLocations();
        }
        
        private void UpdateRosterDiceLocations()
        {
            for (int i = 0; i < m_rosterDice.Count; i++)
            {
                if (VectorUtils.IsApprox(m_rosterDice[i].transform.position, m_rollableDiceSpaces[i].position))
                {
                    continue;
                }
                
                m_rosterDice[i].MoveDie(m_rollableDiceSpaces[i].position, 0.25f, false);
            }
        }
        
        private void UpdateSelectedDiceLocations()
        {

            /*for (int i = 0; i < m_selectedDice.Count; i++)
            {
                if (VectorUtils.IsApprox(m_selectedDice[i].transform.position, m_selectedDiceSpaces[i].m_positionRef.position))
                {
                    continue;
                }

                var _angle = Vector3.Angle(m_selectedDice[i].m_currentUpSide.associatedFace.forward,
                    m_mainCamera.transform.position - m_selectedDice[i].transform.position);
                
                Debug.Log($"Angle:{_angle}");
                
                m_selectedDice[i].MoveDie(m_selectedDiceSpaces[i].m_positionRef.position, 0.25f, true);
            }*/
            
        }

        private void ClearAllSelectedDice()
        {
            foreach (var _selectedDiceSpace in m_selectedDiceSpaces)
            {
                _selectedDiceSpace.m_lockedGO.SetActive(false);
                _selectedDiceSpace.m_lockedDie.SetDraggable(true);
                m_rosterDice.Add(_selectedDiceSpace.m_lockedDie);
                _selectedDiceSpace.m_lockedDie = null;
            }

            m_currentRollsAmount = maxRollsAmount;
            m_selectedDiceCount = 0;
            UpdateDiceLocations();
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
            UpdateRosterDiceLocations();
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

        #region Dice Interactions

        private void OnDieUnHovered(BaseDie _baseDie)
        {
            if (_baseDie.IsNull())
            {
                return;
            }
            
            _baseDie.HoverEffects(false);
        }

        private void OnDieHovered(BaseDie _baseDie)
        {
            if (_baseDie.IsNull())
            {
                return;
            }
            
            _baseDie.HoverEffects(true);
        }

        private void OnDieUnSelected(BaseDie _baseDie)
        {
            
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

        private async UniTask T_RunCalculationSequence()
        {
            
            await T_OnPlayModifiers();
            
            foreach (var _selectedDiceSpace in m_selectedDiceSpaces)
            {
                m_calculatedOutcome *= _selectedDiceSpace.m_lockedDie.dieValue;
                Debug.Log($"<color=orange>Outcome: {m_calculatedOutcome}</color>");
                _selectedDiceSpace.m_lockedDie.CalculationEffects();
                UIController.Instance.CreateFloatingTextAtPosition(m_calculatedOutcome.ToString(), Color.white,
                    _selectedDiceSpace.m_lockedDie.transform.position.FlattenVectorToY(_selectedDiceSpace.m_positionRef.position.y + 0.25f));

                await T_OnScoreModifier(_selectedDiceSpace.m_lockedDie);
                await UniTask.WaitForSeconds(m_calculationSpeed);
            }

            await T_OnHoldModifier();

            await T_OnPassiveModifier();
            
            onOutcomeCalculated?.Invoke(m_calculatedOutcome);
            
        }

        public void OnResetDiceAfterPlay()
        {
            ClearAllSelectedDice();

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

        public async UniTask T_CacheDice()
        {
            await UniTask.WaitForEndOfFrame();
            //Animate Dice go away
            //Cache dice 
        }
        
        
        #endregion

    }
}