using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Character.StateMachines;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Reward State")]
    public class RewardState: StateBase
    {
        
        

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
        
       
    }
}