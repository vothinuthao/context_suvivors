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

        public UnityAction onSelectedCharacterChanged;

        public int SelectedCharacterId => selectedCharacterId;

        protected List<int> BoughtCharacterIds { get; set; }

        public virtual void Init()
        {
            if (boughtCharacterIds == null)
            {
                boughtCharacterIds = new int[] { 0 };

                selectedCharacterId = 0;
            }
            BoughtCharacterIds = new List<int>(boughtCharacterIds);
        }

        public virtual bool HasCharacterBeenBought(int id)
        {
            if (BoughtCharacterIds == null) Init();

            return BoughtCharacterIds.Contains(id);
        }

        public virtual void AddBoughtCharacter(int id)
        {
            if (BoughtCharacterIds == null) Init();

            BoughtCharacterIds.Add(id);
        }

        public virtual void SetSelectedCharacterId(int id)
        {
            if (BoughtCharacterIds == null) Init();

            selectedCharacterId = id;

            onSelectedCharacterChanged?.Invoke();
        }

        public virtual void Flush()
        {
            if (BoughtCharacterIds == null) Init();

            boughtCharacterIds = BoughtCharacterIds.ToArray();
        }
    }
}