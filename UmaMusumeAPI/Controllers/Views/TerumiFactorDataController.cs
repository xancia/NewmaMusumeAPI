using System.Collections.Generic;
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
    public class TerumiFactorDataController : ControllerBase
    {
        private readonly string _connectionString;

        public TerumiFactorDataController(UmaMusumeDbContext context)
        {
            _connectionString = context.Database.GetConnectionString();
        }

        // GET: api/TerumiFactorData
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TerumiFactorData>>> GetFactorData()
        {
            var result = new List<TerumiFactorData>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT 
                        sf.factor_id as Id,
                        IFNULL(td_name.text, 'Unknown') as Name,
                        IFNULL(td_desc.text, 'Unknown description') as Description,
                        sf.rarity as Rarity,
                        sf.grade as Grade,
                        sf.factor_type as Type
                    FROM succession_factor sf
                    LEFT JOIN text_data td_name ON td_name.category = 147 AND td_name.`index` = sf.factor_id
                    LEFT JOIN text_data td_desc ON td_desc.category = 172 AND td_desc.`index` = sf.factor_id
                    ORDER BY sf.factor_id";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(
                            new TerumiFactorData
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("Name"),
                                Description = reader.GetString("Description"),
                                Rarity = reader.GetInt32("Rarity"),
                                Grade = reader.GetInt32("Grade"),
                                Type = reader.GetInt32("Type"),
                            }
                        );
                    }
                }
            }

            return result;
        }

        // GET: api/TerumiFactorData/debug/categories
        [HttpGet("debug/categories")]
        public async Task<ActionResult<object>> GetTextCategories()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT DISTINCT category, COUNT(*) as count 
                    FROM text_data 
                    GROUP BY category 
                    ORDER BY category 
                    LIMIT 50";

                var result = new List<object>();
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(
                            new
                            {
                                Category = reader.GetInt32("category"),
                                Count = reader.GetInt32("count"),
                            }
                        );
                    }
                }

                return result;
            }
        }

        // GET: api/TerumiFactorData/debug/text/101
        [HttpGet("debug/text/{factorId}")]
        public async Task<ActionResult<object>> GetFactorText(int factorId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT category, `index`, text 
                    FROM text_data 
                    WHERE `index` = @factorId 
                    ORDER BY category";

                var result = new List<object>();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@factorId", factorId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(
                                new
                                {
                                    Category = reader.GetInt32("category"),
                                    Index = reader.GetInt32("index"),
                                    Text = reader.GetString("text"),
                                }
                            );
                        }
                    }
                }

                return result;
            }
        }

        // GET: api/TerumiFactorData/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TerumiFactorData>> GetFactorData(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query =
                    @"
                    SELECT 
                        sf.factor_id as Id,
                        IFNULL(td_name.text, 'Unknown') as Name,
                        IFNULL(td_desc.text, 'Unknown description') as Description,
                        sf.rarity as Rarity,
                        sf.grade as Grade,
                        sf.factor_type as Type
                    FROM succession_factor sf
                    LEFT JOIN text_data td_name ON td_name.category = 68 AND td_name.`index` = sf.factor_id
                    LEFT JOIN text_data td_desc ON td_desc.category = 69 AND td_desc.`index` = sf.factor_id
                    WHERE sf.factor_id = @id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new TerumiFactorData
                            {
                                Id = reader.GetInt32("Id"),
                                Name = reader.GetString("Name"),
                                Description = reader.GetString("Description"),
                                Rarity = reader.GetInt32("Rarity"),
                                Grade = reader.GetInt32("Grade"),
                                Type = reader.GetInt32("Type"),
                            };
                        }
                    }
                }
            }

            return NotFound();
        }

        private (string name, string description, int type) GetFactorNameDescType(int id)
        {
            // Based on your factor.json pattern: 101-103=Speed, 201-203=Stamina, 301-303=Power, 401-403=Guts, 501-503=Wit
            var baseId = id / 100 * 100; // Get first digit (100s place)

            return baseId switch
            {
                100 => ("Speed", "A Spark that increases Speed.", 1),
                200 => ("Stamina", "A Spark that increases Stamina.", 1),
                300 => ("Power", "A Spark that increases Power.", 1),
                400 => ("Guts", "A Spark that increases Guts.", 1),
                500 => ("Wit", "A Spark that increases Wit.", 1),
                _ => ("Unknown Factor", "Unknown factor description.", 1),
            };
        }
    }
}
