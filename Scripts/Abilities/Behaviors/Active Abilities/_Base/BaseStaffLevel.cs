using UnityEngine;
using OctoberStudio.Abilities;

namespace OctoberStudio.Abilities
{
    [System.Serializable]
    public abstract class BaseStaffLevel : BaseMagicLevel
    {
        [Header("Staff Specific")]
        [SerializeField] protected float channelTime = 1f;
        public float ChannelTime => channelTime;
    }
}