using System;

namespace UmaMusumeAPI.Models.Views
{
    public class TerumiCharacterData
    {
        public int CharaId { get; set; }
        public string CharaName { get; set; }
        public string VoiceActor { get; set; }
        public int? CardId { get; set; }
        public string CardTitle { get; set; }
        public int? SupportCardId { get; set; }
        public DateTime? StartDate { get; set; }

        // Base Stats (5-star)
        public int? BaseSpeed { get; set; }
        public int? BaseStamina { get; set; }
        public int? BasePower { get; set; }
        public int? BaseGuts { get; set; }
        public int? BaseWisdom { get; set; }

        // Talent (Growth Rate)
        public int? TalentSpeed { get; set; }
        public int? TalentStamina { get; set; }
        public int? TalentPower { get; set; }
        public int? TalentGuts { get; set; }
        public int? TalentWisdom { get; set; }

        // Aptitudes (Ground)
        public string AptitudeTurf { get; set; }
        public string AptitudeDirt { get; set; }

        // Aptitudes (Distance)
        public string AptitudeShort { get; set; }
        public string AptitudeMile { get; set; }
        public string AptitudeMiddle { get; set; }
        public string AptitudeLong { get; set; }

        // Aptitudes (Running Style)
        public string AptitudeRunner { get; set; } // Nige (Escape/Runner)
        public string AptitudeLeader { get; set; } // Senko (Lead/Leader)
        public string AptitudeBetweener { get; set; } // Sashi (Insert/Betweener)
        public string AptitudeChaser { get; set; } // Oikomi (Stalker/Chaser)

        // Bio Data
        public int? BirthYear { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthDay { get; set; }
        public int? Sex { get; set; }
        public int? Height { get; set; }
        public int? Bust { get; set; }

        // Character Category
        public int? CharaCategory { get; set; }

        // URA Objectives
        public int? UraObjectives { get; set; }

        // Skills
        public string SkillIds { get; set; }
    }
}
