using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;

namespace Runtime.StateMachines
{
    [AddComponentMenu("State/Map State")]
    public class MapState: StateBase
    {

        #region Serialized Fields

        [SerializeField] private Vector3 m_defaultCamRot, m_mapCamRot;

        [SerializeField] private float m_rotationDuration = 0.15f;
        
        #endregion
        
        public override async UniTask EnterState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            //Draw Map outside of player's view
            await LocalMapController.Instance.T_DrawMapAsync(token);
            //Move map to player
            await base.EnterState(token);
            //Change camera Angle (might not be necessary)
            await LocalCameraController.Instance.RotateCameraTo(m_mapCamRot, m_rotationDuration, token);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override async UniTask ExitState(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            //Reset Camera
            LocalCameraController.Instance.RotateCameraTo(m_defaultCamRot, m_rotationDuration, token);
            //Move map off screen
            await base.ExitState(token);
        }
        
        public override void UpdateState()
        {
            //Do interaction checks here
        }
        
        
    }
}