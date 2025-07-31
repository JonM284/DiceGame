using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class GlobalMovementController: GameControllerBase
    {

        #region Nested Classes

        [Serializable]
        public class MovableObject
        {
            public Vector3 from, to;
            public Quaternion fromRot, toRot;
            public Transform target;
            public float percentage, startTime, maxTime;
            public Action onComplete;


            public MovableObject(Transform _target, float _maxTime, Vector3 _from, Vector3 _to, Action _onComplete = null)
            {
                target = _target;
                startTime = Time.time;
                maxTime = _maxTime;
                from = _from;
                to = _to;
                percentage = 0;
                this.onComplete = _onComplete;
            }

        }

        #endregion
        
        #region Instance

        public static GlobalMovementController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private AnimationCurve m_curve;

        #endregion

        #region Private Fields

        private List<MovableObject> m_movingObjects = new List<MovableObject>();

        private List<MovableObject> m_finishedMovingObjects = new List<MovableObject>();

        #endregion
        
        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion

        #region Unity Events

        private void Update()
        {
            if (m_movingObjects.Count == 0)
            {
                return;
            }
            
            foreach (var _movingObject in m_movingObjects)
            {
                _movingObject.percentage = (Time.time - _movingObject.startTime) / _movingObject.maxTime;

                _movingObject.target.position =
                    Vector3.LerpUnclamped(_movingObject.from, _movingObject.to, m_curve.Evaluate(_movingObject.percentage));

                if (_movingObject.percentage >= 1)
                {
                    m_finishedMovingObjects.Add(_movingObject);
                }
            }

            if (m_finishedMovingObjects.Count == 0)
            {
                return;
            }
            
            foreach (var _finished in m_finishedMovingObjects)
            {
                m_movingObjects.Remove(_finished);
            }
            
            m_finishedMovingObjects.Clear();
        }

        #endregion

        #region Class Implementation

        public void AddMovableObject(Transform _target, float _maxTime, Vector3 _from, Vector3 _to, Action onComplete = null)
        {
            m_movingObjects.Add(new MovableObject(_target, _maxTime, _from, _to, onComplete));
        }

        #endregion
        
    }
}