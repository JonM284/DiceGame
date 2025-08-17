using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Runtime.Gameplay
{
    /// <summary>
    /// This is data to represent each point on the map
    /// </summary>
    [Serializable]
    public class MapPointData
    {
        public int pointID;
        public Vector3 worldPointLocation;
        public string eventGUID;
        public bool isCurrentPoint, isPassed;
        public SerializedDictionary<int, Vector3> nextLevelConnectedPoints = new SerializedDictionary<int, Vector3>();
        public SerializedDictionary<int, Vector3> previousLevelConnectedPoints = new SerializedDictionary<int, Vector3>();
    }
}