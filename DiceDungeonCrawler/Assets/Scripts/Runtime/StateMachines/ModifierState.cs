using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Character.StateMachines;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("SubState/Modifier SubState")]
    public class ModifierState: StateBase
    {
        #region Serialized Fields
        
        
        
        #endregion

        #region Private Fields

        private bool isRunning;

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

        public void OnSubstateInteraction()
        {
            if (stateManager.isTransitioning)
            {
                return;
            }

            isRunning = !isRunning;
            
            if (!isRunning)
            {
                isCompleted = true;
            }
            
            stateManager.DoSubstateProcess(isRunning ,this.stateEnum);
        }

        #endregion
    }
}