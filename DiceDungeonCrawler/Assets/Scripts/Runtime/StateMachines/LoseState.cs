using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Character.StateMachines;
using Runtime.Gameplay;
using Runtime.RunStates;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Lose State")]
    public class LoseState: StateBase
    {
        
        #region Serialized Fields
        
        
        
        #endregion

        #region State Inherited Methods

        public override async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.EnterState(token);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override async UniTask ExitState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.ExitState(token);
        } 

        public override void UpdateState()
        {
            //Do interaction checks here
        }
        
        #endregion

        #region Class Implementation

        public void RestartRun()
        {
            //Reset game
            LocalMapController.Instance.ResetAll();
            stateManager.ChangeState(ERunState.MAP);
        }

        #endregion
        
        
    }
}