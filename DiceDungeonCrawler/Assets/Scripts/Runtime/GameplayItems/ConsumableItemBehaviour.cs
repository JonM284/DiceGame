using System.Threading;
using Cysharp.Threading.Tasks;

namespace Runtime.GameplayItems
{
    public class ConsumableItemBehaviour: GameplayItemsBase
    {

        #region Private Fields

         

        #endregion
        
        #region GameplayItemBase Inherited Methods

        public void InitializeConsumable()
        {
            
        }

        public override async UniTask DoItemAbility(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            
            
            await base.DoItemAbility(token);
        }

        #endregion

        #region Class Implementation

        

        #endregion
        
    }
}