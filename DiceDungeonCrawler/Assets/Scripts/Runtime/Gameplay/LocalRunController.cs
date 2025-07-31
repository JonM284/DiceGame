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

        #endregion

        #region Class Implementation

        public void StartRun()
        {
            m_startingGO.SetActive(false);
            m_runStateManager.InitStateMachine(ERunState.MAP);
        }

        
        #endregion


    }
}