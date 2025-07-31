
using UnityEngine;

namespace Runtime.Selection
{
    public interface IDraggable
    {
        public void OnBeginDrag(Vector3 _dragStartPos);
        public void OnUpdateDragPosition(Vector3 _newPosition);
        public void OnEndDrag(Vector3 _dragEndPos);
    }
}