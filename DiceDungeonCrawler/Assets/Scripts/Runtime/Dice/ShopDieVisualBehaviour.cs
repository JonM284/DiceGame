using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Runtime.Dice
{
    public class ShopDieVisualBehaviour: MonoBehaviour
    {

        [SerializeField] protected List<TMP_Text> faces = new List<TMP_Text>();
        
        public async UniTask Initialize(List<int> dieSides)
        {
            for (var i = 0; i < dieSides.Count; i++)
            {
                if(faces[i].IsNull()) continue;
                faces[i].text = dieSides[i] is 6 or 9
                    ? $"<u>{dieSides[i]}</u>"
                    : dieSides[i].ToString();
            }
        }
        
    }
}