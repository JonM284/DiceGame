using System;
using System.Collections.Generic;
using Data.DataSaving;
using NUnit.Framework;

namespace Runtime.Gameplay
{
    /// <summary>
    /// Level -> 1 row in the map
    /// </summary>
    [Serializable]
    public class LevelItem
    {
        public int levelIndex;
        public List<MapPointData> levelPoints = new  List<MapPointData>();
    }
}