using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class LocalCameraController: GameControllerBase
    {
        
        #region Static

        public static LocalCameraController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private Transform m_cameraPositionTracker, m_cameraRotationTracker, m_cameraZoomTracker;
        [SerializeField] private Camera m_cameraRef;
        
        #endregion

        #region Unity Events

        private void Start()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
        }

        #endregion
        
        #region Class Implementation

        public async UniTask MoveCameraToAsync(Vector3 _newPosition, float _duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await m_cameraPositionTracker.DOMove(_newPosition, _duration).SetEase(Ease.Linear).AsyncWaitForCompletion();
        }

        public async UniTask RotateCameraTo(Vector3 _endRotation, float _duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await m_cameraRotationTracker.DORotate(_endRotation, _duration).SetEase(Ease.Linear).AsyncWaitForCompletion();
        }

        public async UniTask ZoomCamera(float _endZoom, float _duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await m_cameraZoomTracker.DOMoveZ(_endZoom, _duration).SetEase(Ease.Linear).AsyncWaitForCompletion();
        }

        #endregion

    }
}