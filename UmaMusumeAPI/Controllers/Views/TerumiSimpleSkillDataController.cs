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
    public class TerumiSimpleSkillDataController : ControllerBase
    {
        private readonly string _connectionString;

        public TerumiSimpleSkillDataController(UmaMusumeDbContext context)
        {
            _connectionString = context.Database.GetConnectionString();
        }

        // GET: api/TerumiSimpleSkillData
        [HttpGet]
        public async Task<
            ActionResult<IEnumerable<TerumiSimpleSkillData>>
        > GetTerumiSimpleSkillData()
        {
            var skills = new List<TerumiSimpleSkillData>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT 
                        s.id as skillId,
                        s.rarity,
                        s.grade_value as gradeValue,
                        s.skill_category as skillCategory,
                        s.tag_id as tagId,
                        s.condition_1 as activationCondition,
                        s.precondition_1 as precondition,
                        s.ability_type_1_1,
                        s.float_ability_value_1_1,
                        s.ability_type_1_2,
                        s.float_ability_value_1_2,
                        s.float_ability_time_1 as duration,
                        s.float_cooldown_time_1 as cooldownTime,
                        s.condition_2 as activationCondition2,
                        s.precondition_2 as precondition2,
                        s.ability_type_2_1,
                        s.float_ability_value_2_1,
                        s.ability_type_2_2,
                        s.float_ability_value_2_2,
                        s.float_ability_time_2 as duration2,
                        s.float_cooldown_time_2 as cooldownTime2,
                        s.icon_id as iconId,
                        smsnp.need_skill_point as needSkillPoint,
                        t_name.text as skillName,
                        t_desc.text as skillDesc,
                        (SELECT GROUP_CONCAT(DISTINCT support_card_id ORDER BY support_card_id SEPARATOR ',')
                         FROM single_mode_hint_gain
                         WHERE hint_gain_type = 0 AND hint_value_1 = s.id) as supportCardIds
                    FROM skill_data s
                    LEFT JOIN text_data t_name ON t_name.category = 47 AND t_name.index = s.id
                    LEFT JOIN text_data t_desc ON t_desc.category = 48 AND t_desc.index = s.id
                    LEFT JOIN single_mode_skill_need_point smsnp ON smsnp.id = s.id";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Read ability info first to determine category
                        var dbSkillCategory = reader.GetInt32("skillCategory");
                        var abilityType1 = reader.GetInt32("ability_type_1_1");
                        var abilityValue1 = reader.IsDBNull(
                            reader.GetOrdinal("float_ability_value_1_1")
                        )
                            ? 0
                            : reader.GetInt32("float_ability_value_1_1");

                        var skill = new TerumiSimpleSkillData
                        {
                            SkillId = reader.GetInt32("skillId"),
                            Rarity = reader.GetInt32("rarity"),
                            GradeValue = reader.GetInt32("gradeValue"),
                            SkillCategory = DetermineSkillCategory(
                                dbSkillCategory,
                                abilityType1,
                                abilityValue1
                            ),
                            TagId = reader.IsDBNull(reader.GetOrdinal("tagId"))
                                ? ""
                                : reader.GetString("tagId"),
                            ActivationCondition = reader.IsDBNull(
                                reader.GetOrdinal("activationCondition")
                            )
                                ? ""
                                : reader.GetString("activationCondition"),
                            Precondition = reader.IsDBNull(reader.GetOrdinal("precondition"))
                                ? ""
                                : reader.GetString("precondition"),
                            ActivationCondition2 = reader.IsDBNull(
                                reader.GetOrdinal("activationCondition2")
                            )
                                ? ""
                                : reader.GetString("activationCondition2"),
                            Precondition2 = reader.IsDBNull(reader.GetOrdinal("precondition2"))
                                ? ""
                                : reader.GetString("precondition2"),
                            IconId = reader.GetInt32("iconId"),
                            NeedSkillPoint = reader.IsDBNull(reader.GetOrdinal("needSkillPoint"))
                                ? 0
                                : reader.GetInt32("needSkillPoint"),
                            SkillName = reader.IsDBNull(reader.GetOrdinal("skillName"))
                                ? ""
                                : reader.GetString("skillName"),
                            SkillDesc = reader.IsDBNull(reader.GetOrdinal("skillDesc"))
                                ? ""
                                : reader.GetString("skillDesc"),
                            Duration = reader.IsDBNull(reader.GetOrdinal("duration"))
                                ? null
                                : (decimal?)reader.GetInt32("duration") / 10000m,
                            CooldownTime = reader.IsDBNull(reader.GetOrdinal("cooldownTime"))
                                ? null
                                : (decimal?)reader.GetInt32("cooldownTime") / 10000m,
                            Duration2 = reader.IsDBNull(reader.GetOrdinal("duration2"))
                                ? null
                                : (decimal?)reader.GetInt32("duration2") / 10000m,
                            CooldownTime2 = reader.IsDBNull(reader.GetOrdinal("cooldownTime2"))
                                ? null
                                : (decimal?)reader.GetInt32("cooldownTime2") / 10000m,
                            SupportCardIds = reader.IsDBNull(reader.GetOrdinal("supportCardIds"))
                                ? ""
                                : reader.GetString("supportCardIds"),
                            Effects = new List<SkillEffect>(),
                            Effects2 = new List<SkillEffect>(),
                        };

                        // Parse condition 1 effects
                        if (abilityType1 > 0)
                        {
                            skill.Effects.Add(CreateSkillEffect(abilityType1, abilityValue1));
                        }

                        var abilityType2 = reader.GetInt32("ability_type_1_2");
                        var abilityValue2 = reader.IsDBNull(
                            reader.GetOrdinal("float_ability_value_1_2")
                        )
                            ? 0
                            : reader.GetInt32("float_ability_value_1_2");

                        if (abilityType2 > 0)
                        {
                            skill.Effects.Add(CreateSkillEffect(abilityType2, abilityValue2));
                        }

                        // Parse condition 2 effects
                        var abilityType2_1 = reader.IsDBNull(reader.GetOrdinal("ability_type_2_1"))
                            ? 0
                            : reader.GetInt32("ability_type_2_1");
                        var abilityValue2_1 = reader.IsDBNull(
                            reader.GetOrdinal("float_ability_value_2_1")
                        )
                            ? 0
                            : reader.GetInt32("float_ability_value_2_1");

                        if (abilityType2_1 > 0)
                        {
                            skill.Effects2.Add(CreateSkillEffect(abilityType2_1, abilityValue2_1));
                        }

                        var abilityType2_2 = reader.IsDBNull(reader.GetOrdinal("ability_type_2_2"))
                            ? 0
                            : reader.GetInt32("ability_type_2_2");
                        var abilityValue2_2 = reader.IsDBNull(
                            reader.GetOrdinal("float_ability_value_2_2")
                        )
                            ? 0
                            : reader.GetInt32("float_ability_value_2_2");

                        if (abilityType2_2 > 0)
                        {
                            skill.Effects2.Add(CreateSkillEffect(abilityType2_2, abilityValue2_2));
                        }

                        // Build effect summaries
                        skill.EffectSummary = string.Join(
                            " | ",
                            skill
                                .Effects.Select(e => e.DisplayText)
                                .Where(t => !string.IsNullOrEmpty(t))
                        );

                        skill.EffectSummary2 = string.Join(
                            " | ",
                            skill
                                .Effects2.Select(e => e.DisplayText)
                                .Where(t => !string.IsNullOrEmpty(t))
                        );

                        skills.Add(skill);
                    }
                }
            }

            return skills;
        }

        // GET: api/TerumiSimpleSkillData/{skillId}
        [HttpGet("{skillId}")]
        public async Task<ActionResult<TerumiSimpleSkillData>> GetTerumiSimpleSkillData(int skillId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT 
                        s.id as skillId,
                        s.rarity,
                        s.grade_value as gradeValue,
                        s.skill_category as skillCategory,
                        s.tag_id as tagId,
                        s.condition_1 as activationCondition,
                        s.precondition_1 as precondition,
                        s.ability_type_1_1,
                        s.float_ability_value_1_1,
                        s.ability_type_1_2,
                        s.float_ability_value_1_2,
                        s.float_ability_time_1 as duration,
                        s.float_cooldown_time_1 as cooldownTime,
                        s.condition_2 as activationCondition2,
                        s.precondition_2 as precondition2,
                        s.ability_type_2_1,
                        s.float_ability_value_2_1,
                        s.ability_type_2_2,
                        s.float_ability_value_2_2,
                        s.float_ability_time_2 as duration2,
                        s.float_cooldown_time_2 as cooldownTime2,
                        s.icon_id as iconId,
                        smsnp.need_skill_point as needSkillPoint,
                        t_name.text as skillName,
                        t_desc.text as skillDesc,
                        (SELECT GROUP_CONCAT(DISTINCT support_card_id ORDER BY support_card_id SEPARATOR ',')
                         FROM single_mode_hint_gain
                         WHERE hint_gain_type = 0 AND hint_value_1 = s.id) as supportCardIds
                    FROM skill_data s
                    LEFT JOIN text_data t_name ON t_name.category = 47 AND t_name.index = s.id
                    LEFT JOIN text_data t_desc ON t_desc.category = 48 AND t_desc.index = s.id
                    LEFT JOIN single_mode_skill_need_point smsnp ON smsnp.id = s.id
                    WHERE s.id = @skillId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@skillId", skillId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Read ability info first to determine category
                            var dbSkillCategory = reader.GetInt32("skillCategory");
                            var abilityType1 = reader.GetInt32("ability_type_1_1");
                            var abilityValue1 = reader.IsDBNull(
                                reader.GetOrdinal("float_ability_value_1_1")
                            )
                                ? 0
                                : reader.GetInt32("float_ability_value_1_1");

                            var skill = new TerumiSimpleSkillData
                            {
                                SkillId = reader.GetInt32("skillId"),
                                Rarity = reader.GetInt32("rarity"),
                                GradeValue = reader.GetInt32("gradeValue"),
                                SkillCategory = DetermineSkillCategory(
                                    dbSkillCategory,
                                    abilityType1,
                                    abilityValue1
                                ),
                                TagId = reader.IsDBNull(reader.GetOrdinal("tagId"))
                                    ? ""
                                    : reader.GetString("tagId"),
                                ActivationCondition = reader.IsDBNull(
                                    reader.GetOrdinal("activationCondition")
                                )
                                    ? ""
                                    : reader.GetString("activationCondition"),
                                Precondition = reader.IsDBNull(reader.GetOrdinal("precondition"))
                                    ? ""
                                    : reader.GetString("precondition"),
                                ActivationCondition2 = reader.IsDBNull(
                                    reader.GetOrdinal("activationCondition2")
                                )
                                    ? ""
                                    : reader.GetString("activationCondition2"),
                                Precondition2 = reader.IsDBNull(reader.GetOrdinal("precondition2"))
                                    ? ""
                                    : reader.GetString("precondition2"),
                                IconId = reader.GetInt32("iconId"),
                                NeedSkillPoint = reader.IsDBNull(
                                    reader.GetOrdinal("needSkillPoint")
                                )
                                    ? 0
                                    : reader.GetInt32("needSkillPoint"),
                                SkillName = reader.IsDBNull(reader.GetOrdinal("skillName"))
                                    ? ""
                                    : reader.GetString("skillName"),
                                SkillDesc = reader.IsDBNull(reader.GetOrdinal("skillDesc"))
                                    ? ""
                                    : reader.GetString("skillDesc"),
                                Duration = reader.IsDBNull(reader.GetOrdinal("duration"))
                                    ? null
                                    : (decimal?)reader.GetInt32("duration") / 10000m,
                                CooldownTime = reader.IsDBNull(reader.GetOrdinal("cooldownTime"))
                                    ? null
                                    : (decimal?)reader.GetInt32("cooldownTime") / 10000m,
                                Duration2 = reader.IsDBNull(reader.GetOrdinal("duration2"))
                                    ? null
                                    : (decimal?)reader.GetInt32("duration2") / 10000m,
                                CooldownTime2 = reader.IsDBNull(reader.GetOrdinal("cooldownTime2"))
                                    ? null
                                    : (decimal?)reader.GetInt32("cooldownTime2") / 10000m,
                                SupportCardIds = reader.IsDBNull(
                                    reader.GetOrdinal("supportCardIds")
                                )
                                    ? ""
                                    : reader.GetString("supportCardIds"),
                                Effects = new List<SkillEffect>(),
                                Effects2 = new List<SkillEffect>(),
                            };

                            // Parse condition 1 effects
                            if (abilityType1 > 0)
                            {
                                skill.Effects.Add(CreateSkillEffect(abilityType1, abilityValue1));
                            }

                            var abilityType2 = reader.GetInt32("ability_type_1_2");
                            var abilityValue2 = reader.IsDBNull(
                                reader.GetOrdinal("float_ability_value_1_2")
                            )
                                ? 0
                                : reader.GetInt32("float_ability_value_1_2");

                            if (abilityType2 > 0)
                            {
                                skill.Effects.Add(CreateSkillEffect(abilityType2, abilityValue2));
                            }

                            // Parse condition 2 effects
                            var abilityType2_1 = reader.IsDBNull(
                                reader.GetOrdinal("ability_type_2_1")
                            )
                                ? 0
                                : reader.GetInt32("ability_type_2_1");
                            var abilityValue2_1 = reader.IsDBNull(
                                reader.GetOrdinal("float_ability_value_2_1")
                            )
                                ? 0
                                : reader.GetInt32("float_ability_value_2_1");

                            if (abilityType2_1 > 0)
                            {
                                skill.Effects2.Add(
                                    CreateSkillEffect(abilityType2_1, abilityValue2_1)
                                );
                            }

                            var abilityType2_2 = reader.IsDBNull(
                                reader.GetOrdinal("ability_type_2_2")
                            )
                                ? 0
                                : reader.GetInt32("ability_type_2_2");
                            var abilityValue2_2 = reader.IsDBNull(
                                reader.GetOrdinal("float_ability_value_2_2")
                            )
                                ? 0
                                : reader.GetInt32("float_ability_value_2_2");

                            if (abilityType2_2 > 0)
                            {
                                skill.Effects2.Add(
                                    CreateSkillEffect(abilityType2_2, abilityValue2_2)
                                );
                            }

                            // Build effect summaries
                            skill.EffectSummary = string.Join(
                                " | ",
                                skill
                                    .Effects.Select(e => e.DisplayText)
                                    .Where(t => !string.IsNullOrEmpty(t))
                            );

                            skill.EffectSummary2 = string.Join(
                                " | ",
                                skill
                                    .Effects2.Select(e => e.DisplayText)
                                    .Where(t => !string.IsNullOrEmpty(t))
                            );

                            return skill;
                        }
                    }
                }
            }

            return NotFound();
        }

        private string GetSkillCategoryName(int category)
        {
            // skill_category 5 is always "Unique" regardless of effect
            if (category == 5)
                return "Unique";

            // skill_category 0 is always "Passive" (stat-based skills)
            if (category == 0)
                return "Passive";

            // For other categories, we'll determine later based on effect type
            return category switch
            {
                1 => "Debuff",
                2 => "Recovery",
                3 => "Speed Boost",
                4 => "Lane Effect",
                _ => $"Category{category}",
            };
        }

        private string DetermineSkillCategory(int dbCategory, int abilityType, int abilityValue)
        {
            // skill_category 5 is always "Unique"
            if (dbCategory == 5)
                return "Unique";

            // skill_category 0 is always "Passive" (conditional stat boosts like course/turn preferences)
            if (dbCategory == 0)
                return "Passive";

            // Check if this is a debuff (negative value on normally positive effects, or positive on negative effects like "morale down")
            bool isDebuff = false;

            // For most effect types, negative value = debuff
            // But type 21 (mood/give-up tendency) is always a debuff (makes opponents lose motivation)
            // And type 13 (frenzy time) is always a debuff when applied to enemies
            switch (abilityType)
            {
                case 1: // Speed stat
                case 2: // Stamina stat
                case 3: // Power stat
                case 4: // Guts stat
                case 5: // Intelligence stat
                    isDebuff = abilityValue < 0;
                    break;
                case 9: // Stamina Recovery
                    if (abilityValue < 0)
                        isDebuff = true;
                    else
                        return "Recovery";
                    break;
                case 27: // Target Speed
                    if (abilityValue < 0)
                        isDebuff = true;
                    else
                        return "Speed Boost";
                    break;
                case 31: // Acceleration
                    if (abilityValue < 0)
                        isDebuff = true;
                    else
                        return "Acceleration";
                    break;
                case 28: // Navigation/Lane positioning
                    if (abilityValue < 0)
                        isDebuff = true;
                    else
                        return "Lane Effect";
                    break;
                case 10: // Gate/Start reaction
                    if (abilityValue < 0) // Negative = faster start
                        return "Gate";
                    else
                        isDebuff = true; // Positive = slower start
                    break;
                case 8: // Field of view
                    if (abilityValue < 0)
                        isDebuff = true;
                    else
                        return "Vision";
                    break;
                case 21: // Mood/Give-up tendency - always affects opponents negatively
                    isDebuff = true;
                    break;
                case 13: // Frenzy/calm-down time - always a debuff when applied to enemies
                    isDebuff = true;
                    break;
                case 6: // Unknown type 6
                    return "Special";
                case 502: // Unknown type 502
                    return "Special";
                default:
                    // Fall back to database category
                    return GetSkillCategoryName(dbCategory);
            }

            if (isDebuff)
                return "Debuff";

            return GetSkillCategoryName(dbCategory);
        }

        private SkillEffect CreateSkillEffect(int abilityType, int abilityValue)
        {
            var effect = new SkillEffect();
            var valueDecimal = abilityValue / 10000m;

            switch (abilityType)
            {
                case 1:
                    effect.Type = "Speed Stat";
                    effect.Value = Math.Round(valueDecimal, 0);
                    effect.DisplayText = $"Speed +{effect.Value}";
                    break;
                case 2:
                    effect.Type = "Stamina Stat";
                    effect.Value = Math.Round(valueDecimal, 0);
                    effect.DisplayText = $"Stamina +{effect.Value}";
                    break;
                case 3:
                    effect.Type = "Power Stat";
                    effect.Value = Math.Round(valueDecimal, 0);
                    effect.DisplayText = $"Power +{effect.Value}";
                    break;
                case 4:
                    effect.Type = "Guts Stat";
                    effect.Value = Math.Round(valueDecimal, 0);
                    effect.DisplayText = $"Guts +{effect.Value}";
                    break;
                case 5:
                    effect.Type = "Intelligence Stat";
                    effect.Value = Math.Round(valueDecimal, 0);
                    effect.DisplayText = $"Intelligence +{effect.Value}";
                    break;
                case 9:
                    effect.Type = "Stamina Recovery";
                    effect.Value = Math.Round(valueDecimal, 3);
                    effect.DisplayText = $"Stamina Recovery +{effect.Value}";
                    break;
                case 27:
                    effect.Type = "Target Speed";
                    effect.Value = Math.Round(valueDecimal, 3);
                    effect.DisplayText = $"Target Speed +{effect.Value}";
                    break;
                case 31:
                    effect.Type = "Acceleration";
                    effect.Value = Math.Round(valueDecimal, 3);
                    effect.DisplayText = $"Acceleration +{effect.Value}";
                    break;
                default:
                    effect.Type = $"Type{abilityType}";
                    effect.Value = Math.Round(valueDecimal, 3);
                    effect.DisplayText = "";
                    break;
            }

            return effect;
        }
    }
}
