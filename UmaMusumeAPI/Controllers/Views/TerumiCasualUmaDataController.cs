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
    public class TerumiCasualUmaDataController : ControllerBase
    {
        private const int PlayableCardIdLimit = 9000000;

        // text_data categories
        private const int CategoryCharaName = 6;
        private const int CategoryCardTitle = 5;
        private const int CategoryDressName = 14;
        private const int CategoryDressDescription = 15;
        private const int CategorySongTitle = 16;
        private const int CategorySongCredits = 17;
        private const int CategorySongDescription = 128;
        private const int CategoryRaceName = 28;
        private const int CategoryValentineMessageMale = 271;
        private const int CategoryValentineMessageFemale = 272;
        private const int CategoryValentineGiftName = 273;

        // legend_race first_clear_item_category value that refers to a dress
        private const int RewardCategoryDress = 102;

        private readonly string _connectionString;

        public TerumiCasualUmaDataController(UmaMusumeDbContext context)
        {
            _connectionString = context.Database.GetConnectionString();
        }

        // GET: api/TerumiCasualUmaData
        /// <summary>
        /// Get casual/fun information for all playable Uma Musume: outfits per card,
        /// casual wear, songs they have vocals for (including their solo song),
        /// Valentine's gifts, and Legend Race trophies.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TerumiCasualUmaData>>> GetTerumiCasualUmaData()
        {
            return await BuildAllAsync();
        }

        // GET: api/TerumiCasualUmaData/1001
        /// <summary>
        /// Get casual/fun information for a specific Uma Musume by chara ID
        /// </summary>
        [HttpGet("{charaId}")]
        public async Task<ActionResult<TerumiCasualUmaData>> GetTerumiCasualUmaData(int charaId)
        {
            var all = await BuildAllAsync();
            var match = all.FirstOrDefault(c => c.CharaId == charaId);

            if (match == null)
                return NotFound();

            return match;
        }

        private async Task<List<TerumiCasualUmaData>> BuildAllAsync()
        {
            var charas = new List<TerumiCasualUmaData>();
            var outfitsByChara = new Dictionary<int, List<TerumiUmaOutfit>>();
            var casualByChara = new Dictionary<int, TerumiUmaOutfit>();
            var giftsByChara = new Dictionary<int, List<TerumiValentineGift>>();
            var trophiesByChara = new Dictionary<int, List<TerumiLegendRaceTrophy>>();
            var permissionsByChara = new Dictionary<int, List<int>>();
            var songsById = new Dictionary<int, SongRow>();

            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                await LoadCharasAsync(connection, charas);
                await LoadOutfitsAsync(connection, outfitsByChara);
                await LoadCasualOutfitsAsync(connection, casualByChara);
                await LoadSongsAsync(connection, songsById);
                await LoadSongPermissionsAsync(connection, permissionsByChara);
                await LoadValentineGiftsAsync(connection, giftsByChara);
                await LoadLegendRaceTrophiesAsync(connection, trophiesByChara);
            }

            List<SongRow> allCharaSongs = songsById
                .Values.Where(s => s.SongCharaType == 1)
                .OrderBy(s => s.MusicId)
                .ToList();

            foreach (TerumiCasualUmaData chara in charas)
            {
                if (outfitsByChara.TryGetValue(chara.CharaId, out var outfits))
                    chara.Outfits = outfits;

                if (casualByChara.TryGetValue(chara.CharaId, out var casual))
                    chara.CasualOutfit = casual;

                if (giftsByChara.TryGetValue(chara.CharaId, out var gifts))
                    chara.ValentineGifts = gifts;

                if (trophiesByChara.TryGetValue(chara.CharaId, out var trophies))
                    chara.LegendRaceTrophies = trophies;

                var songs = new List<TerumiUmaSong>();
                var addedMusicIds = new HashSet<int>();

                if (permissionsByChara.TryGetValue(chara.CharaId, out var musicIds))
                {
                    foreach (int musicId in musicIds.OrderBy(id => id))
                    {
                        if (!songsById.TryGetValue(musicId, out SongRow song))
                            continue;

                        if (addedMusicIds.Add(musicId))
                            songs.Add(ToUmaSong(song, isSolo: song.PermissionCount == 1));
                    }
                }

                foreach (SongRow song in allCharaSongs)
                {
                    if (addedMusicIds.Add(song.MusicId))
                        songs.Add(ToUmaSong(song, isSolo: false));
                }

                chara.Songs = songs;
            }

            return charas;
        }

        private static TerumiUmaSong ToUmaSong(SongRow song, bool isSolo)
        {
            return new TerumiUmaSong
            {
                MusicId = song.MusicId,
                Title = song.Title,
                Credits = song.Credits,
                Description = song.Description,
                IsSoloSong = isSolo,
                AllCharasSong = song.SongCharaType == 1,
                HasLivePerformance = song.HasLive,
                LiveMemberCount = song.LiveMemberCount,
                ReleasedAt = song.ReleasedAt,
            };
        }

        private async Task LoadCharasAsync(
            MySqlConnection connection,
            List<TerumiCasualUmaData> charas
        )
        {
            var query =
                @"
                SELECT DISTINCT
                    cd.id as CharaId,
                    t_name.text as CharaName
                FROM chara_data cd
                INNER JOIN card_data card ON card.chara_id = cd.id AND card.id < @playableCardIdLimit
                INNER JOIN text_data t_name ON t_name.category = @categoryCharaName AND t_name.`index` = cd.id
                ORDER BY cd.id";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new MySqlParameter("@playableCardIdLimit", PlayableCardIdLimit));
            command.Parameters.Add(new MySqlParameter("@categoryCharaName", CategoryCharaName));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                charas.Add(
                    new TerumiCasualUmaData
                    {
                        CharaId = reader.GetInt32(reader.GetOrdinal("CharaId")),
                        CharaName = reader.GetString(reader.GetOrdinal("CharaName")),
                    }
                );
            }
        }

        private async Task LoadOutfitsAsync(
            MySqlConnection connection,
            Dictionary<int, List<TerumiUmaOutfit>> outfitsByChara
        )
        {
            var query =
                @"
                SELECT
                    card.chara_id as CharaId,
                    card.id as CardId,
                    t_card.text as CardTitle,
                    r.race_dress_id as DressId,
                    t_dress.text as DressName,
                    t_desc.text as DressDescription
                FROM card_data card
                INNER JOIN (
                    SELECT card_id, MAX(race_dress_id) as race_dress_id
                    FROM card_rarity_data
                    GROUP BY card_id
                ) r ON r.card_id = card.id
                LEFT JOIN text_data t_card ON t_card.category = @categoryCardTitle AND t_card.`index` = card.id
                LEFT JOIN text_data t_dress ON t_dress.category = @categoryDressName AND t_dress.`index` = r.race_dress_id
                LEFT JOIN text_data t_desc ON t_desc.category = @categoryDressDescription AND t_desc.`index` = r.race_dress_id
                WHERE card.id < @playableCardIdLimit
                ORDER BY card.id";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new MySqlParameter("@playableCardIdLimit", PlayableCardIdLimit));
            command.Parameters.Add(new MySqlParameter("@categoryCardTitle", CategoryCardTitle));
            command.Parameters.Add(new MySqlParameter("@categoryDressName", CategoryDressName));
            command.Parameters.Add(
                new MySqlParameter("@categoryDressDescription", CategoryDressDescription)
            );

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int charaId = reader.GetInt32(reader.GetOrdinal("CharaId"));

                if (!outfitsByChara.TryGetValue(charaId, out var outfits))
                {
                    outfits = new List<TerumiUmaOutfit>();
                    outfitsByChara[charaId] = outfits;
                }

                outfits.Add(
                    new TerumiUmaOutfit
                    {
                        CardId = reader.GetInt32(reader.GetOrdinal("CardId")),
                        CardTitle = reader.IsDBNull(reader.GetOrdinal("CardTitle"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CardTitle")),
                        DressId = reader.GetInt32(reader.GetOrdinal("DressId")),
                        DressName = reader.IsDBNull(reader.GetOrdinal("DressName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("DressName")),
                        DressDescription = reader.IsDBNull(reader.GetOrdinal("DressDescription"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("DressDescription")),
                    }
                );
            }
        }

        private async Task LoadCasualOutfitsAsync(
            MySqlConnection connection,
            Dictionary<int, TerumiUmaOutfit> casualByChara
        )
        {
            var query =
                @"
                SELECT
                    d.chara_id as CharaId,
                    d.id as DressId,
                    t_dress.text as DressName,
                    t_desc.text as DressDescription
                FROM dress_data d
                LEFT JOIN text_data t_dress ON t_dress.category = @categoryDressName AND t_dress.`index` = d.id
                LEFT JOIN text_data t_desc ON t_desc.category = @categoryDressDescription AND t_desc.`index` = d.id
                WHERE d.condition_type = 6 AND d.chara_id > 0
                ORDER BY d.id";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new MySqlParameter("@categoryDressName", CategoryDressName));
            command.Parameters.Add(
                new MySqlParameter("@categoryDressDescription", CategoryDressDescription)
            );

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int charaId = reader.GetInt32(reader.GetOrdinal("CharaId"));

                casualByChara[charaId] = new TerumiUmaOutfit
                {
                    CardId = null,
                    CardTitle = null,
                    DressId = reader.GetInt32(reader.GetOrdinal("DressId")),
                    DressName = reader.IsDBNull(reader.GetOrdinal("DressName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("DressName")),
                    DressDescription = reader.IsDBNull(reader.GetOrdinal("DressDescription"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("DressDescription")),
                };
            }
        }

        private async Task LoadSongsAsync(
            MySqlConnection connection,
            Dictionary<int, SongRow> songsById
        )
        {
            var query =
                @"
                SELECT
                    l.music_id as MusicId,
                    l.song_chara_type as SongCharaType,
                    l.live_member_number as LiveMemberCount,
                    l.has_live as HasLive,
                    FROM_UNIXTIME(l.start_date) as ReleasedAt,
                    t_title.text as Title,
                    t_credit.text as Credits,
                    t_desc.text as Description,
                    (SELECT COUNT(*) FROM live_permission_data p WHERE p.music_id = l.music_id) as PermissionCount
                FROM live_data l
                LEFT JOIN text_data t_title ON t_title.category = @categorySongTitle AND t_title.`index` = l.music_id
                LEFT JOIN text_data t_credit ON t_credit.category = @categorySongCredits AND t_credit.`index` = l.music_id
                LEFT JOIN text_data t_desc ON t_desc.category = @categorySongDescription AND t_desc.`index` = l.music_id
                WHERE l.music_type <> 99 -- exclude system variants (MC versions, Prom Dance)
                ORDER BY l.music_id";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new MySqlParameter("@categorySongTitle", CategorySongTitle));
            command.Parameters.Add(new MySqlParameter("@categorySongCredits", CategorySongCredits));
            command.Parameters.Add(
                new MySqlParameter("@categorySongDescription", CategorySongDescription)
            );

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(reader.GetOrdinal("Title")))
                    continue;

                var song = new SongRow
                {
                    MusicId = reader.GetInt32(reader.GetOrdinal("MusicId")),
                    SongCharaType = reader.GetInt32(reader.GetOrdinal("SongCharaType")),
                    LiveMemberCount = reader.GetInt32(reader.GetOrdinal("LiveMemberCount")),
                    HasLive = reader.GetInt32(reader.GetOrdinal("HasLive")) != 0,
                    ReleasedAt = reader.IsDBNull(reader.GetOrdinal("ReleasedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ReleasedAt")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Credits = reader.IsDBNull(reader.GetOrdinal("Credits"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Credits")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Description")),
                    PermissionCount = reader.GetInt32(reader.GetOrdinal("PermissionCount")),
                };

                songsById[song.MusicId] = song;
            }
        }

        private async Task LoadSongPermissionsAsync(
            MySqlConnection connection,
            Dictionary<int, List<int>> permissionsByChara
        )
        {
            var query = @"SELECT music_id as MusicId, chara_id as CharaId FROM live_permission_data";

            await using var command = connection.CreateCommand();
            command.CommandText = query;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int charaId = reader.GetInt32(reader.GetOrdinal("CharaId"));
                int musicId = reader.GetInt32(reader.GetOrdinal("MusicId"));

                if (!permissionsByChara.TryGetValue(charaId, out var musicIds))
                {
                    musicIds = new List<int>();
                    permissionsByChara[charaId] = musicIds;
                }

                musicIds.Add(musicId);
            }
        }

        private async Task LoadValentineGiftsAsync(
            MySqlConnection connection,
            Dictionary<int, List<TerumiValentineGift>> giftsByChara
        )
        {
            var query =
                @"
                SELECT
                    t_name.`index` as GiftId,
                    t_name.text as GiftName,
                    t_male.text as MessageMaleTrainer,
                    t_female.text as MessageFemaleTrainer
                FROM text_data t_name
                LEFT JOIN text_data t_male ON t_male.category = @categoryMessageMale AND t_male.`index` = t_name.`index`
                LEFT JOIN text_data t_female ON t_female.category = @categoryMessageFemale AND t_female.`index` = t_name.`index`
                WHERE t_name.category = @categoryGiftName
                ORDER BY t_name.`index`";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(
                new MySqlParameter("@categoryMessageMale", CategoryValentineMessageMale)
            );
            command.Parameters.Add(
                new MySqlParameter("@categoryMessageFemale", CategoryValentineMessageFemale)
            );
            command.Parameters.Add(
                new MySqlParameter("@categoryGiftName", CategoryValentineGiftName)
            );

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int giftId = reader.GetInt32(reader.GetOrdinal("GiftId"));

                // Gift text ids are chara-scoped: chara_id * 100 + variant number
                int charaId = giftId / 100;

                if (!giftsByChara.TryGetValue(charaId, out var gifts))
                {
                    gifts = new List<TerumiValentineGift>();
                    giftsByChara[charaId] = gifts;
                }

                var gift = new TerumiValentineGift
                {
                    GiftId = giftId,
                    GiftName = reader.IsDBNull(reader.GetOrdinal("GiftName"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("GiftName")),
                    MessageMaleTrainer = reader.IsDBNull(reader.GetOrdinal("MessageMaleTrainer"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("MessageMaleTrainer")),
                    MessageFemaleTrainer = reader.IsDBNull(
                        reader.GetOrdinal("MessageFemaleTrainer")
                    )
                        ? null
                        : reader.GetString(reader.GetOrdinal("MessageFemaleTrainer")),
                };

                // Identical gift text is repeated once per card release; keep distinct gifts only
                bool isDuplicate = gifts.Any(g =>
                    g.GiftName == gift.GiftName
                    && g.MessageMaleTrainer == gift.MessageMaleTrainer
                    && g.MessageFemaleTrainer == gift.MessageFemaleTrainer
                );

                if (!isDuplicate)
                    gifts.Add(gift);
            }
        }

        private async Task LoadLegendRaceTrophiesAsync(
            MySqlConnection connection,
            Dictionary<int, List<TerumiLegendRaceTrophy>> trophiesByChara
        )
        {
            var query =
                @"
                SELECT
                    lr.race_instance_id as RaceInstanceId,
                    npc.chara_id as CharaId,
                    rt.trophy_id as TrophyId,
                    t_race.text as RaceName,
                    lr.first_clear_item_category_1 as RewardCategory,
                    lr.first_clear_item_id_1 as RewardId,
                    t_dress.text as RewardDressName
                FROM (
                    SELECT race_instance_id, legend_race_boss_npc_id, first_clear_item_category_1, first_clear_item_id_1
                    FROM legend_race
                    UNION
                    SELECT race_instance_id, legend_race_boss_npc_id, first_clear_item_category_1, first_clear_item_id_1
                    FROM daily_legend_race
                ) lr
                INNER JOIN legend_race_boss_npc npc ON npc.id = lr.legend_race_boss_npc_id
                LEFT JOIN race_trophy rt ON rt.race_instance_id = lr.race_instance_id AND rt.event_type = 1
                LEFT JOIN text_data t_race ON t_race.category = @categoryRaceName AND t_race.`index` = lr.race_instance_id
                LEFT JOIN text_data t_dress ON t_dress.category = @categoryDressName
                    AND t_dress.`index` = lr.first_clear_item_id_1
                    AND lr.first_clear_item_category_1 = @rewardCategoryDress
                ORDER BY lr.race_instance_id";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new MySqlParameter("@categoryRaceName", CategoryRaceName));
            command.Parameters.Add(new MySqlParameter("@categoryDressName", CategoryDressName));
            command.Parameters.Add(
                new MySqlParameter("@rewardCategoryDress", RewardCategoryDress)
            );

            var seenRaceInstances = new HashSet<int>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int raceInstanceId = reader.GetInt32(reader.GetOrdinal("RaceInstanceId"));

                // legend_race and daily_legend_race can list the same race instance
                if (!seenRaceInstances.Add(raceInstanceId))
                    continue;

                int charaId = reader.GetInt32(reader.GetOrdinal("CharaId"));
                int rewardCategory = reader.GetInt32(reader.GetOrdinal("RewardCategory"));

                if (!trophiesByChara.TryGetValue(charaId, out var trophies))
                {
                    trophies = new List<TerumiLegendRaceTrophy>();
                    trophiesByChara[charaId] = trophies;
                }

                trophies.Add(
                    new TerumiLegendRaceTrophy
                    {
                        RaceInstanceId = raceInstanceId,
                        RaceName = reader.IsDBNull(reader.GetOrdinal("RaceName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("RaceName")),
                        TrophyId = reader.IsDBNull(reader.GetOrdinal("TrophyId"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("TrophyId")),
                        RewardDressId =
                            rewardCategory == RewardCategoryDress
                                ? reader.GetInt32(reader.GetOrdinal("RewardId"))
                                : null,
                        RewardDressName = reader.IsDBNull(reader.GetOrdinal("RewardDressName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("RewardDressName")),
                    }
                );
            }
        }

        private class SongRow
        {
            public int MusicId { get; set; }
            public int SongCharaType { get; set; }
            public int LiveMemberCount { get; set; }
            public bool HasLive { get; set; }
            public System.DateTime? ReleasedAt { get; set; }
            public string Title { get; set; }
            public string Credits { get; set; }
            public string Description { get; set; }
            public int PermissionCount { get; set; }
        }
    }
}
