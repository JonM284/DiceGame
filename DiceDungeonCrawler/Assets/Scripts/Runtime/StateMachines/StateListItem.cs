using System;
using Runtime.RunStates;

namespace Runtime.Character.StateMachines
{
    [Serializable]
    public class StateListItem
    {
        public ERunState stateType;
        public StateBase stateBehavior;

        public StateListItem(ERunState _stateEnum, StateBase _stateBehavior)
        {
            stateType = _stateEnum;
            stateBehavior = _stateBehavior;
        }
    }
}