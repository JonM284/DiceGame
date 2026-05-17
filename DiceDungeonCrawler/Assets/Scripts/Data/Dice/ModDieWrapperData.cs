using UnityEngine;

namespace Data.Dice
{
    [SerializeField]
    public class ModDieWrapperData
    {
        public int index;
        public string guid;

        public ModDieWrapperData(int _index)
        {
            index = _index;
        }
    }
}