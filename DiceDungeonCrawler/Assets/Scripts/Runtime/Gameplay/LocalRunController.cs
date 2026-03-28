using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.RunStates;
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

        private CancellationTokenSource cts;
        
        #endregion

        #region Class Implementation

        public void StartRun()
        {
            if (!cts.IsNull())
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            
            m_startingGO.SetActive(false);
            m_runStateManager.InitStateMachine(ERunState.MAP, cts.Token).Forget();
        }
        
        #endregion


    }
}