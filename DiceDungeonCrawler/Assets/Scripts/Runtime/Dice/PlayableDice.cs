using System;
using System.Collections.Generic;
using DG.Tweening;
using Project.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Dice
{
    public class PlayableDice: BaseDie
    {

        #region Actions

        public static event Action<int> onDieRollFinished; 

        #endregion

        #region Serialized Fields
        
        [SerializeField] protected float m_minThrowForce = 1f, m_maxThrowForce = 3f;

        [SerializeField] protected float m_minRollForce = 1f, m_maxRollForce = 2.6f;
        
        [SerializeField] protected AudioSource m_audioSource;

        [SerializeField] protected float m_minPitch = 0.9f, m_maxPitch = 1.1f;

        [SerializeField] protected AnimationCurve m_volumeCurve;

        #endregion

        #region Private Fields

        protected float m_startTime, m_maxTime = 1f;

        #endregion
        
        #region Accessors

        public int rollValue { get; protected set; }
        
        #endregion
        
        #region Unity Events

        private void FixedUpdate()
        {
            if (!isRolling)
            {
                return;
            }

            CheckFinishedRolling();
        }

        private void OnCollisionEnter(Collision other)
        {
            PlayRandomSound();
        }

        #endregion
        
        #region Class Implementation

        
        public override void Initialize()
        {
            faces.ForEach(df => df.faceValueText.text = df.value == 6 || df.value == 9 ? $"<u>{df.value}</u>" 
                : df.value.ToString());
        }

        public override void DoAction()
        {
            if (rb.IsNull())
            {
                return;
            }

            isRolling = true;

            m_startTime = Time.time;
            
            Vector3 _randomThrowForce = new Vector3(Random.Range(-0.5f, 0.5f),
                Random.Range(m_minThrowForce, m_maxThrowForce)
                , Random.Range(m_minThrowForce, m_maxThrowForce));
            rb.AddForce(_randomThrowForce, ForceMode.Impulse);

            Vector3 _randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) *
                                    Random.Range(m_minRollForce, m_maxRollForce);
            rb.AddTorque(_randomTorque, ForceMode.Impulse);
        }
        
        public void GetUpFace()
        {
            m_currentUpSide = null;
            float _highestDot = -1f;

            foreach (var _face in faces)
            {
                var _dot = Vector3.Dot(_face.associatedFace.forward, Vector3.up);

                if (_dot < _highestDot)
                {
                    continue;
                }
                
                _highestDot = _dot;
                m_currentUpSide = _face;
            }

            if (m_currentUpSide.IsNull())
            {
                rollValue = 0;
                return;
            }

            rollValue = m_currentUpSide.value;
            onDieRollFinished?.Invoke(rollValue);
            Debug.Log($"Rolled: {rollValue}");
        }

        private void OnDieStopRolling()
        {
            GetUpFace();
        }

        private void CheckFinishedRolling()
        {
            if (!rb.IsSleeping())
            {
                return;
            }

            isRolling = false;
            OnDieStopRolling();
        }
        
        private void HighlightUpsideFace(bool _enabled)
        {
            if (m_currentUpSide.IsNull())
            {
                return;
            }
            
            m_currentUpSide.faceValueText.color = _enabled ? Color.green : Color.white;
        }

        public override void MoveDie(Vector3 _newPosition, float _duration, bool _highlightEffects)
        {
            SelectEffects(false);
            
            Debug.Log("Moving");
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
            transform.DORotate(_endRotation, _duration).SetEase(Ease.InOutElastic);
        }

        public override void SelectEffects(bool _enabled)
        {
            HighlightUpsideFace(_enabled);

            base.SelectEffects(_enabled);
        }

        public void PlayRandomSound()
        {
            if (m_audioSource.IsNull())
            {
                return;
            }

            if (!isRolling)
            {
                return;
            }
            
            m_audioSource.pitch = Random.Range(m_minPitch, m_maxPitch);
            m_audioSource.volume = m_volumeCurve.Evaluate(Time.time - m_startTime / m_maxTime);
            m_audioSource.Play();
        }

        #endregion
        
        
    }
}