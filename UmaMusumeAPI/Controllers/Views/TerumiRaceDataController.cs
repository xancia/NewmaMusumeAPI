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
    public class TerumiRaceDataController : ControllerBase
    {
        private readonly string _connectionString;

        public TerumiRaceDataController(UmaMusumeDbContext context)
        {
            _connectionString = context.Database.GetConnectionString();
        }

        // GET: api/TerumiRaceData
        /// <summary>
        /// Get all races with detailed information including track, distance, ground type, and schedules
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TerumiRaceData>>> GetTerumiRaceData()
        {
            var result = new List<TerumiRaceData>();

            // First, get all race base data
            var raceQuery =
                @"
                SELECT 
                    r.id as RaceId,
                    td_name.text as RaceName,
                    r.grade as Grade,
                    rcs.distance as Distance,
                    rcs.ground as Ground,
                    rcs.turn as Turn,
                    rcs.race_track_id as TrackId,
                    td_track.text as TrackName,
                    r.entry_num as EntryNum
                FROM race r
                JOIN race_course_set rcs ON r.course_set = rcs.id
                LEFT JOIN text_data td_name ON r.id = td_name.`index` AND td_name.category = 32
                LEFT JOIN text_data td_track ON rcs.race_track_id = td_track.`index` AND td_track.category = 35
                WHERE td_name.text IS NOT NULL
                ORDER BY r.grade, r.id";

            var races = new Dictionary<int, TerumiRaceData>();

            await using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = raceQuery;

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var raceId = reader.GetInt32(reader.GetOrdinal("RaceId"));
                            var grade = reader.GetInt32(reader.GetOrdinal("Grade"));
                            var distance = reader.GetInt32(reader.GetOrdinal("Distance"));
                            var ground = reader.GetInt32(reader.GetOrdinal("Ground"));
                            var turn = reader.GetInt32(reader.GetOrdinal("Turn"));

                            var race = new TerumiRaceData
                            {
                                RaceId = raceId,
                                RaceName = reader.IsDBNull(reader.GetOrdinal("RaceName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("RaceName")),
                                Grade = grade,
                                GradeName = GetGradeName(grade),
                                Distance = distance,
                                DistanceCategory = GetDistanceCategory(distance),
                                Ground = ground,
                                GroundName = GetGroundName(ground),
                                Turn = turn,
                                TurnName = GetTurnName(turn),
                                TrackId = reader.GetInt32(reader.GetOrdinal("TrackId")),
                                TrackName = reader.IsDBNull(reader.GetOrdinal("TrackName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("TrackName")),
                                EntryNum = reader.GetInt32(reader.GetOrdinal("EntryNum")),
                                Schedules = new List<RaceSchedule>(),
                            };

                            races[raceId] = race;
                        }
                    }
                }
            }

            // Now get all race instances (schedules)
            var instanceQuery =
                @"
                SELECT 
                    ri.id as InstanceId,
                    ri.race_id as RaceId,
                    ri.date as Date,
                    ri.time as Time
                FROM race_instance ri
                WHERE ri.date > 0
                ORDER BY ri.race_id, ri.date";

            await using (var connection2 = new MySqlConnection(_connectionString))
            {
                await connection2.OpenAsync();
                await using (var command = connection2.CreateCommand())
                {
                    command.CommandText = instanceQuery;

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var raceId = reader.GetInt32(reader.GetOrdinal("RaceId"));

                            if (!races.ContainsKey(raceId))
                                continue;

                            var date = reader.GetInt32(reader.GetOrdinal("Date"));
                            var time = reader.GetInt32(reader.GetOrdinal("Time"));

                            // Date format is MMDD (e.g., 825 = August 25)
                            var month = date / 100;
                            var day = date % 100;
                            var half = day <= 15 ? 1 : 2;

                            var schedule = new RaceSchedule
                            {
                                InstanceId = reader.GetInt32(reader.GetOrdinal("InstanceId")),
                                Month = month,
                                Day = day,
                                Half = half,
                                HalfName = half == 1 ? "First Half" : "Second Half",
                                Time = time,
                                TimeName = GetTimeName(time),
                            };

                            races[raceId].Schedules.Add(schedule);
                        }
                    }
                }
            }

            return races.Values.ToList();
        }

        // GET: api/TerumiRaceData/5
        /// <summary>
        /// Get a specific race by ID with detailed information
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TerumiRaceData>> GetTerumiRaceData(int id)
        {
            var allRaces = await GetTerumiRaceData();
            var race = allRaces.Value?.FirstOrDefault(r => r.RaceId == id);

            if (race == null)
            {
                return NotFound();
            }

            return race;
        }

        private static string GetGradeName(int grade)
        {
            return grade switch
            {
                100 => "G1",
                200 => "G2",
                300 => "G3",
                400 => "OP/L (Open/Listed)",
                700 => "Pre-OP",
                800 => "Maiden (New Horse/Unraced)",
                900 => "Debut",
                999 => "URA Finals",
                1000 => "Aoharu Cup",
                _ => $"Unknown ({grade})",
            };
        }

        private static string GetDistanceCategory(int distance)
        {
            return distance switch
            {
                <= 1400 => "Short",
                <= 1800 => "Mile",
                <= 2400 => "Middle",
                _ => "Long",
            };
        }

        private static string GetGroundName(int ground)
        {
            return ground switch
            {
                1 => "Turf",
                2 => "Dirt",
                _ => $"Unknown ({ground})",
            };
        }

        private static string GetTurnName(int turn)
        {
            return turn switch
            {
                1 => "Right",
                2 => "Left",
                4 => "Straight",
                _ => $"Unknown ({turn})",
            };
        }

        private static string GetTimeName(int time)
        {
            return time switch
            {
                2 => "Morning",
                3 => "Afternoon",
                4 => "Evening",
                _ => $"Unknown ({time})",
            };
        }
    }
}
