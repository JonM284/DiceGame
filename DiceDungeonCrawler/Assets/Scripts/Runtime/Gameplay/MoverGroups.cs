using System;
using UnityEngine;

namespace Runtime.Gameplay
{
    [Serializable]
    public class MoverGroups
    {
        public Vector3 onScreenPosition, offScreenPosition;
        public Transform target;
    }
}