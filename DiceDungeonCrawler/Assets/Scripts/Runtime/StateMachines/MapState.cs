using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Character.StateMachines;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;

namespace Runtime.StateMachines
{
    public class MapState: StateBase
    {

        #region Serialized Fields

        [SerializeField] private Vector3 m_defaultCamRot, m_mapCamRot;

        [SerializeField] private float m_rotationDuration = 0.15f;
        #endregion
        
        public override async UniTask EnterState()
        {
            await base.EnterState();
            LocalMapController.Instance.DrawMap();
            LocalCameraController.Instance.RotateCameraTo(m_mapCamRot, m_rotationDuration);
        }

        public override void AssignArgument(params object[] _arguments)
        {
            
        }

        public override void SetupState()
        {
            
        }

        public override async UniTask ExitState()
        {
            LocalCameraController.Instance.RotateCameraTo(m_defaultCamRot, m_rotationDuration);
            await base.ExitState();
        }
        
        
    }
}