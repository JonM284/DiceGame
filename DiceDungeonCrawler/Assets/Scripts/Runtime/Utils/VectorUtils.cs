using UnityEngine;

namespace Project.Scripts.Utils
{
    public static class VectorUtils
    {

        #region Class Implementation

        public static Vector3 FlattenVector3Y(this Vector3 _vector)
        {
            _vector.y = 0;
            return _vector;
        }

        public static bool IsNan(this Vector3 vector3)
        {
            return float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z);
        }
        
        public static Vector3 FlattenVectorToY(this Vector3 _vector, float _desiredY)
        {
            _vector.y = _desiredY;
            return _vector;
        }

        public static bool IsApprox(Vector3 _a, Vector3 _b, float _threshold = 0.1f)
        {
            return Mathf.Abs(_a.x - _b.x) < _threshold 
                   && Mathf.Abs(_a.y - _b.y) < _threshold 
                   && Mathf.Abs(_a.z - _b.z) < _threshold;
        }
        
        #endregion
        
    }
}