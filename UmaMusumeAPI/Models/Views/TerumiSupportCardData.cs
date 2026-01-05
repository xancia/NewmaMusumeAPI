using System;
using System.Collections.Generic;

namespace UmaMusumeAPI.Models.Views
{
    public class TerumiSupportCardData
    {
        public int SupportCardId { get; set; }
        public int CharaId { get; set; }
        public string CharaName { get; set; }
        public string SupportCardTitle { get; set; }
        public int Rarity { get; set; }
        public string RarityDisplay { get; set; }
        public int SupportCardType { get; set; }
        public string SupportCardTypeName { get; set; }
        public int EffectTableId { get; set; }
        public int UniqueEffectId { get; set; }
        public int SkillSetId { get; set; }
        public int CommandType { get; set; }
        public int CommandId { get; set; }
        public DateTime? StartDate { get; set; }
        public int OutingMax { get; set; }
        public int EffectId { get; set; }

        // Effect values at each limit break level
        public List<SupportCardEffect> Effects { get; set; }

        // Skill hints available on this card
        public List<SkillHint> SkillHints { get; set; }

        // Support card events (chain events)
        public List<EventDetail> Events { get; set; }

        // Unique effect details
        public UniqueEffectDetail UniqueEffect { get; set; }
    }

    public class SupportCardEffect
    {
        public int EffectType { get; set; }
        public string EffectTypeName { get; set; }
        public int InitValue { get; set; }
        public int Level5Value { get; set; }
        public int Level10Value { get; set; }
        public int Level15Value { get; set; }
        public int Level20Value { get; set; }
        public int Level25Value { get; set; }
        public int Level30Value { get; set; }
        public int Level35Value { get; set; }
        public int Level40Value { get; set; }
        public int Level45Value { get; set; }
        public int Level50Value { get; set; }
    }

    public class SkillHint
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int SkillLevel { get; set; }
    }

    public class EventDetail
    {
        public int StoryId { get; set; }
        public string EventTitle { get; set; }
        public string EventType { get; set; } // "Chain Event" or "Random Event"
        public int EventOrder { get; set; } // 1, 2, 3 for chain events; 0 for random events
    }

    public class UniqueEffectDetail
    {
        public int Level { get; set; }
        public int Type0 { get; set; }
        public string Type0Name { get; set; }
        public int Value0 { get; set; }
        public int Value01 { get; set; }
        public int Value02 { get; set; }
        public int Value03 { get; set; }
        public int Value04 { get; set; }
        public int Type1 { get; set; }
        public string Type1Name { get; set; }
        public int Value1 { get; set; }
        public int Value11 { get; set; }
        public int Value12 { get; set; }
        public int Value13 { get; set; }
        public int Value14 { get; set; }
    }
}
