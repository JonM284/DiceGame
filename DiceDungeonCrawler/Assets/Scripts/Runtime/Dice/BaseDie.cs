using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Selection;
using UnityEngine;

namespace Runtime.Dice
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseDie: DraggableBase, ISelectable
    {

        #region Actions

        public static event Action<BaseDie> onDieSelected;

        public static event Action<BaseDie> onDieUnselected;

        public static event Action<BaseDie> onDieHovered;

        public static event Action<BaseDie> onDieUnhovered; 
        
        #endregion

        #region Serialized Fields
        
        [SerializeField] protected List<DieFace> faces = new List<DieFace>();

        [Header("Game Feel")]
        [SerializeField] protected float m_scaleOffset = 0.25f;

        [SerializeField] protected float m_positionShakeStrength = 0.003f, m_positionShakeDuration = 0.4f;
        [SerializeField] protected float m_rotationShakeStrength = 20f, m_rotationShakeDuration = 2f;

        #endregion

        #region Private Fieldss

        protected Rigidbody m_rigidbodyRef;
        
        protected Collider m_colliderRef;

        protected Tweener m_selectPositionTweener, m_selectRotationTweener;

        protected Vector3 m_savedRestPosition, m_originalScale;
        
        #endregion

        #region Accessors

        public Rigidbody rb => CommonUtils.GetRequiredComponent(ref m_rigidbodyRef, GetComponent<Rigidbody>);
        
        public Collider boxCol => CommonUtils.GetRequiredComponent(ref m_colliderRef, GetComponent<Collider>);
        
        public bool isRolling { get; protected set; }
        
        public DieFace m_currentUpSide { get; protected set; }

        public int dieValue => !m_currentUpSide.IsNull() ? m_currentUpSide.value : 1;

        #endregion

        #region Unity Events

        private void Start()
        {
            m_originalScale = transform.localScale;
        }

        #endregion
        
        #region Class Implementation

        [ContextMenu("Initialize Die")]
        public abstract void Initialize();

        [ContextMenu("Roll Die")]
        public virtual async UniTask DoActionAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }

        public void EnablePhysics(bool _enable)
        {
            rb.useGravity = _enable;
            rb.isKinematic = !_enable;
            boxCol.isTrigger = !_enable;
        }

        public virtual async UniTask MoveDieAsync(Vector3 _newPosition, float _duration, bool _highlightEffects,
            CancellationToken token)
        {
            await UniTask.CompletedTask;
        }

        public abstract void RotateDie(Vector3 _endRotation, float _duration);

        public void HoverEffects(bool _onHover)
        {
            transform.DOScale(_onHover ? m_originalScale + (Vector3.one * m_scaleOffset) : m_originalScale, 0.1f).SetEase(Ease.InOutElastic);
        }
        
        public virtual void SelectEffects(bool _enabled)
        {
            Debug.Log($"Highlight: {_enabled}");
            
            if (_enabled)
            {
                m_selectPositionTweener = transform.DOShakePosition(m_positionShakeDuration, m_positionShakeStrength).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InElastic);
                m_selectRotationTweener = transform.DOShakeRotation(m_rotationShakeDuration, m_rotationShakeStrength, 5).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutElastic);
            }
            else
            {
                if (!m_selectPositionTweener.IsNull())
                {
                    m_selectPositionTweener.Kill();
                }
                
                if (!m_selectRotationTweener.IsNull())
                {
                    m_selectRotationTweener.Kill();
                }
            }
        }

        public virtual void CalculationEffects()
        {
            transform.DOShakeRotation(0.3f, 60f);
            transform.DOScale(m_originalScale + (Vector3.one * m_scaleOffset)
                , 0.15f).SetEase(Ease.InOutElastic).OnComplete(() =>
                {
                    transform.DOScale(m_originalScale, 0.15f).SetEase(Ease.InOutElastic);
                });
        }
        
        #endregion

        #region ISelectable Inherited Methods

        public void OnSelect()
        {
            onDieSelected?.Invoke(this);
        }

        public void OnUnselect()
        {
            onDieUnselected?.Invoke(this);
        }

        public void OnHoverStart()
        {
            onDieHovered?.Invoke(this);
        }

        public void OnHoverEnd()
        {
            onDieUnhovered?.Invoke(this);
        }

        #endregion
        
        
        
    }
}