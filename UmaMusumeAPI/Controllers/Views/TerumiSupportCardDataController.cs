using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using UmaMusumeAPI.Context;
using UmaMusumeAPI.Models.Views;

namespace UmaMusumeAPI.Controllers.Views
{
    [Route("api/[controller]")]
    [ApiController]
    public class TerumiSupportCardDataController : ControllerBase
    {
        private readonly UmaMusumeDbContext _context;

        public TerumiSupportCardDataController(UmaMusumeDbContext context)
        {
            _context = context;
        }

        // GET: api/TerumiSupportCardData
        /// <summary>
        /// Get all support cards with detailed information including effects at each level, skill hints, and unique effects
        /// </summary>
        [HttpGet]
        public async Task<
            ActionResult<IEnumerable<TerumiSupportCardData>>
        > GetTerumiSupportCardData()
        {
            var result = new List<TerumiSupportCardData>();

            var query =
                @"
                SELECT 
                    scd.id as SupportCardId,
                    scd.chara_id as CharaId,
                    t_chara.text as CharaName,
                    t_card.text as SupportCardTitle,
                    scd.rarity as Rarity,
                    scd.support_card_type as SupportCardType,
                    scd.effect_table_id as EffectTableId,
                    scd.unique_effect_id as UniqueEffectId,
                    scd.skill_set_id as SkillSetId,
                    scd.command_type as CommandType,
                    scd.command_id as CommandId,
                    FROM_UNIXTIME_SECONDS(scd.start_date) as StartDate,
                    scd.outing_max as OutingMax,
                    scd.effect_id as EffectId
                FROM support_card_data scd
                LEFT JOIN chara_data cd ON scd.chara_id = cd.id
                LEFT JOIN text_data t_chara ON t_chara.`index` = cd.id AND t_chara.category = 6
                LEFT JOIN text_data t_card ON t_card.`index` = scd.id AND t_card.category = 76
                WHERE t_card.text IS NOT NULL
                ORDER BY scd.id";

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                await _context.Database.OpenConnectionAsync();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var supportCardId = reader.GetInt32(reader.GetOrdinal("SupportCardId"));
                        var rarity = reader.GetInt32(reader.GetOrdinal("Rarity"));
                        var supportCardType = reader.GetInt32(reader.GetOrdinal("SupportCardType"));
                        var effectTableId = reader.GetInt32(reader.GetOrdinal("EffectTableId"));
                        var skillSetId = reader.GetInt32(reader.GetOrdinal("SkillSetId"));
                        var uniqueEffectId = reader.GetInt32(reader.GetOrdinal("UniqueEffectId"));

                        var card = new TerumiSupportCardData
                        {
                            SupportCardId = supportCardId,
                            CharaId = reader.GetInt32(reader.GetOrdinal("CharaId")),
                            CharaName = reader.IsDBNull(reader.GetOrdinal("CharaName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("CharaName")),
                            SupportCardTitle = reader.IsDBNull(
                                reader.GetOrdinal("SupportCardTitle")
                            )
                                ? null
                                : reader.GetString(reader.GetOrdinal("SupportCardTitle")),
                            Rarity = rarity,
                            RarityDisplay = GetRarityDisplay(rarity),
                            SupportCardType = supportCardType,
                            SupportCardTypeName = GetSupportCardTypeName(supportCardType),
                            EffectTableId = effectTableId,
                            UniqueEffectId = uniqueEffectId,
                            SkillSetId = skillSetId,
                            CommandType = reader.GetInt32(reader.GetOrdinal("CommandType")),
                            CommandId = reader.GetInt32(reader.GetOrdinal("CommandId")),
                            StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("StartDate")),
                            OutingMax = reader.GetInt32(reader.GetOrdinal("OutingMax")),
                            EffectId = reader.GetInt32(reader.GetOrdinal("EffectId")),
                            Effects = new List<SupportCardEffect>(),
                            SkillHints = new List<SkillHint>(),
                        };

                        result.Add(card);
                    }
                }
            }

