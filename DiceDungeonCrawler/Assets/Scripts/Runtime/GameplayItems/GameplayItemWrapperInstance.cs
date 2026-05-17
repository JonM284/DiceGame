using System;
using Data.ItemDatas;

namespace Runtime.GameplayItems
{
    [Serializable]
    public class GameplayItemWrapperInstance
    {
        public string itemDataGUID;
        public int currentUses;

        public GameplayItemWrapperInstance(string itemGUID, int savedUses = 0)
        {
            itemDataGUID = itemGUID;
            currentUses = savedUses;
        }
    }
}