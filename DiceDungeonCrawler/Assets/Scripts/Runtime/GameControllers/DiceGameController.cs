using System.Collections.Generic;
using System.Linq;
using Data.DataSaving;
using Data.Dice;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.Dice;
using UnityEngine;
using UnityEngine.Serialization;

namespace Runtime.GameControllers
{
    public class DiceGameController: GameControllerBase, ISaveableData
    {
        
        
        #region Static

        public static DiceGameController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private List<Sprite> m_allPossibleDieFaceNumbers = new List<Sprite>();
        
        [SerializeField] private List<DieData> m_allDiceDatas = new List<DieData>();

        [SerializeField] private List<StartingDiceSets> m_allStartingSets = new List<StartingDiceSets>();

        #endregion

        #region Private Fields

        private List<SavedDiceData> m_rosterDiceData = new List<SavedDiceData>();
        private List<SavedDiceData> m_inventoryDiceData = new List<SavedDiceData>();
        private List<SavedDiceData> m_perkDiceData = new List<SavedDiceData>();

        #endregion
        
        
        #region GameControllerBase Inherited Methods
        
        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion

        #region Class Implementation

        public Sprite GetCorrectNumber(int _inputNumber)
        {
            return _inputNumber is < 0 or > 100 ? m_allPossibleDieFaceNumbers[0] : m_allPossibleDieFaceNumbers[_inputNumber - 1];
        }

        public List<SavedDiceData> GetRosterDiceData()
        {
            if (m_rosterDiceData.IsNull() || m_rosterDiceData.Count == 0)
            {
                var _newSet = GetDiceSet(0);
                return _newSet;
            }
            
            return m_rosterDiceData.ToNewList();
        }

        private List<SavedDiceData> GetDiceSet(int _index)
        {
            if (m_allStartingSets.Count < _index || m_allStartingSets[_index].IsNull())
            {
                return default;
            }

            return m_allStartingSets[_index].m_startingDice.Select(_dieData => new SavedDiceData(_dieData.dieGuid)).ToList();
        }

        private void SearchForDiceDetails()
        {
            List<SavedDiceData> references = new List<SavedDiceData>();
            foreach (var _die in m_rosterDiceData)
            {
                references.Add(new SavedDiceData(_die.guid));
            }

            m_rosterDiceData = CommonUtils.ToNewList(references);
        }

        public DieData GetDieByGUID(string _searchGUID)
        {
            return m_allDiceDatas.FirstOrDefault(csb => csb.dieGuid == _searchGUID);
        }

        #endregion


        #region ISavableData Inherited Methods

        public void LoadData(SavedGameData _savedGameData)
        {
            m_rosterDiceData = _savedGameData.m_savedDiceRoster;
            m_inventoryDiceData = _savedGameData.m_savedInventory;
            m_perkDiceData = _savedGameData.m_savedPerkDice;
            SearchForDiceDetails();
        }

        public void SaveData(ref SavedGameData _savedGameData)
        {
            _savedGameData.m_savedDiceRoster = m_rosterDiceData;
            _savedGameData.m_savedInventory = m_inventoryDiceData;
            _savedGameData.m_savedPerkDice = m_perkDiceData;
        }

        #endregion
       
    }
}