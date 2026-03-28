using System;
using UnityEngine;

namespace Runtime.Gameplay
{
    [Serializable]
    public class MoverGroups
    {
        public bool isRotate;
        public Transform onScreenTransform, offScreenTransform;
        public Transform target;
    }
}