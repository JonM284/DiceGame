using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Runtime.Character.StateMachines;
using Runtime.GameControllers;
using Runtime.RunStates;
using TMPro;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class LocalRunController: MonoBehaviour
    {

        #region Serialized Fields

        [Header("Connected Controllers")]
        [SerializeField] private LocalDiceController m_diceController;
        [SerializeField] private StateManager m_runStateManager;
        
        [Header("Other Locals")]
        [SerializeField] private Transform cameraParent;
        [SerializeField] private GameObject m_startingGO;
        [SerializeField] private Transform m_cameraDefaultLocation;
        
        #endregion

        #region Private Fields

        private float m_amountToBeat;

        private bool m_isInventoryOpen, m_isModifiersOpen;

        #endregion

        #region Class Implementation

        public void StartRun()
        {
            m_startingGO.SetActive(false);
            m_runStateManager.InitStateMachine(ERunState.MAP);
        }

        public void OpenInventory()
        {
            if (m_runStateManager.isTransitioning)
            {
                return;
            }

            if (m_runStateManager.currentState.stateType != ERunState.MAP 
            && m_runStateManager.currentState.stateType != ERunState.INVENTORY)
            {
                return;
            }
            
            m_isInventoryOpen = !m_isInventoryOpen;
            
            if (!m_isInventoryOpen)
            {
                m_runStateManager.ChangeState(ERunState.INVENTORY);
            }
            else
            {
                m_runStateManager.ReturnToPreviousState();
            }
        }

        public void OpenModifiers()
        {
            if (m_runStateManager.isTransitioning)
            {
                return;
            }

            if (m_runStateManager.currentState.stateType == ERunState.REWARD ||
                m_runStateManager.currentState.stateType == ERunState.LOSE)
            {
                return;
            }
            
            m_isModifiersOpen = !m_isModifiersOpen;
            
            if (!m_isModifiersOpen)
            {
                m_runStateManager.ChangeState(ERunState.MODIFIER_SWAP);
            }
            else
            {
                m_runStateManager.ReturnToPreviousState();
            }
        }
        
        #endregion


    }
}