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
    public class TerumiCharacterDataController : ControllerBase
    {
        private readonly string _connectionString;

        public TerumiCharacterDataController(UmaMusumeDbContext context)
        {
            _connectionString = context.Database.GetConnectionString();
        }

        // GET: api/TerumiCharacterData
        /// <summary>
        /// Get all Uma Musume characters with detailed information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TerumiCharacterData>>> GetTerumiCharacterData()
        {
            var query =
                @"
                SELECT 
                    cd.id as CharaId,
                    t_name.text as CharaName,
                    t_cv.text as VoiceActor,
                    card.id as CardId,
                    t_card.text as CardTitle,
                    scd.id as SupportCardId,
                    FROM_UNIXTIME(cd.start_date) as StartDate,
                    
                    -- Base Stats from 5-star card
                    r5.speed as BaseSpeed,
                    r5.stamina as BaseStamina,
                    r5.pow as BasePower,
                    r5.guts as BaseGuts,
                    r5.wiz as BaseWisdom,
                    
                    -- Talent (Growth Rate)
                    card.talent_speed as TalentSpeed,
                    card.talent_stamina as TalentStamina,
                    card.talent_pow as TalentPower,
                    card.talent_guts as TalentGuts,
                    card.talent_wiz as TalentWisdom,
                    
                    -- Aptitudes
                    r5.proper_ground_turf as AptitudeTurf,
                    r5.proper_ground_dirt as AptitudeDirt,
                    r5.proper_distance_short as AptitudeShort,
                    r5.proper_distance_mile as AptitudeMile,
                    r5.proper_distance_middle as AptitudeMiddle,
                    r5.proper_distance_long as AptitudeLong,
                    r5.proper_running_style_nige as AptitudeRunner,
                    r5.proper_running_style_senko as AptitudeLeader,
                    r5.proper_running_style_sashi as AptitudeBetweener,
                    r5.proper_running_style_oikomi as AptitudeChaser,
                    
                    -- Bio Data
                    cd.birth_year as BirthYear,
                    cd.birth_month as BirthMonth,
                    cd.birth_day as BirthDay,
                    cd.sex as Sex,
                    cd.height as Height,
                    cd.bust as Bust,
                    cd.chara_category as CharaCategory,
                    
                    -- URA Objectives
                    ura.race_set_id as UraObjectives,
                    
                    -- Skills: unique skill (100xxx/110xxx) from skill_set.skill_id1 + regular skills (200xxx) from available_skill_set
                    TRIM(BOTH ',' FROM CONCAT(
                        IFNULL((SELECT ss.skill_id1 FROM skill_set ss WHERE ss.id = CONCAT('1', card.id) AND ss.skill_id1 >= 100000 AND ss.skill_id1 < 200000), ''),
                        ',',
                        IFNULL((SELECT crd_ss.skill_id1 FROM card_rarity_data crd INNER JOIN skill_set crd_ss ON crd_ss.id = crd.skill_set WHERE crd.card_id = card.id AND crd_ss.skill_id1 >= 100000 AND crd_ss.skill_id1 < 200000 LIMIT 1), ''),
                        ',',
                        IFNULL((SELECT GROUP_CONCAT(DISTINCT ass.skill_id ORDER BY ass.skill_id SEPARATOR ',') FROM available_skill_set ass WHERE ass.available_skill_set_id = card.available_skill_set_id), '')
                    )) as SkillIds
                    
                FROM card_data card
                LEFT JOIN chara_data cd ON card.chara_id = cd.id
                LEFT JOIN text_data t_name ON t_name.`index` = cd.id AND t_name.category = 6
                LEFT JOIN text_data t_cv ON t_cv.`index` = cd.id AND t_cv.category = 7
                LEFT JOIN text_data t_card ON t_card.`index` = card.id AND t_card.category = 5
                LEFT JOIN card_rarity_data r5 ON r5.card_id = card.id AND r5.rarity = 5
                LEFT JOIN (
                    SELECT chara_id, MIN(id) as id 
                    FROM support_card_data 
                    WHERE support_card_type = 1 
                    GROUP BY chara_id
                ) scd ON scd.chara_id = cd.id
                LEFT JOIN single_mode_route ura ON ura.chara_id = cd.id AND ura.scenario_id = 0
                WHERE t_name.text IS NOT NULL AND card.id IS NOT NULL
                ORDER BY card.id";

            var result = new List<TerumiCharacterData>();

            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(
                                new TerumiCharacterData
                                {
                                    CharaId = reader.GetInt32(reader.GetOrdinal("CharaId")),
                                    CharaName = reader.IsDBNull(reader.GetOrdinal("CharaName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("CharaName")),
                                    VoiceActor = reader.IsDBNull(reader.GetOrdinal("VoiceActor"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("VoiceActor")),
                                    CardId = reader.IsDBNull(reader.GetOrdinal("CardId"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("CardId")),
                                    CardTitle = reader.IsDBNull(reader.GetOrdinal("CardTitle"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("CardTitle")),
                                    SupportCardId = reader.IsDBNull(
                                        reader.GetOrdinal("SupportCardId")
                                    )
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("SupportCardId")),
                                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                                        ? null
                                        : reader.GetDateTime(reader.GetOrdinal("StartDate")),

                                    BaseSpeed = reader.IsDBNull(reader.GetOrdinal("BaseSpeed"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BaseSpeed")),
                                    BaseStamina = reader.IsDBNull(reader.GetOrdinal("BaseStamina"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BaseStamina")),
                                    BasePower = reader.IsDBNull(reader.GetOrdinal("BasePower"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BasePower")),
                                    BaseGuts = reader.IsDBNull(reader.GetOrdinal("BaseGuts"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BaseGuts")),
                                    BaseWisdom = reader.IsDBNull(reader.GetOrdinal("BaseWisdom"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BaseWisdom")),

                                    TalentSpeed = reader.IsDBNull(reader.GetOrdinal("TalentSpeed"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("TalentSpeed")),
                                    TalentStamina = reader.IsDBNull(
                                        reader.GetOrdinal("TalentStamina")
                                    )
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("TalentStamina")),
                                    TalentPower = reader.IsDBNull(reader.GetOrdinal("TalentPower"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("TalentPower")),
                                    TalentGuts = reader.IsDBNull(reader.GetOrdinal("TalentGuts"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("TalentGuts")),
                                    TalentWisdom = reader.IsDBNull(
                                        reader.GetOrdinal("TalentWisdom")
                                    )
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("TalentWisdom")),

                                    AptitudeTurf = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeTurf")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeTurf"))
                                        ),
                                    AptitudeDirt = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeDirt")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeDirt"))
                                        ),
                                    AptitudeShort = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeShort")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeShort"))
                                        ),
                                    AptitudeMile = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeMile")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeMile"))
                                        ),
                                    AptitudeMiddle = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeMiddle")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeMiddle"))
                                        ),
                                    AptitudeLong = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeLong")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeLong"))
                                        ),
                                    AptitudeRunner = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeRunner")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeRunner"))
                                        ),
                                    AptitudeLeader = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeLeader")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeLeader"))
                                        ),
                                    AptitudeBetweener = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeBetweener")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeBetweener"))
                                        ),
                                    AptitudeChaser = reader.IsDBNull(
                                        reader.GetOrdinal("AptitudeChaser")
                                    )
                                        ? null
                                        : GetAptitudeRank(
                                            reader.GetInt32(reader.GetOrdinal("AptitudeChaser"))
                                        ),

                                    BirthYear = reader.IsDBNull(reader.GetOrdinal("BirthYear"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BirthYear")),
                                    BirthMonth = reader.IsDBNull(reader.GetOrdinal("BirthMonth"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BirthMonth")),
                                    BirthDay = reader.IsDBNull(reader.GetOrdinal("BirthDay"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("BirthDay")),
                                    Sex = reader.IsDBNull(reader.GetOrdinal("Sex"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("Sex")),
                                    Height = reader.IsDBNull(reader.GetOrdinal("Height"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("Height")),
                                    Bust = reader.IsDBNull(reader.GetOrdinal("Bust"))
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("Bust")),
                                    CharaCategory = reader.IsDBNull(
                                        reader.GetOrdinal("CharaCategory")
                                    )
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("CharaCategory")),
                                    UraObjectives = reader.IsDBNull(
                                        reader.GetOrdinal("UraObjectives")
                                    )
                                        ? null
                                        : reader.GetInt32(reader.GetOrdinal("UraObjectives")),
                                    SkillIds = reader.IsDBNull(reader.GetOrdinal("SkillIds"))
                                        ? ""
                                        : RemoveDuplicateSkillIds(
                                            reader.GetString(reader.GetOrdinal("SkillIds"))
                                        ),
                                }
                            );
                        }
                    }
                }
            }

            return result;
        }

        // GET: api/TerumiCharacterData/1001
        /// <summary>
        /// Get a specific Uma Musume character by ID
        /// </summary>
        [HttpGet("{charaId}")]
        public async Task<ActionResult<TerumiCharacterData>> GetTerumiCharacterData(int charaId)
        {
            var query =
                @"
                SELECT 
                    cd.id as CharaId,
                    t_name.text as CharaName,
                    t_cv.text as VoiceActor,
                    card.id as CardId,
                    scd.id as SupportCardId,
                    FROM_UNIXTIME_SECONDS(cd.start_date) as StartDate,
                    
                    -- Base Stats from 5-star card
                    r5.speed as BaseSpeed,
                    r5.stamina as BaseStamina,
                    r5.pow as BasePower,
                    r5.guts as BaseGuts,
                    r5.wiz as BaseWisdom,
                    
                    -- Talent (Growth Rate)
                    card.talent_speed as TalentSpeed,
                    card.talent_stamina as TalentStamina,
                    card.talent_pow as TalentPower,
                    card.talent_guts as TalentGuts,
                    card.talent_wiz as TalentWisdom,
                    
                    -- Aptitudes
                    r5.proper_ground_turf as AptitudeTurf,
                    r5.proper_ground_dirt as AptitudeDirt,
                    r5.proper_distance_short as AptitudeShort,
                    r5.proper_distance_mile as AptitudeMile,
                    r5.proper_distance_middle as AptitudeMiddle,
                    r5.proper_distance_long as AptitudeLong,
                    r5.proper_running_style_nige as AptitudeRunner,
                    r5.proper_running_style_senko as AptitudeLeader,
                    r5.proper_running_style_sashi as AptitudeBetweener,
                    r5.proper_running_style_oikomi as AptitudeChaser,
                    
                    -- Bio Data
                    cd.birth_year as BirthYear,
                    cd.birth_month as BirthMonth,
                    cd.birth_day as BirthDay,
                    cd.sex as Sex,
                    cd.height as Height,
                    cd.bust as Bust,
                    cd.chara_category as CharaCategory,
                    
                    -- URA Objectives
                    ura.race_set_id as UraObjectives,
                    
                    -- Skills: unique skill (100xxx/110xxx) from skill_set.skill_id1 + regular skills (200xxx) from available_skill_set
                    TRIM(BOTH ',' FROM CONCAT(
                        IFNULL((SELECT ss.skill_id1 FROM skill_set ss WHERE ss.id = CONCAT('1', card.id) AND ss.skill_id1 >= 100000 AND ss.skill_id1 < 200000), ''),
                        ',',
                        IFNULL((SELECT crd_ss.skill_id1 FROM card_rarity_data crd INNER JOIN skill_set crd_ss ON crd_ss.id = crd.skill_set WHERE crd.card_id = card.id AND crd_ss.skill_id1 >= 100000 AND crd_ss.skill_id1 < 200000 LIMIT 1), ''),
                        ',',
                        IFNULL((SELECT GROUP_CONCAT(DISTINCT ass.skill_id ORDER BY ass.skill_id SEPARATOR ',') FROM available_skill_set ass WHERE ass.available_skill_set_id = card.available_skill_set_id), '')
                    )) as SkillIds
                    
                FROM chara_data cd
                LEFT JOIN text_data t_name ON t_name.`index` = cd.id AND t_name.category = 6
                LEFT JOIN text_data t_cv ON t_cv.`index` = cd.id AND t_cv.category = 7
                LEFT JOIN card_data card ON card.chara_id = cd.id
                LEFT JOIN card_rarity_data r5 ON r5.card_id = card.id AND r5.rarity = 5
                LEFT JOIN support_card_data scd ON scd.chara_id = cd.id
                LEFT JOIN single_mode_route ura ON ura.chara_id = cd.id AND ura.scenario_id = 0
                WHERE cd.id = @charaId AND t_name.text IS NOT NULL
                GROUP BY cd.id";

            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.Add(new MySqlParameter("@charaId", charaId));

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new TerumiCharacterData
                            {
                                CharaId = reader.GetInt32(reader.GetOrdinal("CharaId")),
                                CharaName = reader.IsDBNull(reader.GetOrdinal("CharaName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("CharaName")),
                                VoiceActor = reader.IsDBNull(reader.GetOrdinal("VoiceActor"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("VoiceActor")),
                                CardId = reader.IsDBNull(reader.GetOrdinal("CardId"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("CardId")),
                                SupportCardId = reader.IsDBNull(reader.GetOrdinal("SupportCardId"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("SupportCardId")),
                                StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate"))
                                    ? null
                                    : reader.GetDateTime(reader.GetOrdinal("StartDate")),

                                BaseSpeed = reader.IsDBNull(reader.GetOrdinal("BaseSpeed"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BaseSpeed")),
                                BaseStamina = reader.IsDBNull(reader.GetOrdinal("BaseStamina"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BaseStamina")),
                                BasePower = reader.IsDBNull(reader.GetOrdinal("BasePower"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BasePower")),
                                BaseGuts = reader.IsDBNull(reader.GetOrdinal("BaseGuts"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BaseGuts")),
                                BaseWisdom = reader.IsDBNull(reader.GetOrdinal("BaseWisdom"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BaseWisdom")),

                                TalentSpeed = reader.IsDBNull(reader.GetOrdinal("TalentSpeed"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("TalentSpeed")),
                                TalentStamina = reader.IsDBNull(reader.GetOrdinal("TalentStamina"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("TalentStamina")),
                                TalentPower = reader.IsDBNull(reader.GetOrdinal("TalentPower"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("TalentPower")),
                                TalentGuts = reader.IsDBNull(reader.GetOrdinal("TalentGuts"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("TalentGuts")),
                                TalentWisdom = reader.IsDBNull(reader.GetOrdinal("TalentWisdom"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("TalentWisdom")),

                                AptitudeTurf = reader.IsDBNull(reader.GetOrdinal("AptitudeTurf"))
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeTurf"))
                                    ),
                                AptitudeDirt = reader.IsDBNull(reader.GetOrdinal("AptitudeDirt"))
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeDirt"))
                                    ),
                                AptitudeShort = reader.IsDBNull(reader.GetOrdinal("AptitudeShort"))
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeShort"))
                                    ),
                                AptitudeMile = reader.IsDBNull(reader.GetOrdinal("AptitudeMile"))
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeMile"))
                                    ),
                                AptitudeMiddle = reader.IsDBNull(
                                    reader.GetOrdinal("AptitudeMiddle")
                                )
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeMiddle"))
                                    ),
                                AptitudeLong = reader.IsDBNull(reader.GetOrdinal("AptitudeLong"))
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeLong"))
                                    ),
                                AptitudeRunner = reader.IsDBNull(
                                    reader.GetOrdinal("AptitudeRunner")
                                )
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeRunner"))
                                    ),
                                AptitudeLeader = reader.IsDBNull(
                                    reader.GetOrdinal("AptitudeLeader")
                                )
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeLeader"))
                                    ),
                                AptitudeBetweener = reader.IsDBNull(
                                    reader.GetOrdinal("AptitudeBetweener")
                                )
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeBetweener"))
                                    ),
                                AptitudeChaser = reader.IsDBNull(
                                    reader.GetOrdinal("AptitudeChaser")
                                )
                                    ? null
                                    : GetAptitudeRank(
                                        reader.GetInt32(reader.GetOrdinal("AptitudeChaser"))
                                    ),

                                BirthYear = reader.IsDBNull(reader.GetOrdinal("BirthYear"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BirthYear")),
                                BirthMonth = reader.IsDBNull(reader.GetOrdinal("BirthMonth"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BirthMonth")),
                                BirthDay = reader.IsDBNull(reader.GetOrdinal("BirthDay"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("BirthDay")),
                                Sex = reader.IsDBNull(reader.GetOrdinal("Sex"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("Sex")),
                                Height = reader.IsDBNull(reader.GetOrdinal("Height"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("Height")),
                                Bust = reader.IsDBNull(reader.GetOrdinal("Bust"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("Bust")),
                                CharaCategory = reader.IsDBNull(reader.GetOrdinal("CharaCategory"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("CharaCategory")),
                                UraObjectives = reader.IsDBNull(reader.GetOrdinal("UraObjectives"))
                                    ? null
                                    : reader.GetInt32(reader.GetOrdinal("UraObjectives")),
                                SkillIds = reader.IsDBNull(reader.GetOrdinal("SkillIds"))
                                    ? ""
                                    : RemoveDuplicateSkillIds(
                                        reader.GetString(reader.GetOrdinal("SkillIds"))
                                    ),
                            };
                        }
                    }
                }
            }

            return NotFound();
        }

        private string GetAptitudeRank(int value)
        {
            return value switch
            {
                1 => "G",
                2 => "F",
                3 => "E",
                4 => "D",
                5 => "C",
                6 => "B",
                7 => "A",
                8 => "S",
                _ => "Unknown",
            };
        }

        private string RemoveDuplicateSkillIds(string skillIds)
        {
            if (string.IsNullOrEmpty(skillIds))
                return skillIds;

            var skillIdArray = skillIds
                .Split(',')
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .ToArray();

            return string.Join(",", skillIdArray);
        }
    }
}