            // Now fetch effects, skill hints, events, and unique effects for all cards
            foreach (var card in result)
            {
                card.Effects = await GetSupportCardEffects(card.EffectTableId);
                card.SkillHints = await GetSkillHints(card.SkillSetId);
                card.Events = await GetEvents(card.SupportCardId, card.CharaId);
                card.UniqueEffect = await GetUniqueEffect(card.UniqueEffectId);
            }

            return result;
        }

        // GET: api/TerumiSupportCardData/30001
        /// <summary>
        /// Get a specific support card by ID with all detailed information
        /// </summary>
        [HttpGet("{supportCardId}")]
        public async Task<ActionResult<TerumiSupportCardData>> GetTerumiSupportCardData(
            int supportCardId
        )
        {
            var query =
                @"
                SELECT 
                    scd.id as SupportCardId,
                    scd.chara_id as CharaId,
                    t_chara.text as CharaName,
                    t_card.text as SupportCardTitle,
                    scd.rarity as Rarity,
                    scd.support_card_type as SupportCardType,
                    scd.effect_table_id as EffectTableId,
                    scd.unique_effect_id as UniqueEffectId,
                    scd.skill_set_id as SkillSetId,
                    scd.command_type as CommandType,
                    scd.command_id as CommandId,
                    FROM_UNIXTIME_SECONDS(scd.start_date) as StartDate,
                    scd.outing_max as OutingMax,
                    scd.effect_id as EffectId
                FROM support_card_data scd
                LEFT JOIN chara_data cd ON scd.chara_id = cd.id
                LEFT JOIN text_data t_chara ON t_chara.`index` = cd.id AND t_chara.category = 6
                LEFT JOIN text_data t_card ON t_card.`index` = scd.id AND t_card.category = 76
                WHERE scd.id = @supportCardId AND t_card.text IS NOT NULL";

            TerumiSupportCardData card = null;
            int effectTableId = 0;
            int uniqueEffectId = 0;
            int commandId = 0;
            int skillSetId = 0;
            int charaId = 0;

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@supportCardId", supportCardId));
                await _context.Database.OpenConnectionAsync();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var rarity = reader.GetInt32(reader.GetOrdinal("Rarity"));
                        var supportCardType = reader.GetInt32(reader.GetOrdinal("SupportCardType"));
                        effectTableId = reader.GetInt32(reader.GetOrdinal("EffectTableId"));
                        skillSetId = reader.GetInt32(reader.GetOrdinal("SkillSetId"));
                        uniqueEffectId = reader.GetInt32(reader.GetOrdinal("UniqueEffectId"));
                        commandId = reader.GetInt32(reader.GetOrdinal("CommandId"));
                        charaId = reader.GetInt32(reader.GetOrdinal("CharaId"));

                        card = new TerumiSupportCardData
                        {
                            SupportCardId = reader.GetInt32(reader.GetOrdinal("SupportCardId")),
                            CharaId = reader.GetInt32(reader.GetOrdinal("CharaId")),
                            CharaName = reader.IsDBNull(reader.GetOrdinal("CharaName"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("CharaName")),
                            SupportCardTitle = reader.IsDBNull(
                                reader.GetOrdinal("SupportCardTitle")
                            )
                                ? null
                                : reader.GetString(reader.GetOrdinal("SupportCardTitle")),
                            Rarity = rarity,
                            RarityDisplay = GetRarityDisplay(rarity),
                            SupportCardType = supportCardType,
                            SupportCardTypeName = GetSupportCardTypeName(supportCardType),
                            EffectTableId = effectTableId,
                            UniqueEffectId = uniqueEffectId,
                            SkillSetId = skillSetId,
                            CommandType = reader.GetInt32(reader.GetOrdinal("CommandType")),
                            CommandId = commandId,
                            StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("StartDate")),
                            OutingMax = reader.GetInt32(reader.GetOrdinal("OutingMax")),
                            EffectId = reader.GetInt32(reader.GetOrdinal("EffectId")),
                        };
                    }
                }
            }

            // Fetch related data after closing the reader
            if (card != null)
            {
                card.Effects = await GetSupportCardEffects(effectTableId);
                card.SkillHints = await GetSkillHints(skillSetId);
                card.Events = await GetEvents(supportCardId, charaId);
                card.UniqueEffect = await GetUniqueEffect(uniqueEffectId);
            }

            if (card == null)
                return NotFound();

            return card;
        }

        private async Task<List<SupportCardEffect>> GetSupportCardEffects(int effectTableId)
        {
            var effects = new List<SupportCardEffect>();

            var query =
                @"
                SELECT 
                    type, init, limit_lv5, limit_lv10, limit_lv15, limit_lv20, 
                    limit_lv25, limit_lv30, limit_lv35, limit_lv40, limit_lv45, limit_lv50
                FROM support_card_effect_table
                WHERE id = @effectTableId";

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@effectTableId", effectTableId));

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var effectType = reader.GetInt32(0);
                        effects.Add(
                            new SupportCardEffect
                            {
                                EffectType = effectType,
                                EffectTypeName = GetEffectTypeName(effectType),
                                InitValue = reader.GetInt32(1),
                                Level5Value = reader.GetInt32(2),
                                Level10Value = reader.GetInt32(3),
                                Level15Value = reader.GetInt32(4),
                                Level20Value = reader.GetInt32(5),
                                Level25Value = reader.GetInt32(6),
                                Level30Value = reader.GetInt32(7),
                                Level35Value = reader.GetInt32(8),
                                Level40Value = reader.GetInt32(9),
                                Level45Value = reader.GetInt32(10),
                                Level50Value = reader.GetInt32(11),
                            }
                        );
                    }
                }
            }

            return effects;
        }

        private async Task<List<SkillHint>> GetSkillHints(int skillSetId)
        {
            var hints = new List<SkillHint>();

            if (skillSetId == 0)
                return hints;

            // Query single_mode_hint_gain for both skill hints (type 0) and stat gains (type 1)
            var query =
                @"
                SELECT hint_gain_type, hint_value_1, hint_value_2
                FROM single_mode_hint_gain
                WHERE hint_id = @skillSetId
                ORDER BY hint_gain_type, hint_group";

            var skillIds = new List<int>();
            var statGains = new List<(int statType, int amount)>();

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@skillSetId", skillSetId));

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var hintType = reader.GetInt32(0); // hint_gain_type
                        var value1 = reader.GetInt32(1); // hint_value_1
                        var value2 = reader.GetInt32(2); // hint_value_2

                        if (hintType == 0)
                        {
                            // Skill hint
                            if (value1 > 0 && !skillIds.Contains(value1))
                            {
                                skillIds.Add(value1);
                            }
                        }
                        else if (hintType == 1)
                        {
                            // Stat gain event - check for duplicates
                            var statGain = (value1, value2);
                            if (!statGains.Contains(statGain))
                            {
                                statGains.Add(statGain);
                            }
                        }
                    }
                }
            }

            // Add skill hints
            foreach (var skillId in skillIds)
            {
                var skillName = await GetSkillName(skillId);
                hints.Add(
                    new SkillHint
                    {
                        SkillId = skillId,
                        SkillName = skillName,
                        SkillLevel = 0, // Hints don't have levels initially
                    }
                );
            }

            // Add stat gain events
            foreach (var (statType, amount) in statGains)
            {
                var statName = GetStatName(statType);
                hints.Add(
                    new SkillHint
                    {
                        SkillId = 0,
                        SkillName = $"+{amount} {statName}",
                        SkillLevel = 0,
                    }
                );
            }

            return hints;
        }

        private string GetStatName(int statType)
        {
            return statType switch
            {
                1 => "Speed",
                2 => "Stamina",
                3 => "Power",
                4 => "Guts",
                5 => "Wits",
                30 => "Skill Pt",
                _ => $"Unknown({statType})",
            };
        }

        private async Task<string> GetSkillName(int skillId)
        {
            var query =
                @"
                SELECT text 
                FROM text_data 
                WHERE text_data.index = @skillId AND category = 47 
                LIMIT 1";

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@skillId", skillId));

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
        }

        private async Task<List<EventDetail>> GetEvents(int supportCardId, int charaId)
        {
            var events = new List<EventDetail>();

            // Get both card-specific chain events AND character-shared random events
            var query =
                @"
                SELECT s.story_id, t.text as event_title, s.support_card_id, s.show_progress_1
                FROM single_mode_story_data s
                LEFT JOIN text_data t ON s.story_id = t.`index` AND t.category = 181
                WHERE (s.support_card_id = @supportCardId) 
                   OR (s.support_chara_id = @charaId AND s.support_card_id = 0)
                ORDER BY s.support_card_id DESC, s.show_progress_1";

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@supportCardId", supportCardId));
                command.Parameters.Add(new MySqlParameter("@charaId", charaId));

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var storyId = reader.GetInt32(0);
                        var eventTitle = reader.IsDBNull(1)
                            ? $"Event {storyId}"
                            : reader.GetString(1);
                        var cardId = reader.GetInt32(2);
                        var showProgress = reader.GetInt32(3);

                        var isChainEvent = cardId > 0;

                        events.Add(
                            new EventDetail
                            {
                                StoryId = storyId,
                                EventTitle = eventTitle,
                                EventType = isChainEvent ? "Chain Event" : "Random Event",
                                EventOrder = isChainEvent ? showProgress : 0,
                            }
                        );
                    }
                }
            }

            return events;
        }

        private async Task<UniqueEffectDetail> GetUniqueEffect(int uniqueEffectId)
        {
            if (uniqueEffectId == 0)
                return null;

            var query =
                @"
                SELECT 
                    lv, type_0, value_0, value_0_1, value_0_2, value_0_3, value_0_4,
                    type_1, value_1, value_1_1, value_1_2, value_1_3, value_1_4
                FROM support_card_unique_effect
                WHERE id = @uniqueEffectId";

            await using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(new MySqlParameter("@uniqueEffectId", uniqueEffectId));

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                // Read all data first, then close reader before getting names
                int level = 0,
                    type0 = 0,
                    type1 = 0;
                int value0 = 0,
                    value01 = 0,
                    value02 = 0,
                    value03 = 0,
                    value04 = 0;
                int value1 = 0,
                    value11 = 0,
                    value12 = 0,
                    value13 = 0,
                    value14 = 0;
                bool hasData = false;

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        hasData = true;
                        level = reader.GetInt32(0);
                        type0 = reader.GetInt32(1);
                        value0 = reader.GetInt32(2);
                        value01 = reader.GetInt32(3);
                        value02 = reader.GetInt32(4);
                        value03 = reader.GetInt32(5);
                        value04 = reader.GetInt32(6);
                        type1 = reader.GetInt32(7);
                        value1 = reader.GetInt32(8);
                        value11 = reader.GetInt32(9);
                        value12 = reader.GetInt32(10);
                        value13 = reader.GetInt32(11);
                        value14 = reader.GetInt32(12);
                    }
                }

                if (hasData)
                {
                    // Now get the names after reader is closed
                    return new UniqueEffectDetail
                    {
                        Level = level,
                        Type0 = type0,
                        Type0Name = GetUniqueEffectTypeName(type0),
                        Value0 = value0,
                        Value01 = value01,
                        Value02 = value02,
                        Value03 = value03,
                        Value04 = value04,
                        Type1 = type1,
                        Type1Name = GetUniqueEffectTypeName(type1),
                        Value1 = value1,
                        Value11 = value11,
                        Value12 = value12,
                        Value13 = value13,
                        Value14 = value14,
                    };
                }
            }

            return null;
        }

        private string GetRarityDisplay(int rarity)
        {
            return rarity switch
            {
                1 => "R",
                2 => "SR",
                3 => "SSR",
                _ => $"{rarity}★",
            };
        }

        private string GetSupportCardTypeName(int type)
        {
            return type switch
            {
                1 => "Speed",
                2 => "Stamina",
                3 => "Power",
                4 => "Guts",
                5 => "Intelligence",
                6 => "Friend",
                _ => "Unknown",
            };
        }

        private string GetEffectTypeName(int type)
        {
            return type switch
            {
                1 => "Friendship Bonus",
                2 => "Mood Effect",
                3 => "Speed Bonus",
                4 => "Stamina Bonus",
                5 => "Power Bonus",
                6 => "Guts Bonus",
                7 => "Wit Bonus",
                8 => "Training Effectiveness",
                9 => "Initial Speed",
                10 => "Initial Stamina",
                11 => "Initial Power",
                12 => "Initial Guts",
                13 => "Initial Wit",
                14 => "Initial Friendship Gauge",
                15 => "Race Bonus",
                16 => "Fan Bonus",
                17 => "Hint Levels",
                18 => "Hint Frequency",
                19 => "Specialty Priority",
                20 => "Max Speed",
                21 => "Max Stamina",
                22 => "Max Power",
                23 => "Max Guts",
                24 => "Max Wit",
                25 => "Event Recovery",
                26 => "Event Effectiveness",
                27 => "Failure Protection",
                28 => "Energy Cost Reduction",
                29 => "Minigame Effectiveness",
                30 => "Skill Point Bonus",
                31 => "Wit Friendship Recovery",
                _ => $"Effect Type {type}",
            };
        }

        private string GetUniqueEffectTypeName(int type)
        {
            if (type == 0)
                return "None";

            // Mappings from text_data category 180
            return type switch
            {
                1 => "Increases the effectiveness of Friendship Training",
                2 => "Amplifies the effect of mood when training together",
                3 => "Increases Speed gain when training together",
                4 => "Increases Stamina gain when training together",
                5 => "Increases Power gain when training together",
                6 => "Increases Guts gain when training together",
                7 => "Increases Wit gain when training together",
                8 => "Increases the effectiveness of training performed together",
                9 => "Increases initial Speed when beginning a Career playthrough",
                10 => "Increases initial Stamina when beginning a Career playthrough",
                11 => "Increases initial Power when beginning a Career playthrough",
                12 => "Increases initial Guts when beginning a Career playthrough",
                13 => "Increases initial Wit when beginning a Career playthrough",
                14 => "Increases initial Friendship Gauge when beginning a Career playthrough",
                15 => "Increases stat gain from races",
                16 => "Increases fan gain from races",
                17 => "Increases the level of hints gained through events",
                18 => "Increases the frequency at which hint events occur",
                19 =>
                    "Increases the frequency at which the character participates in their preferred training type",
                20 => "Increases max Speed value when beginning a Career playthrough",
                21 => "Increases max Stamina value when beginning a Career playthrough",
                22 => "Increases max Power value when beginning a Career playthrough",
                23 => "Increases max Guts value when beginning a Career playthrough",
                24 => "Increases max Wit value when beginning a Career playthrough",
                25 => "Increases Energy recovery from this Support Card's events",
                26 => "Increases stat gain from this Support Card's events",
                27 => "Increases stat gain from Wit Training",
                28 => "Increases initial bond with the character",
                29 => "Increases stat gain from minigames",
                30 => "Increases skill point gain when training together",
                31 => "Increases Energy recovery from Wit Friendship Training",
                _ => $"Special Effect (Type {type})",
            };
        }
    }
}
