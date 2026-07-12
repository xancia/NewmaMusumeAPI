using System;
using System.Collections.Generic;

namespace UmaMusumeAPI.Models.Views
{
    public class TerumiUmaOutfit
    {
        public int? CardId { get; set; } // null for outfits not tied to a card (e.g. casual wear)
        public string CardTitle { get; set; }
        public int DressId { get; set; }
        public string DressName { get; set; }
        public string DressDescription { get; set; }
    }

    public class TerumiUmaSong
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Credits { get; set; }
        public string Description { get; set; }
        public bool IsSoloSong { get; set; } // this uma is the only permitted singer
        public bool AllCharasSong { get; set; } // every uma can sing this song (e.g. Umapyoi Legend)
        public bool HasLivePerformance { get; set; }
        public int LiveMemberCount { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }

    public class TerumiValentineGift
    {
        public int GiftId { get; set; }
        public string GiftName { get; set; }
        public string MessageMaleTrainer { get; set; }
        public string MessageFemaleTrainer { get; set; }
    }

    public class TerumiLegendRaceTrophy
    {
        public int RaceInstanceId { get; set; }
        public string RaceName { get; set; }
        public int? TrophyId { get; set; }
        public int? RewardDressId { get; set; } // first-clear reward: the uma's racing outfit
        public string RewardDressName { get; set; }
    }

    public class TerumiCasualUmaData
    {
        public int CharaId { get; set; }
        public string CharaName { get; set; }

        // One entry per playable card (alternate versions), each with its racing outfit
        public List<TerumiUmaOutfit> Outfits { get; set; } = new();

        // Casual/private wear (dress_data condition_type = 6), null if not released yet
        public TerumiUmaOutfit CasualOutfit { get; set; }

        // Songs this uma has vocals for (live permissions + all-cast songs);
        // IsSoloSong marks the uma's solo song
        public List<TerumiUmaSong> Songs { get; set; } = new();

        // Valentine's Day gifts (name + message per variant)
        public List<TerumiValentineGift> ValentineGifts { get; set; } = new();

        // Legend Race appearances as the boss, with trophy and first-clear outfit reward
        public List<TerumiLegendRaceTrophy> LegendRaceTrophies { get; set; } = new();
    }
}
