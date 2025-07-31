using Project.Scripts.Utils;
using Runtime.Selection;
using UnityEngine;
using UnityEngine.EventSystems;
using Rewired;

namespace Runtime.GameControllers
{
    public class SelectionController : GameControllerBase
    {

        #region Serialized Fields

        [SerializeField] private LayerMask selectableLayers;

        #endregion

        #region Private Fields

        private float m_mouseDownTime;

        private float m_mouseInputThreshold = 0.25f;

        private int playerID = 0;

        private Player m_player;

        private UnityEngine.Camera m_mainCamera;
        
        private Plane m_dragPlane = new Plane(Vector3.up, Vector3.zero);
        
        private bool m_isDrag;
        
        private Vector3 m_dragStartPos;

        #endregion

        #region Accessors

        public ISelectable currentSelectable { get; private set; }
        
        public UnityEngine.Camera mainCamera => CommonUtils.GetRequiredComponent(ref m_mainCamera, () =>
        {
            var c = UnityEngine.Camera.main;
            return c;
        });
        
        #endregion
        
        #region Unity Events

        private void Update()
        {
            if (!is_Initialized)
            {
                return;
            }
            
            CheckUserInput();
        }

        #endregion
        
        #region Class Implementation

        private void CheckUserInput()
        {
            if (m_player.GetButtonDown("Confirm"))
            {
                m_mouseDownTime = Time.time;
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                float entry;
                if (m_dragPlane.Raycast(ray, out entry))
                {
                    m_dragStartPos = ray.GetPoint(entry);
                }
            }

            if (m_player.GetButtonUp("Confirm"))
            {
                var _timeFromPress = Time.time - m_mouseDownTime;
                if (_timeFromPress <= m_mouseInputThreshold && !m_isDrag)
                {
                    TrySelect();
                }

                m_isDrag = false;
            }
        }

        private void TrySelect()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            Debug.Log("TrySelect");
            if (mainCamera == null)
            {
                Debug.LogError("<color=red>No Camera</color>");
                return;
            }

            if (!Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, selectableLayers))
            {
                Debug.Log("<color=red>No Hit Detected</color>");
                return;
            }

            var selectable = hit.collider.GetComponent<ISelectable>();

            if (selectable == null)
            {
                Debug.Log($"<color=red>No Selectable // Name:{hit.collider.name} </color>", hit.collider);
                return;
            }

            if (currentSelectable != selectable)
            {
                if (!currentSelectable.IsNull())
                {
                    currentSelectable.OnUnselect();
                }
                currentSelectable = selectable;
            }
            
            selectable.OnSelect();
        }

        #endregion

        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            m_player = ReInput.players.GetPlayer(playerID);
            base.Initialize();
        }

        #endregion

        #region Saved Code

        /*Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        float entry;
            if (m_dragPlane.Raycast(ray, out entry))
        {
            var dragCurrentPosition = ray.GetPoint(entry);
            if ((m_dragStartPos - dragCurrentPosition).magnitude > 0.2)
            {
                m_isDrag = true;
            }
        }*/

        #endregion
        
        
    }
}