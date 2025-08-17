using Cysharp.Threading.Tasks;
using Runtime.Character.StateMachines;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Reward State")]
    public class RewardState: StateBase
    {
        #region Serialized Fields
        
        
        
        #endregion

        #region State Inherited Methods

        public override async UniTask EnterState()
        {
            await base.EnterState();
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override async UniTask ExitState()
        {
            await base.ExitState();
        } 

        #endregion
        
       
    }
}