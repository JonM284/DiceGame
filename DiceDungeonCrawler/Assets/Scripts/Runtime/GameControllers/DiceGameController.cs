using System.Collections.Generic;
using System.Linq;
using Data.DataSaving;
using Data.Dice;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.Dice;
using Runtime.Dice.Enums;
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

        private List<DieWrapperData> savedRosterDice = new List<DieWrapperData>();
        private List<DieWrapperData> savedInventoryDice = new List<DieWrapperData>();
        private List<ModDieWrapperData> savedModifierDice = new List<ModDieWrapperData>();

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

        public List<DieWrapperData> GetRosterDiceData()
        {
            if (savedRosterDice.IsNull() || savedRosterDice.Count == 0)
            {
                var _newSet = GetDiceSet(0);
                return _newSet;
            }
            
            return savedRosterDice.ToNewList();
        }

        private List<DieWrapperData> GetDiceSet(int _index)
        {
            if (m_allStartingSets.Count < _index || m_allStartingSets[_index].IsNull())
            {
                return default;
            }

            return m_allStartingSets[_index].m_startingDice.Select(_dieData => new DieWrapperData(_dieData.valuesPerSide.ToList(), TintType.NONE)).ToList();
        }

        private void SearchForDiceDetails()
        {
            List<DieWrapperData> references = new List<DieWrapperData>();
            foreach (var _die in savedRosterDice)
            {
                references.Add(new DieWrapperData(_die.faceValues, _die.tintType));
            }

            savedRosterDice = CommonUtils.ToNewList(references);
        }

        public DieData GetDieByGUID(string _searchGUID)
        {
            return m_allDiceDatas.FirstOrDefault(csb => csb.itemGuid == _searchGUID);
        }

        public DieData GetDieByAmountOfSides(int _amountOfSides)
        {
            return m_allDiceDatas.FirstOrDefault(dd => dd.valuesPerSide.Count == _amountOfSides);
        }

        #endregion


        #region ISavableData Inherited Methods

        public void LoadData(SavedGameData _savedGameData)
        {
            savedRosterDice = _savedGameData.m_savedDiceRoster;
            savedInventoryDice = _savedGameData.m_savedInventory;
            savedModifierDice = _savedGameData.m_savedModDice;
            SearchForDiceDetails();
        }

        public void SaveData(ref SavedGameData _savedGameData)
        {
            _savedGameData.m_savedDiceRoster = savedRosterDice;
            _savedGameData.m_savedInventory = savedInventoryDice;
            _savedGameData.m_savedModDice = savedModifierDice;
        }

        #endregion
       
    }
}