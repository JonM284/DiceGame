using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.TintData;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Dice.Enums;
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

        [SerializeField] protected float m_minPitch = 0.4f, m_maxPitch = 1.1f;

        [SerializeField] protected AnimationCurve m_volumeCurve;

        [SerializeField] protected MeshRenderer meshRenderer;

        #endregion

        #region Private Fields

        protected float m_startTime, m_maxTime = 1f;

        private CancellationTokenSource cts = new CancellationTokenSource();

        #endregion
        
        #region Accessors

        public int rollValue { get; protected set; }

        public TintDataBase TintData { get; protected set; }

        public TintType TintType => !TintData.IsNull() ? TintData.tintType : TintType.NONE; 

        #endregion
        
        #region Unity Events

        private void OnCollisionEnter(Collision other)
        {
            PlayRandomSound();
        }

        #endregion
        
        #region Class Implementation

        
        public override void Initialize()
        {
            EnablePhysics(false);
            faces.ForEach(df => df.faceValueText.ForEach(text => text.text = df.value is 6 or 9 ? $"<u>{df.value}</u>" 
                : df.value.ToString()));
        }

        private CancellationToken GetToken()
        {
            if (!cts.IsNull())
            {
                cts.Cancel();
            }
            
            cts = new CancellationTokenSource();
            
            return cts.Token;
        }

        public override async UniTask DoActionAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (rb.IsNull())
            {
                return;
            }

            //Throw Die
            isRolling = true;
            m_startTime = Time.time;
            
            Vector3 _randomThrowForce = new Vector3(Random.Range(-0.5f, 0.5f),
                Random.Range(m_minThrowForce, m_maxThrowForce)
                , Random.Range(m_minThrowForce, m_maxThrowForce));
            
            rb.AddForce(_randomThrowForce, ForceMode.Impulse);

            Vector3 _randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) *
                                    Random.Range(m_minRollForce, m_maxRollForce);
            rb.AddTorque(_randomTorque, ForceMode.Impulse);
            
            //Wait to finish
            await UniTask.WaitUntil(() => rb.IsSleeping(), cancellationToken: token);

            //Show top face
            isRolling = false;
            await ShowResultFaceAsync(token);
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

        private async UniTask ShowResultFaceAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GetUpFace();
            
            var targetUpRotation = Quaternion.FromToRotation(m_currentUpSide.associatedFace.forward, Vector3.up) * transform.rotation;
            var targetForwardRotation =
                Quaternion.FromToRotation(m_currentUpSide.associatedFace.up, Vector3.forward) * transform.rotation;
            
            var tasks = new List<UniTask>();
            
            tasks.Add(transform.DORotateQuaternion(targetUpRotation, 0.5f).WithCancellation(token));
            tasks.Add(transform.DORotateQuaternion(targetForwardRotation, 0.5f).WithCancellation(token));

            await tasks;
        }
        
        private void HighlightUpsideFace(bool _enabled)
        {
            if (m_currentUpSide.IsNull())
            {
                return;
            }
            
            m_currentUpSide.faceValueText.ForEach(text => text.color = _enabled ? Color.green : Color.white);
        }

        public override async UniTask MoveDieAsync(Vector3 _newPosition, float _duration, bool _highlightEffects, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SelectEffects(false);
            
            Debug.Log("Moving");
            await transform.DOMove(_newPosition, _duration).SetEase(Ease.Linear).ToUniTask(cancellationToken: token);
            
            if (_highlightEffects)
            {
                SelectEffects(_highlightEffects);
            }
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

        public void ChangeTintType(TintDataBase newTint)
        {
            if (newTint.IsNull())
            {
                return;
            }

            TintData = newTint;

            if (meshRenderer.IsNull())
            {
                Debug.LogError("[Dice Logic] MeshRenderer not assigned");
                return;
            }

            var mat = meshRenderer.materials[0];
            mat.color = newTint.tintColor;
            
            faces.ForEach(df => df.faceValueText.ForEach(text => text.color = newTint.tintColor));
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

        public List<DieFace> GetFaces() => faces;

        #endregion


    }
}