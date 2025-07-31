using DG.Tweening;
using UnityEngine;

namespace Runtime.Selection
{
    public class DraggableBase: MonoBehaviour, IDraggable
    {

        [SerializeField] protected float m_dragSpeed;

        public bool canBeDragged { get; private set; } = true;
        
        public Vector3 savedReturnLocation { get; private set; }

        public void OnBeginDrag(Vector3 _dragStartPos)
        {
            savedReturnLocation = transform.position;
            transform.position = _dragStartPos;
        }

        public void OnUpdateDragPosition(Vector3 _newPosition)
        {
            transform.position = Vector3.Lerp(transform.position, _newPosition, m_dragSpeed);
        }

        public void OnEndDrag(Vector3 _dragEndPos)
        {
            transform.DOMove(_dragEndPos, 0.15f).SetEase(Ease.InOutElastic);
        }

        public void SetDraggable(bool _canDrag)
        {
            canBeDragged = _canDrag;
        }
    }
}