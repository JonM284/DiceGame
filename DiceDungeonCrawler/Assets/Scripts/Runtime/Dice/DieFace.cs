using System;
using TMPro;
using UnityEngine;

namespace Runtime.Dice
{
    [Serializable]
    public class DieFace
    {
        public TMP_Text faceValueText;
        public Transform associatedFace;
        public int value;
    }
}