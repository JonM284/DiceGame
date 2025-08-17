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
        
        public override async UniTask EnterState()
        {
            //Draw Map outside of player's view
            await LocalMapController.Instance.T_DrawMapAsync();
            //Move map to player
            await base.EnterState();
            //Change camera Angle (might not be necessary)
            LocalCameraController.Instance.RotateCameraTo(m_mapCamRot, m_rotationDuration);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override async UniTask ExitState()
        {
            //Reset Camera
            LocalCameraController.Instance.RotateCameraTo(m_defaultCamRot, m_rotationDuration);
            //Move map off screen
            await base.ExitState();
        }
        
        
    }
}