using System.Collections.Generic;

namespace UmaMusumeAPI.Models.Views
{
    public class TerumiSimpleSkillData
    {
        public int SkillId { get; set; }
        public int Rarity { get; set; }
        public int GradeValue { get; set; }
        public string SkillCategory { get; set; }
        public string TagId { get; set; }
        public string ActivationCondition { get; set; }
        public string Precondition { get; set; }
        public List<SkillEffect> Effects { get; set; }
        public string EffectSummary { get; set; }
        public int IconId { get; set; }
        public string SkillName { get; set; }
        public string SkillDesc { get; set; }
        public int NeedSkillPoint { get; set; }
        public decimal? Duration { get; set; }
        public decimal? CooldownTime { get; set; }
        public string SupportCardIds { get; set; }

        // Hybrid skill fields (condition 2)
        public string ActivationCondition2 { get; set; }
        public string Precondition2 { get; set; }
        public List<SkillEffect> Effects2 { get; set; }
        public string EffectSummary2 { get; set; }
        public decimal? Duration2 { get; set; }
        public decimal? CooldownTime2 { get; set; }
    }

    public class SkillEffect
    {
        public string Type { get; set; }
        public decimal Value { get; set; }
        public string DisplayText { get; set; }
    }
}
