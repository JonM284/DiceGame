using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Runtime.Dice
{
    [Serializable]
    public class DieFace
    {
        public List<TMP_Text> faceValueText;
        public Transform associatedFace;
        public int value;
    }
}