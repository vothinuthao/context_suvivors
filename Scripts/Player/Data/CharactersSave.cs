using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class CharactersSave : ISave
    {
        [SerializeField] protected int[] boughtCharacterIds;
        [SerializeField] protected int selectedCharacterId;
        [SerializeField] protected CharacterStarData[] characterStarLevels;
        [SerializeField] protected CharacterLevelData[] characterLevels;
        [SerializeField] protected CharacterPiecesData[] characterPieces;
        [SerializeField] protected CharacterSubStarData[] characterSubStars;

        public UnityAction onSelectedCharacterChanged;
        public UnityAction<int> onCharacterUpgraded;

        public int SelectedCharacterId => selectedCharacterId;

        protected List<int> BoughtCharacterIds { get; set; }
        protected Dictionary<int, int> CharacterStarLevels { get; set; }
        protected Dictionary<int, int> CharacterLevels { get; set; }
        protected Dictionary<int, int> CharacterPieces { get; set; }
        protected Dictionary<int, int> CharacterSubStars { get; set; } // Tracks sub-star progress within current star tier

        public virtual void Init()
        {
            if (boughtCharacterIds == null)
            {
                boughtCharacterIds = new int[] { 0 };
                selectedCharacterId = 0;
            }
            BoughtCharacterIds = new List<int>(boughtCharacterIds);

            if (characterStarLevels == null)
            {
                characterStarLevels = new CharacterStarData[0];
            }

            CharacterStarLevels = new Dictionary<int, int>();
            for (int i = 0; i < characterStarLevels.Length; i++)
            {
                CharacterStarLevels[characterStarLevels[i].characterId] = characterStarLevels[i].starLevel;
            }

            if (characterLevels == null)
            {
                characterLevels = new CharacterLevelData[0];
            }

            CharacterLevels = new Dictionary<int, int>();
            for (int i = 0; i < characterLevels.Length; i++)
            {
                CharacterLevels[characterLevels[i].characterId] = characterLevels[i].level;
            }

            if (characterPieces == null)
            {
                characterPieces = new CharacterPiecesData[0];
            }

            CharacterPieces = new Dictionary<int, int>();
            for (int i = 0; i < characterPieces.Length; i++)
            {
                CharacterPieces[characterPieces[i].characterId] = characterPieces[i].pieces;
            }

            if (characterSubStars == null)
            {
                characterSubStars = new CharacterSubStarData[0];
            }

            CharacterSubStars = new Dictionary<int, int>();
            for (int i = 0; i < characterSubStars.Length; i++)
            {
                CharacterSubStars[characterSubStars[i].characterId] = characterSubStars[i].subStarProgress;
            }
        }

        public virtual bool HasCharacterBeenBought(int id)
        {
            if (BoughtCharacterIds == null) Init();

            return BoughtCharacterIds.Contains(id);
        }

        public virtual void AddBoughtCharacter(int id)
        {
            if (BoughtCharacterIds == null) Init();

            if (!BoughtCharacterIds.Contains(id))
            {
                BoughtCharacterIds.Add(id);
            }
        }

        public virtual void ClearBoughtCharacters()
        {
            if (BoughtCharacterIds == null) Init();

            BoughtCharacterIds.Clear();
            // Keep the default character (index 0)
            BoughtCharacterIds.Add(0);

            // Reset selected character to default
            selectedCharacterId = 0;
            onSelectedCharacterChanged?.Invoke();
        }

        public virtual void SetSelectedCharacterId(int id)
        {
            if (BoughtCharacterIds == null) Init();

            selectedCharacterId = id;

            onSelectedCharacterChanged?.Invoke();
        }

        public virtual int GetCharacterStarLevel(int characterId)
        {
            if (CharacterStarLevels == null) Init();

            if (CharacterStarLevels.ContainsKey(characterId))
            {
                return CharacterStarLevels[characterId];
            }
            return 1; // Characters always start at star level 1, not 0
        }

        public virtual void UpgradeCharacterStar(int characterId)
        {
            if (CharacterStarLevels == null) Init();

            if (CharacterStarLevels.ContainsKey(characterId))
            {
                CharacterStarLevels[characterId]++;
            }
            else
            {
                CharacterStarLevels[characterId] = 2; // If upgrading from default (1), go to 2
            }

            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual bool CanUpgradeCharacter(int characterId, int maxStars)
        {
            return GetCharacterStarLevel(characterId) < maxStars;
        }

        // New character level management methods
        public virtual int GetCharacterLevel(int characterId)
        {
            if (CharacterLevels == null) Init();

            if (CharacterLevels.ContainsKey(characterId))
            {
                return CharacterLevels[characterId];
            }
            return 1; // Default level is 1
        }

        public virtual void UpgradeCharacterLevel(int characterId)
        {
            if (CharacterLevels == null) Init();

            if (CharacterLevels.ContainsKey(characterId))
            {
                CharacterLevels[characterId]++;
            }
            else
            {
                CharacterLevels[characterId] = 2; // Start from level 2 if not exists
            }

            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual bool CanUpgradeCharacterLevel(int characterId, int maxLevel)
        {
            return GetCharacterLevel(characterId) < maxLevel;
        }

        // Character pieces management methods
        public virtual int GetCharacterPieces(int characterId)
        {
            if (CharacterPieces == null) Init();

            if (CharacterPieces.ContainsKey(characterId))
            {
                return CharacterPieces[characterId];
            }
            return 0; // Default pieces is 0
        }

        public virtual void AddCharacterPieces(int characterId, int amount)
        {
            if (CharacterPieces == null) Init();

            if (CharacterPieces.ContainsKey(characterId))
            {
                CharacterPieces[characterId] += amount;
            }
            else
            {
                CharacterPieces[characterId] = amount;
            }

            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual bool TrySpendCharacterPieces(int characterId, int amount)
        {
            if (CharacterPieces == null) Init();

            int currentPieces = GetCharacterPieces(characterId);
            if (currentPieces >= amount)
            {
                CharacterPieces[characterId] = currentPieces - amount;
                onCharacterUpgraded?.Invoke(characterId);
                return true;
            }
            return false;
        }

        public virtual bool CanAffordCharacterPieces(int characterId, int amount)
        {
            return GetCharacterPieces(characterId) >= amount;
        }

        // Sub-star progress management methods
        public virtual int GetCharacterSubStarProgress(int characterId)
        {
            if (CharacterSubStars == null) Init();

            if (CharacterSubStars.ContainsKey(characterId))
            {
                return CharacterSubStars[characterId];
            }
            return 0; // Default sub-star progress is 0
        }

        public virtual void SetCharacterSubStarProgress(int characterId, int subStarProgress)
        {
            if (CharacterSubStars == null) Init();

            CharacterSubStars[characterId] = subStarProgress;
            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual void IncrementCharacterSubStarProgress(int characterId)
        {
            if (CharacterSubStars == null) Init();

            int currentProgress = GetCharacterSubStarProgress(characterId);
            CharacterSubStars[characterId] = currentProgress + 1;
            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual void ResetCharacterSubStarProgress(int characterId)
        {
            if (CharacterSubStars == null) Init();

            CharacterSubStars[characterId] = 0;
            onCharacterUpgraded?.Invoke(characterId);
        }

        public virtual void Flush()
        {
            if (BoughtCharacterIds == null) Init();
            if (CharacterStarLevels == null) Init();
            if (CharacterLevels == null) Init();
            if (CharacterPieces == null) Init();
            if (CharacterSubStars == null) Init();

            boughtCharacterIds = BoughtCharacterIds.ToArray();

            // Convert dictionary back to array for serialization
            var starDataList = new List<CharacterStarData>();
            foreach (var kvp in CharacterStarLevels)
            {
                starDataList.Add(new CharacterStarData { characterId = kvp.Key, starLevel = kvp.Value });
            }
            characterStarLevels = starDataList.ToArray();

            // Convert character levels dictionary back to array for serialization
            var levelDataList = new List<CharacterLevelData>();
            foreach (var kvp in CharacterLevels)
            {
                levelDataList.Add(new CharacterLevelData { characterId = kvp.Key, level = kvp.Value });
            }
            characterLevels = levelDataList.ToArray();

            // Convert character pieces dictionary back to array for serialization
            var piecesDataList = new List<CharacterPiecesData>();
            foreach (var kvp in CharacterPieces)
            {
                piecesDataList.Add(new CharacterPiecesData { characterId = kvp.Key, pieces = kvp.Value });
            }
            characterPieces = piecesDataList.ToArray();

            // Convert character sub-stars dictionary back to array for serialization
            var subStarDataList = new List<CharacterSubStarData>();
            foreach (var kvp in CharacterSubStars)
            {
                subStarDataList.Add(new CharacterSubStarData { characterId = kvp.Key, subStarProgress = kvp.Value });
            }
            characterSubStars = subStarDataList.ToArray();
        }
    }

    [System.Serializable]
    public struct CharacterStarData
    {
        public int characterId;
        public int starLevel;
    }

    [System.Serializable]
    public struct CharacterLevelData
    {
        public int characterId;
        public int level;
    }

    [System.Serializable]
    public struct CharacterPiecesData
    {
        public int characterId;
        public int pieces;
    }

    [System.Serializable]
    public struct CharacterSubStarData
    {
        public int characterId;
        public int subStarProgress; // Number of sub-stars completed in current star tier
    }
}