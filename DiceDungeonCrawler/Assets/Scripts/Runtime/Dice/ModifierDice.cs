using DG.Tweening;
using UnityEngine;

namespace Runtime.Dice
{
    public class ModifierDice: BaseDie
    {
        
        
        public override void Initialize()
        {
            
        }

        public override void DoAction()
        {
            
        }

        public override void MoveDie(Vector3 _newPosition, float _duration, bool _highlightEffects)
        {
            if (!_highlightEffects)
            {
                SelectEffects(_highlightEffects);
            }
            
            transform.DOMove(_newPosition, _duration).SetEase(Ease.Linear).OnComplete(() =>
            {
                if (_highlightEffects)
                {
                    SelectEffects(_highlightEffects);
                }
            });
        }

        public override void RotateDie(Vector3 _endRotation, float _duration)
        {
            
        }
    }
}