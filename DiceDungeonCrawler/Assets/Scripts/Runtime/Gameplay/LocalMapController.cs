using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using Data.DataSaving;
using DG.Tweening;
using Project.Scripts.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    public class LocalMapController: MonoBehaviour, ISaveableData
    {

        #region Singleton

        public static LocalMapController Instance { get; private set; }

        #endregion

        #region Events

        public static event Action OnMapGenerated;

        #endregion
        
        #region Serialized Fields
        
        [SerializeField] private bool isLobbyScene;

        [SerializeField] private GameObject m_mapEventPrefab;

        //Connector Line is a Line Renderer, remember this when instantiating
        [SerializeField] private GameObject connectorPrefab;

        [SerializeField] private Transform mapGenParent, starterPoint;
        
        [SerializeField] private float levelHorizontalSpacing = 3f;

        [SerializeField] private float pointOffsetX = 1f, pointOffsetY = 1f;

        [SerializeField] private int maxAmountOfLevels, amountOfLevelsToShow = 3;
        [SerializeField] private float m_levelSpacing = 1f;

        [SerializeField] private List<GameplayEventType> possibleRandomEventTypes = new List<GameplayEventType>();
        [SerializeField] private List<GameplayEventType> allEventTypes = new List<GameplayEventType>();

        [SerializeField] private GameplayEventType miniBossEventType, finalBossEventType, startPointEventType;

        [Header("Player Marker")]
        [SerializeField] private Transform playerMarker;
        [SerializeField] private float markerYOffset = 1f;
        
        #endregion
        
        #region Private Fields
        
        private int maxColumns = 5;
        
        private string m_lastEventIdentifier, m_currentEventIdentifier;

        private Vector3 m_lastPOILocation;

        private bool m_isReturnFromEvent;

        private bool m_currentEventEnded;

        private int m_currentLevel;

        //saved MapLocations (ObjectPooling)
        private HashSet<GameObject> m_cachedPointObjects = new HashSet<GameObject>();

        //Contains all IDs and MapLocations (GameObject) in the current run. Used to find the correct locations 
        private Dictionary<int, MapLocationAction> m_activePointObjects = new Dictionary<int, MapLocationAction>(); 

        //saved connectors (ObjectPooling)
        private HashSet<GameObject> m_cachedConnectors = new HashSet<GameObject>();
        
        //current connectors (ObjectPooling)
        private HashSet<GameObject> m_activePointConnectors = new HashSet<GameObject>();

        private int m_currentPointIndex;
        private MapPointData m_currentPointData, m_previousPointData;
        private MapLocationAction m_currentPointObj, m_previousPointObj;
        
        private Transform m_inactivePool;

        private int m_currentPointIDIterator;
        
        private SerializableDictionary<int,LevelItem> allCurrentRunLevels = new SerializableDictionary<int,LevelItem>();
        
        private List<GameplayEventType> m_possibleEventTypes = new List<GameplayEventType>();
        
        private float mapTotalDist;

        private Dictionary<int,Vector3> fakeNextLevelPoints = new Dictionary<int,Vector3>();
        
        #endregion

        #region Accessors
        
        public Transform inactivePool => CommonUtils.GetRequiredComponent(ref m_inactivePool, () => TransformUtils.CreatePool(this.transform, false));
        
        #endregion

        #region Unity Events
        
        public void Start()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
        }

        #endregion

        #region Class Implementation

        private async UniTask T_SetupMapForPlayerSelectionAsync()
        {
            //Move Map to show current level + 3 levels above
            Debug.Log("Setting up player selection");
            
            //Force Player marker location
            playerMarker.localPosition =
                m_currentPointObj.transform.localPosition.FlattenVectorToY(starterPoint.localPosition.y + markerYOffset);
            
            //Set connected points to selectable
            SetLevelSelectables();
        }

        //When coming back to map, set points above current point to selectable, other points are considered passed
        public void SetLevelSelectables()
        {
            //For each point in the next row
            foreach (var _pointData in allCurrentRunLevels[m_currentLevel + 1].levelPoints)
            {
                m_activePointObjects.TryGetValue(_pointData.pointID, out MapLocationAction _mapLocationAction);

                if (_mapLocationAction.IsNull())
                {
                    continue;
                }
                
                //If this point ID is in the list of the current point the player is on -> it can be selected
                //i.e.: If the points are connected
                _mapLocationAction.SetSelectable(m_currentPointData.nextLevelConnectedPoints.Contains(_pointData.pointID));
            }
        }

        //After selecting a point
        public async UniTask T_PointSelectedAsync(MapLocationAction _selectedPoint)
        {
            if (_selectedPoint.IsNull())
            {
                return;
            }
            
            //ToDo: check if needed. At the moment not needed
            m_previousPointData = m_currentPointData;
            m_previousPointObj = m_currentPointObj;
            
            m_currentPointData = _selectedPoint.assignedData;
            m_currentPointIndex = _selectedPoint.assignedData.pointID;
            m_currentPointObj = _selectedPoint;
            
            //1. Visuals, set each point in level's visual's to be correct
            foreach (var _pointData in allCurrentRunLevels[m_currentLevel].levelPoints)
            {
                if (m_currentPointData.pointID == _pointData.pointID)
                {
                    //This is the current point: Don't mark passed
                    continue;
                }
                
                m_activePointObjects.TryGetValue(_pointData.pointID, out MapLocationAction _mapLocationAction);

                if (_mapLocationAction.IsNull())
                {
                    continue;
                }
                
                _mapLocationAction.SetPassed();
            }
            
            //2. Move player piece from current point to next point
            await playerMarker
                .DOLocalMove(m_currentPointObj.transform.localPosition
                        .FlattenVectorToY(m_currentPointObj.transform.localPosition.y + markerYOffset), 
                    0.15f)
                .AsyncWaitForCompletion();

            //ToDo: Move Map? maybe not, maybe next time it shows, just move it before the player can see it on screen

        }

        public async UniTask T_DrawMapAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            //Map is not created -> Create New Map
            if (allCurrentRunLevels.IsNull() || allCurrentRunLevels.Count == 0)
            {
                await T_CreateMapAsync();
                return;
            }
            
            //Visuals are already created -> setup space for player selection
            if (m_activePointObjects.Count > 0 
                && !m_activePointObjects.FirstOrDefault().Value.IsNull())
            {
                await T_GenerateVisualMapFromData();
                GetCurrentPointObj();
                await T_SetupMapForPlayerSelectionAsync();
                return;
            }
            
            //Map Data is Generated, but Visuals are NOT Generated
            await T_GenerateVisualMapFromData();
            GetCurrentPointObj();
            await T_SetupMapForPlayerSelectionAsync();
        }
        
        
        //Create Map from scratch
        [ContextMenu("Create Map")]
        private void CreateMap()
        {
            T_CreateMapAsync();
        }
        
        private async UniTask T_CreateMapAsync()
        {

            mapTotalDist = starterPoint.transform.localPosition.z + (m_levelSpacing * (maxAmountOfLevels + 1));
            m_currentPointIDIterator = 0;
            
            CreateFirstLevel();
            
            for (int i = 1; i < maxAmountOfLevels; i++)
            {
                //create random event points
                CreateNormalLevel(i);
            }

            CreateLastLevel();

            for (int i = 1; i < allCurrentRunLevels.Count; i++)
            {
                //Connect event points to form paths
                CheckPreviousPoints(i);
            }
            
            m_currentLevel = 0;
            m_currentPointIndex = 0;
            m_currentPointData = allCurrentRunLevels[0].levelPoints[0]; //First Point is always first created point

            
            await T_GenerateVisualMapFromData();

            GetCurrentPointObj();
                
            Debug.Log("FINISHED MAP GENERATION");
            await T_SetupMapForPlayerSelectionAsync();

        }

        //-----------------------
        
        #region Map Visual Generation
        
        //Regenerate Visuals from Saved Data
        private async UniTask T_GenerateVisualMapFromData()
        {
            //Generate locations when displaying the map
            CacheAllPreviousItems();
            
            int _localLevelIndex = 0;
            
            for (int i = m_currentLevel; i < m_currentLevel + amountOfLevelsToShow; i++)
            {
                int _currentPointIndex = 0;
             
                foreach (var _pointData in allCurrentRunLevels[i].levelPoints)
                {
                    InstantiatePointAt(GetPointPosition(_localLevelIndex, _currentPointIndex, 
                            allCurrentRunLevels[i].levelPoints.Count) 
                        , _pointData ,_pointData.eventGUID);
                    
                    _currentPointIndex++;
                    await UniTask.WaitForEndOfFrame();
                }

                _localLevelIndex++;
            }

            if (m_currentLevel + amountOfLevelsToShow < allCurrentRunLevels.Count)
            {
                fakeNextLevelPoints.Clear();
                
                for(int i = 0; i < allCurrentRunLevels[m_currentLevel + amountOfLevelsToShow].levelPoints.Count; i++)
                {
                    var position = GetPointPosition(m_currentLevel + amountOfLevelsToShow
                        , i, allCurrentRunLevels[m_currentLevel + amountOfLevelsToShow].levelPoints.Count);
                    fakeNextLevelPoints.Add(allCurrentRunLevels[m_currentLevel + amountOfLevelsToShow].levelPoints[i].pointID, position);
                }   
            }
            
            for (int i = m_currentLevel; i < m_currentLevel + amountOfLevelsToShow; i++)
            {
                foreach (var _pointData in allCurrentRunLevels[i].levelPoints)
                {
                    var currentPointObj = GetPointAtIndex(_pointData.pointID);

                    if (currentPointObj.IsNull())
                    {
                        continue;
                    }
                    
                    foreach (var _nextlevelPointData in _pointData.nextLevelConnectedPoints)
                    {
                        var nextLevelPointObj = GetPointAtIndex(_nextlevelPointData);

                        if (nextLevelPointObj.IsNull())
                        {
                            //Do one last try
                            if (fakeNextLevelPoints.Count > 0)
                            {
                                fakeNextLevelPoints.TryGetValue(_nextlevelPointData, out Vector3 pos);

                                if (pos.IsNull() || pos.IsNan())
                                {
                                    continue;
                                }
                                
                                ConnectPoints(currentPointObj.transform.localPosition, pos);
                            }
                            continue;
                        }
                        
                        ConnectPoints(currentPointObj.transform.localPosition, nextLevelPointObj.transform.localPosition);
                    }
                    
                    await UniTask.WaitForEndOfFrame();
                }
            }
        }

        private Vector3 GetPointPosition(int _localLevelIndex, int _currentPointIndex ,int _levelPointAmount)
        {
            float _zPos = starterPoint.localPosition.z + (_localLevelIndex * m_levelSpacing);
                
            Vector3 _startPos = new Vector3(starterPoint.localPosition.x, starterPoint.localPosition.y, _zPos)
                                - (transform.right * ((_levelPointAmount - 1) 
                                                      * levelHorizontalSpacing) / 2f);
            
            float _Xposition = _levelPointAmount > 1 ? 
                _startPos.x + (transform.right.x * _currentPointIndex * levelHorizontalSpacing)
                            + Random.Range(-pointOffsetX, pointOffsetX) : 0;
                    
            _zPos += Random.Range(-pointOffsetY, pointOffsetY);
                    
            return new Vector3(_Xposition, starterPoint.localPosition.y, _zPos);
        }

        #endregion
        
        //-------------------------
        
        #region Map Data Generation

        
        //Starting Location
        private void CreateFirstLevel()
        {
            var _newLevel = new LevelItem();
            var _startingPoint = new MapPointData
            {
                pointID = m_currentPointIDIterator,
                isCurrentPoint = true,
                eventGUID = startPointEventType.eventGUID,
            };

            _newLevel.levelPoints.Add(_startingPoint);
            allCurrentRunLevels.Add(0,_newLevel);
        }

        //Final Boss
        private void CreateLastLevel()
        {
            var _newLevel = new LevelItem
            {
                levelIndex = allCurrentRunLevels.Count
            };

            var _endPoint = new MapPointData
            {
                pointID = 600,
                eventGUID = finalBossEventType.eventGUID,
            };

            _newLevel.levelPoints.Add(_endPoint);
            allCurrentRunLevels.Add(maxAmountOfLevels,_newLevel);
        }

        //Run through created level, connect randomly to points above
        private void CheckPreviousPoints(int _index)
        {
            Debug.Log($"<color=cyan>Connect Level Points: {_index}</color>");
            
            var maxPointsCurrentLevel = allCurrentRunLevels[_index].levelPoints.Count;
            var maxPointsPreviousLevel = allCurrentRunLevels[_index - 1].levelPoints.Count;
            
            var _previousLevel = allCurrentRunLevels[_index - 1];
            var _currentLevel = allCurrentRunLevels[_index];

            #region Current Row is 1 Point

            //only one output in this row, connect all previous to this one
            //Current row = 1 point
            if (maxPointsCurrentLevel == 1)
            {

                _previousLevel.levelPoints.ForEach(mpd =>
                {
                    //Previous row (multiple points): All connect to single point in next level
                    mpd.nextLevelConnectedPoints
                        .Add(_currentLevel.levelPoints[0].pointID); //UP
                    //Connect current point (single) to all points in previous row
                    _currentLevel.levelPoints[0].previousLevelConnectedPoints
                        .Add(mpd.pointID); //DOWN
                });
                
                
                Debug.Log($"<color=#00FF00>[LEVEL {_index} GENERATED: Current LEVEL = Single Point]</color>");
                return;
            }

            #endregion

            #region Previous Row is 1 Point

            //only one input from previous row to current row
            //Previous row = 1 point
            if (maxPointsPreviousLevel == 1)
            {
                foreach (var mpd in _currentLevel.levelPoints)
                {
                    //Previous Row (single point): connect to all points in the next level
                    _previousLevel.levelPoints[0].nextLevelConnectedPoints
                        .Add(mpd.pointID);    //UP
                    //Connect all current level points to previous single point
                    mpd.previousLevelConnectedPoints
                        .Add(_previousLevel.levelPoints[0].pointID);    //DOWN
                }
                Debug.Log($"<color=#00FF00>[LEVEL {_index} GENERATED: Previous LEVEL = Single Point]</color>");

                return;
            }

            #endregion

            #region Other Cases above 1
            
            //All other cases (more than 1 point on a level)
            //Check previous row points
            for (int i = 0; i < _previousLevel.levelPoints.Count; i++)
            {
                //Previous Row -> Current Point
                var _prevLevelCurrentPoint = _previousLevel.levelPoints[i];

                //Far left point, only has 2 possible options
                if (i == 0)
                {
                    //Connect [previous row, current point] directly below to [current row, current point]
                    _prevLevelCurrentPoint.nextLevelConnectedPoints
                        .Add(_currentLevel.levelPoints[i].pointID); //UP
                    
                    _currentLevel.levelPoints[i].previousLevelConnectedPoints
                        .Add(_prevLevelCurrentPoint.pointID); //DOWN

                    //Randomly connect if previous row has less points
                    if (maxPointsPreviousLevel < maxPointsCurrentLevel && Random.Range(0,2) == 0)
                    {
                        _prevLevelCurrentPoint.nextLevelConnectedPoints
                            .Add(_currentLevel.levelPoints[1].pointID); //UP
                        
                        _currentLevel.levelPoints[1].previousLevelConnectedPoints
                            .Add(_prevLevelCurrentPoint.pointID); //DOWN
                    }
                    
                    continue;
                }
 
                
                //Current Point is Far right in the Previous Level
                //Far Right Point only has 2 possible options
                if (i == _previousLevel.levelPoints.Count - 1)
                {
                    //Far Right Point Current Level
                    var _rightMostPointCurrentLevel = _currentLevel.levelPoints.LastOrDefault();
                    
                    //Connect [Previous Level Right Most Point] to [Far right Point Current Level]
                    _prevLevelCurrentPoint.nextLevelConnectedPoints
                        .Add(_rightMostPointCurrentLevel.pointID); //UP
                    
                    _rightMostPointCurrentLevel.previousLevelConnectedPoints
                        .Add(_prevLevelCurrentPoint.pointID); //DOWN
                    
                    //Randomly connect if previous row has less points
                    if (maxPointsPreviousLevel < maxPointsCurrentLevel && Random.Range(0,2) == 0)
                    {
                        _prevLevelCurrentPoint.nextLevelConnectedPoints
                            .Add(_currentLevel.levelPoints[^2].pointID); //UP
                        
                        _currentLevel.levelPoints[^2].previousLevelConnectedPoints
                            .Add(_prevLevelCurrentPoint.pointID); //DOWN
                    }
                    
                    continue;
                }

                Debug.Log("//// Creating Initial Random Connections ////");
                //Checking previous level point against current level -> Going Upward
                List<MapPointData> _availableConnects = GetPossiblePoints(i, _currentLevel,
                    maxPointsCurrentLevel >= maxPointsPreviousLevel);
                
                for (int j = 0; j < Random.Range(1, _availableConnects.Count); j++)
                {
                    var randomPoint = _availableConnects[Random.Range(0, _availableConnects.Count)];
                    _availableConnects.Remove(randomPoint);
                    _prevLevelCurrentPoint.nextLevelConnectedPoints
                        .Add(randomPoint.pointID);   //UP
                    randomPoint.previousLevelConnectedPoints
                        .Add(_prevLevelCurrentPoint.pointID);   //DOWN
                }
            }

            #endregion

            #region FINAL CHECK ON LEVEL

            var _unreachablePoints = _currentLevel.levelPoints
                .Where(mpd => mpd.previousLevelConnectedPoints.Count == 0);

            if (!_unreachablePoints.Any())
            {
                Debug.Log($"<color=#00FF00>[LEVEL {_index} GENERATED: NO POINTS ARE UNREACHABLE]</color>");
                return;
            }
            
            Debug.Log($"<color=red>LEVEL {_index} UNREACHABLE POINTS FOUND: [{_unreachablePoints.Count()}]</color>");
            
            //Check all the unreachable points in the current level
            foreach (var _currentPoint in _unreachablePoints)
            {
                //If the point has a point from the previous level connected to this, skip it
                if (_currentPoint.previousLevelConnectedPoints.Count > 0)
                {
                    continue;
                }
                
                //otherwise connect this point to a point from the previous level
                //1. check the index of the point, then check
                var _checkPointIndex = _currentLevel.levelPoints.IndexOf(_currentPoint);

                if (_checkPointIndex == _currentLevel.levelPoints.Count - 1) //is far RIGHT point
                {
                    var _previousLevelPoint = _previousLevel.levelPoints.LastOrDefault();
                    //force connect to last point of previous row
                    _currentPoint.previousLevelConnectedPoints
                        .Add(_previousLevelPoint.pointID);
                    _previousLevelPoint.nextLevelConnectedPoints
                        .Add(_currentPoint.pointID);
                    continue;
                }

                if (_checkPointIndex == 0) //is far LEFT point
                {
                    var _previousLevelPoint = _previousLevel.levelPoints.FirstOrDefault();
                    //force connect to first point of previous row
                    _currentPoint.previousLevelConnectedPoints
                        .Add(_previousLevelPoint.pointID);
                    _previousLevelPoint.nextLevelConnectedPoints
                        .Add(_currentPoint.pointID);
                    continue;
                }

                //Check if there are any points in the previous row that don't have an output
                if (CheckAvailablePointNextRow(_currentPoint, _previousLevel, _checkPointIndex))
                {
                    continue;
                }
                    
                Debug.Log("//// FIXING MISTAKE ////");
                
                //rules: previous row amount = current row amount
                List<MapPointData> _availableConnects = GetPossiblePoints(_checkPointIndex, _previousLevel,
                    maxPointsCurrentLevel <= maxPointsPreviousLevel);

                //Only Connect 1 Point
                var randomOtherConnect = Random.Range(0, _availableConnects.Count);
                var randomPoint = _availableConnects[randomOtherConnect];
                _availableConnects.Remove(randomPoint);
                randomPoint.nextLevelConnectedPoints
                    .Add(_currentPoint.pointID);   //UP
                _currentPoint.previousLevelConnectedPoints
                    .Add(randomPoint.pointID);   //DOWN
            }

            #endregion
            
        }

        private bool CheckAvailablePointNextRow(MapPointData _currentCheckPoint, LevelItem _previousLevel, int _checkPointIndex)
        {
            //First check if there are any that have no outputs
            var _noConnectionPreviousLevelPoint = _previousLevel.levelPoints.FirstOrDefault(mpd =>
                mpd.nextLevelConnectedPoints.Count == 0);

            //If there is none, return false
            if (_noConnectionPreviousLevelPoint.IsNull())
            {
                return false;
            }
                
            //If it is more than 1 index away from the current point return
            if (_previousLevel.levelPoints.IndexOf(_noConnectionPreviousLevelPoint) - _checkPointIndex > 1)
            {
                return false;
            }    
            
            //Otherwise connect
            _currentCheckPoint.previousLevelConnectedPoints
                .Add(_noConnectionPreviousLevelPoint.pointID);
            _noConnectionPreviousLevelPoint.nextLevelConnectedPoints
                .Add(_currentCheckPoint.pointID);
            
            return true;
        }

        private List<MapPointData> GetPossiblePoints(int _index, LevelItem _checkLevel, bool _checkLevelIsBigger)
        {
            //max has to start at 1 because final number is exclusive in random.range
            List<MapPointData> _availableConnects = new List<MapPointData>();

            Debug.Log($"Checking Index: {_index},,,,,, Against Check Level of size {_checkLevel.levelPoints.Count}---- Max Index:{_checkLevel.levelPoints.Count - 1}");
            
            //if you are points in the middle, you usually have 3 options
            if (!_checkLevel.levelPoints[_index - 1].IsNull())
            {
                _availableConnects.Add(_checkLevel.levelPoints[_index - 1]);
            }

            if (_index < _checkLevel.levelPoints.Count &&
                !_checkLevel.levelPoints[_index].IsNull())
            {
                _availableConnects.Add(_checkLevel.levelPoints[_index]);
            }

            if (_checkLevelIsBigger 
                && !_checkLevel.levelPoints[_index + 1].IsNull())
            {
                _availableConnects.Add(_checkLevel.levelPoints[_index + 1]);
            }

            return _availableConnects;
        }
        
        //Create a single level -> points and events
        private void CreateNormalLevel(int _index)
        {
            var _currentLevel = new LevelItem
            {
                levelIndex = _index
            };
            
            var _randomAmountOfEvents = Random.Range(2,maxColumns);

            //Every 6 Levels, MUST FORCE SPECIFIC TYPE OF BATTLE
            
            for (int i = 0; i < _randomAmountOfEvents; i++)
            {
                m_currentPointIDIterator++;
                
                //ToDo: Every 6 levels MUST have 1 miniboss, but it is not a single point
                _currentLevel.levelPoints.Add(new MapPointData
                {
                    eventGUID = _index % 6 == 0 ? GetMiniBossType().eventGUID : GetRandomEventType().eventGUID,
                    pointID = m_currentPointIDIterator
                });
            }
            
            allCurrentRunLevels.Add(_index, _currentLevel);
        }

        #endregion
        

        private void GetCurrentPointObj()
        {
            Debug.Log("Get Current Point Obj");
            m_activePointObjects.TryGetValue(m_currentPointIndex , out MapLocationAction _mapLocationAction);
            m_currentPointObj = _mapLocationAction;
        }

        //Cache points, just in case (object pooling)
        private void CacheAllPreviousItems()
        {
            foreach (var _pointObjRef in m_activePointObjects)
            {
                m_cachedPointObjects.Add(_pointObjRef.Value.gameObject);
                _pointObjRef.Value.transform.parent = inactivePool;
            }

            foreach (var _connector in m_activePointConnectors)
            {
                m_cachedConnectors.Add(_connector);
                _connector.transform.parent = inactivePool;
            }
            
            m_activePointObjects.Clear();
            m_activePointConnectors.Clear();
        }


        //ToDo: This is set to completely random, this should be controlled randomness
        //Set rows, match row, item row, etc
        private GameplayEventType GetRandomEventType()
        {
            return possibleRandomEventTypes[Random.Range(0, possibleRandomEventTypes.Count)];
        }

        private GameplayEventType GetMiniBossType()
        {
            return miniBossEventType;
        }

        private GameplayEventType GetEventByGUID(string _searchGUID)
        {
            return allEventTypes.FirstOrDefault(get => get.eventGUID == _searchGUID);
        }

        private void InstantiatePointAt(Vector3 _instPosition, MapPointData _mapPointData, string _eventType = "")
        {
            GameObject go;
            
            if (m_cachedPointObjects.Count > 0)
            {
                go = m_cachedPointObjects.FirstOrDefault();
                go.transform.parent = mapGenParent;
                m_cachedPointObjects.Remove(go);
            }
            else
            {
                go = Instantiate(m_mapEventPrefab, mapGenParent);
            }
            
            go.transform.localPosition = _instPosition;

            
            //Initialize Point
            go.TryGetComponent(out MapLocationAction pointLocation);

            if (!pointLocation)
            {
                return;
            }
            
            pointLocation.Initialize(string.IsNullOrEmpty(_eventType) ? GetRandomEventType() 
                : GetEventByGUID(_eventType),
                _mapPointData);
            
            m_activePointObjects.Add(_mapPointData.pointID, pointLocation);
        }

        /// <summary>
        /// Connect points with line renderer
        /// </summary>
        /// <param name="_point1LocPos">Position 1</param>
        /// <param name="_point2LocPos">Position 2</param>
        private void ConnectPoints(Vector3 _point1LocPos, Vector3 _point2LocPos)
        {
            GameObject lineGo;
            
            if (m_cachedConnectors.Count > 0)
            {
                lineGo = m_cachedConnectors.FirstOrDefault();
                lineGo.transform.parent = mapGenParent;
                m_cachedConnectors.Remove(lineGo);
            }
            else
            {
                lineGo = Instantiate(connectorPrefab, mapGenParent);
            }
            
            lineGo.TryGetComponent(out LineRenderer _lineRenderer);

            if (_lineRenderer.IsNull())
            {
                return;
            }
            
            for (int i = 0; i < _lineRenderer.positionCount; i++)
            {
                _lineRenderer.SetPosition(i, i == 0 ? _point1LocPos : _point2LocPos);
            }

            m_activePointConnectors.Add(lineGo);
        }

        //Current Level Index
        public int GetCurrentLevel()
        {
            return m_currentLevel;
        }

        //Current Level Index increase
        public void IncreaseCurrentMapLevel()
        {
            m_currentLevel++;
        }

        private MapLocationAction GetPointAtIndex(int _pointIndex)
        {
            m_activePointObjects.TryGetValue(_pointIndex, out MapLocationAction _mapLocationAction);
            return _mapLocationAction.IsNull() ? default : _mapLocationAction;
        }

        [ContextMenu("Reset Run")]
        public void ResetAll()
        {
            m_currentLevel = 0;
            m_currentEventIdentifier = "";

            CacheAllPreviousItems();
            
            allCurrentRunLevels.Clear();
        }

        #endregion
        
        
        #region ISaveableData Inherited Methods

        public void LoadData(SavedGameData _savedGameData)
        {
            allCurrentRunLevels = _savedGameData.savedRunLevels;
            m_currentPointData = _savedGameData.lastSelectedPoint; 
            m_currentLevel = _savedGameData.levelIndex;
            m_currentPointIndex = _savedGameData.lastSelectedPoint.pointID;
        }

        public void SaveData(ref SavedGameData _savedGameData)
        {
            _savedGameData.savedRunLevels = allCurrentRunLevels;
            _savedGameData.lastSelectedPoint = m_currentPointData;
            _savedGameData.levelIndex = m_currentLevel;
        } 

        #endregion
        
    }
}