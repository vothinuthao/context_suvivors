using System;
using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;

namespace Talents
{
    [System.Serializable]
    public class TalentSave : ISave
    {
        [SerializeField] TalentProgressSave[] savedTalents;

        private Dictionary<int, int> _talentLevels;

        public void Init()
        {
            _talentLevels = new Dictionary<int, int>();

            if (savedTalents == null) savedTalents = Array.Empty<TalentProgressSave>();

            for (int i = 0; i < savedTalents.Length; i++)
            {
                var save = savedTalents[i];

                if (_talentLevels.ContainsKey(save.TalentID))
                {
                    var savedLevel = _talentLevels[save.TalentID];

                    if (save.Level > savedLevel)
                    {
                        _talentLevels[save.TalentID] = save.Level;
                    }
                }
                else
                {
                    _talentLevels.Add(save.TalentID, save.Level);
                }
            }
        }

        public int GetTalentLevel(int talentID)
        {
            if (_talentLevels.ContainsKey(talentID))
            {
                return _talentLevels[talentID];
            }
            else
            {
                return 0; // Default level is 0 (not learned)
            }
        }

        public void SetTalentLevel(int talentID, int level)
        {
            if (_talentLevels.ContainsKey(talentID))
            {
                _talentLevels[talentID] = level;
            }
            else
            {
                _talentLevels.Add(talentID, level);
            }
        }

        public bool IsTalentLearned(int talentID)
        {
            return GetTalentLevel(talentID) > 0;
        }

        public void RemoveTalent(int talentID)
        {
            if (_talentLevels.ContainsKey(talentID))
            {
                _talentLevels.Remove(talentID);
            }
        }

        public void Flush()
        {
            savedTalents = new TalentProgressSave[_talentLevels.Count];

            int i = 0;

            foreach (var talent in _talentLevels.Keys)
            {
                var talentSave = new TalentProgressSave(talent, _talentLevels[talent]);
                savedTalents[i++] = talentSave;
            }
        }

        public void Clear()
        {
            _talentLevels.Clear();
        }

        public Dictionary<int, int> GetAllTalents()
        {
            return new Dictionary<int, int>(_talentLevels);
        }

        [System.Serializable]
        private class TalentProgressSave
        {
            [SerializeField] int talentID;
            [SerializeField] int level;

            public int TalentID => talentID;
            public int Level => level;

            public TalentProgressSave(int talentID, int level)
            {
                this.talentID = talentID;
                this.level = level;
            }
        }
    }
}