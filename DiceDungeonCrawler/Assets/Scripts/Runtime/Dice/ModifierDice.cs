using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.Dice;
using DG.Tweening;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.Dice
{
    public class ModifierDice: BaseDie
    {

        #region Serialized Fields

        [Header("Modifier Die Fields")] 
        [SerializeField] private ModifierDiceData modDieData;
        [SerializeField] private List<ModDieFace> modFaces = new List<ModDieFace>();

        #endregion
        
        public override void Initialize()
        {
            SetFaceIcons();
        }

        public ModifierDiceData GetDieData() => modDieData;

        private void SetFaceIcons()
        {
            if (modDieData.dieIcon.IsNull())
            {
                return;
            }
            
            modFaces.ForEach(face => face.spriteRenderer.sprite = modDieData.dieIcon);
        }

        public override UniTask DoActionAsync(CancellationToken token)
        {
            return base.DoActionAsync(token);
        }

        public override async UniTask MoveDieAsync(Vector3 _newPosition, float _duration, bool _highlightEffects, CancellationToken token)
        {
            if (!_highlightEffects)
            {
                SelectEffects(_highlightEffects);
            }

            await transform.DOMove(_newPosition, _duration).SetEase(Ease.Linear).ToUniTask(cancellationToken: token);
            
            if (_highlightEffects)
            {
                SelectEffects(_highlightEffects);
            }
        }

        public override void RotateDie(Vector3 _endRotation, float _duration)
        {
            
        }
    }
}