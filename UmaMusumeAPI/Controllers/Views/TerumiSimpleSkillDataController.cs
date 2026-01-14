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
        private readonly UmaMusumeDbContext _context;

        public TerumiSimpleSkillDataController(UmaMusumeDbContext context)
        {
            _context = context;
        }

        // GET: api/TerumiSimpleSkillData
        [HttpGet]
        public async Task<
            ActionResult<IEnumerable<TerumiSimpleSkillData>>
        > GetTerumiSimpleSkillData()
        {
            var skills = new List<TerumiSimpleSkillData>();

            using (var connection = (MySqlConnection)_context.Database.GetDbConnection())
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
                        s.icon_id as iconId,
                        smsnp.need_skill_point as needSkillPoint,
                        t_name.text as skillName,
                        t_desc.text as skillDesc
                    FROM skill_data s
                    LEFT JOIN text_data t_name ON t_name.category = 47 AND t_name.index = s.id
                    LEFT JOIN text_data t_desc ON t_desc.category = 48 AND t_desc.index = s.id
                    LEFT JOIN single_mode_skill_need_point smsnp ON smsnp.id = s.id";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var skill = new TerumiSimpleSkillData
                        {
                            SkillId = reader.GetInt32("skillId"),
                            Rarity = reader.GetInt32("rarity"),
                            GradeValue = reader.GetInt32("gradeValue"),
                            SkillCategory = GetSkillCategoryName(reader.GetInt32("skillCategory")),
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
                            Effects = new List<SkillEffect>(),
                        };

                        // Parse effects
                        var abilityType1 = reader.GetInt32("ability_type_1_1");
                        var abilityValue1 = reader.IsDBNull(
                            reader.GetOrdinal("float_ability_value_1_1")
                        )
                            ? 0
                            : reader.GetInt32("float_ability_value_1_1");

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

                        // Build effect summary
                        skill.EffectSummary = string.Join(
                            " | ",
                            skill
                                .Effects.Select(e => e.DisplayText)
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
            using (var connection = (MySqlConnection)_context.Database.GetDbConnection())
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
                        s.icon_id as iconId,
                        smsnp.need_skill_point as needSkillPoint,
                        t_name.text as skillName,
                        t_desc.text as skillDesc
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
                            var skill = new TerumiSimpleSkillData
                            {
                                SkillId = reader.GetInt32("skillId"),
                                Rarity = reader.GetInt32("rarity"),
                                GradeValue = reader.GetInt32("gradeValue"),
                                SkillCategory = GetSkillCategoryName(
                                    reader.GetInt32("skillCategory")
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
                                Effects = new List<SkillEffect>(),
                            };

                            // Parse effects
                            var abilityType1 = reader.GetInt32("ability_type_1_1");
                            var abilityValue1 = reader.IsDBNull(
                                reader.GetOrdinal("float_ability_value_1_1")
                            )
                                ? 0
                                : reader.GetInt32("float_ability_value_1_1");

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

                            // Build effect summary
                            skill.EffectSummary = string.Join(
                                " | ",
                                skill
                                    .Effects.Select(e => e.DisplayText)
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
            return category switch
            {
                0 => "Passive",
                1 => "Debuff",
                2 => "Recovery",
                3 => "Speed Boost",
                4 => "Lane Effect",
                5 => "Unique",
                _ => $"Category{category}",
            };
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
