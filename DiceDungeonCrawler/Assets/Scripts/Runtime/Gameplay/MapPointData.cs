using System;
using System.Collections.Generic;

namespace Runtime.Gameplay
{
    /// <summary>
    /// This is data to represent each point on the map
    /// </summary>
    [Serializable]
    public class MapPointData
    {
        public int pointID;
        public string eventGUID;
        public bool isCurrentPoint, isPassed, isCompleted;
        public List<int> nextLevelConnectedPoints = new List<int>();
        public List<int> previousLevelConnectedPoints = new List<int>();
    }
}