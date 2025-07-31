using System;
using Project.Scripts.Utils;
using Runtime.ScriptedAnimations;
using Runtime.ScriptedAnimations.Transform;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.UI.Items
{
    public class PopupTextItem: MonoBehaviour
    {

        #region Actions

        private Action<PopupTextItem> OnEndTextDisplay;

        #endregion
        
        #region Public Fields

        public UnityEvent onStartTextDisplay;

        #endregion

        #region Serialied Fields
        
        [SerializeField] private RelativeTransformPositionAnimation textAnimation;

        [SerializeField] private AnimationsBase fadeAnimation;
        
        [SerializeField] private TMP_Text _text;

        #endregion

        #region Class Implementation

        public void SetupText(string displayString, Color _desiredColor, Action<PopupTextItem> _endAction)
        {
            _text.text = displayString;
            //_text.color = _desiredColor;

            onStartTextDisplay?.Invoke();

            if (!_endAction.IsNull())
            {
                OnEndTextDisplay = _endAction;
            }
            
            textAnimation.Initialize();
            fadeAnimation.Play();
        }

        public void EndText()
        {
            OnEndTextDisplay?.Invoke(this);
        }

        #endregion



    }
}