# Uma Musume REST API (Fork) <br/> [![Build](https://ci.appveyor.com/api/projects/status/e3dq3bvxffkmmpty/branch/master?svg=true)](https://ci.appveyor.com/project/SimpleSandman/umamusumeapi/branch/master) [![Codacy Badge](https://app.codacy.com/project/badge/Grade/e77ffc16dc4c4eeabc2d2618538a2d17)](https://www.codacy.com/gh/SimpleSandman/UmaMusumeAPI/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=SimpleSandman/UmaMusumeAPI&amp;utm_campaign=Badge_Grade) [![codecov](https://codecov.io/gh/SimpleSandman/UmaMusumeAPI/branch/master/graph/badge.svg?token=COWCEBUUO6)](https://codecov.io/gh/SimpleSandman/UmaMusumeAPI)

> **Note:** This is a fork of the [original UmaMusumeAPI](https://github.com/SimpleSandman/UmaMusumeAPI) by SimpleSandman. The original project has been retired as of October 29th. This fork includes custom endpoints and modifications for personal use.

This is a community REST API based on [Uma Musume: Pretty Derby](https://umamusume.jp/)'s `meta` and `master.mdb` files that is read from a MariaDB database. This is based on the Swagger UI/OpenAPI specification. I'm using MariaDB instead of MySQL for the sake of keeping as much of this as open-source as possible.

The original [loader app](https://github.com/SimpleSandman/UmaMusumeLoadSqlData) allows you to load the `meta` and `master.mdb`'s data from the [DMM version](https://dmg.umamusume.jp/) of this game into a MySQL/MariaDB database.

## Custom Endpoints Added in This Fork

### TerumiSimpleSkillData
A simplified skill data endpoint that provides parsed and human-readable skill information, including effect breakdowns and activation conditions.

**Endpoints:**
- `GET /api/TerumiSimpleSkillData` - Returns all skills
- `GET /api/TerumiSimpleSkillData/{skillId}` - Returns a specific skill by ID

**Example Response:**
```json
{
  "skillId": 200162,
  "rarity": 1,
  "gradeValue": 50,
  "skillCategory": "Passive",
  "tagId": "601",
  "activationCondition": "ground_type==1",
  "precondition": "",
  "effects": [
    {
      "type": "Power Stat",
      "value": 40,
      "displayText": "Power +40"
    }
  ],
  "effectSummary": "Power +40",
  "iconId": 200162,
  "skillName": "Wet Conditions ○",
  "skillDesc": "Slightly increase power when on a muddy track.",
  "needSkillPoint": 50
}
```

**How to Use:**
```bash
# Get all skills
curl -X GET "http://localhost:5000/api/TerumiSimpleSkillData" -H "accept: application/json"

# Get specific skill by ID
curl -X GET "http://localhost:5000/api/TerumiSimpleSkillData/200162" -H "accept: application/json"
```

**Fields Explained:**
- `skillCategory`: Human-readable category (Passive, Active, Debuff, Recovery, Unique, Acceleration)
- `effects`: Array of parsed effects with type, numeric value, and display text
- `effectSummary`: Concatenated string of all effects (e.g., "Power +40 | Speed +20")
- `needSkillPoint`: Skill points required to learn (from `single_mode_skill_need_point` table)

### TerumiCasualUmaData
Casual/"fun facts" data per playable Uma Musume, intended for a non-mechanics character tab: outfits, casual wear, songs, Valentine's gifts, and Legend Race trophies.

**Endpoints:**
- `GET /api/TerumiCasualUmaData` - Returns all playable characters
- `GET /api/TerumiCasualUmaData/{charaId}` - Returns a specific character by chara ID (e.g. `1001`)

**Example Response (one record):**
```json
{
  "charaId": 1001,
  "charaName": "Special Week",
  "outfits": [
    {
      "cardId": 100101,
      "cardTitle": "[Special Dreamer]",
      "dressId": 100101,
      "dressName": "Special Dreamer",
      "dressDescription": "Special Week's original outfit."
    }
  ],
  "casualOutfit": {
    "cardId": null,
    "cardTitle": null,
    "dressId": 901001,
    "dressName": "Special Week's Casual Wear",
    "dressDescription": "Special Week's casual attire."
  },
  "songs": [
    {
      "musicId": 1004,
      "title": "Never Looking Back",
      "credits": "Lyrics: yura\\nMusic / Arrangement: ...",
      "description": "Our rivalry is what brought us together...",
      "isSoloSong": false,
      "allCharasSong": false,
      "hasLivePerformance": true,
      "liveMemberCount": 18,
      "releasedAt": "2025-06-26T22:00:00"
    }
  ],
  "valentineGifts": [
    {
      "giftId": 100101,
      "giftName": "Special Week's\\nSuper Swell Choco Carrot",
      "messageMaleTrainer": "...",
      "messageFemaleTrainer": "..."
    }
  ],
  "legendRaceTrophies": [
    {
      "raceInstanceId": 592002,
      "raceName": "Japan Cup (Hard)",
      "trophyId": 5921,
      "rewardDressId": 100101,
      "rewardDressName": "Special Dreamer"
    }
  ]
}
```

**Data Sources & Semantics:**
- `outfits`: One entry per playable card (alternate versions of the character). The racing outfit comes from `card_rarity_data.race_dress_id` joined to `dress_data`/`text_data` (categories 14/15 for name/description).
- `casualOutfit`: `dress_data` rows with `condition_type = 6` (dress ID pattern `900000 + charaId`). `null` for characters whose casual wear isn't released yet.
- `songs`: Songs the character has vocals for, from `live_permission_data` plus songs every uma can sing (`live_data.song_chara_type = 1`, e.g. "Umapyoi Legend", flagged `allCharasSong`). A song where the character is the *only* permitted singer is flagged `isSoloSong` (their solo song). Note: jukebox-only songs with fixed baked-in singers (e.g. "Bakushin Bakushin Bakushinshin") have no permission data in `master.mdb` and are not attributable per character.
- `valentineGifts`: From `text_data` categories 273 (gift name), 271 (message, male-trainer variant), and 272 (message, female-trainer variant). Gift IDs are `charaId * 100 + variant`.
- `legendRaceTrophies`: Legend Races where the character appears as the boss (`legend_race` / `daily_legend_race` → `legend_race_boss_npc`). `trophyId` comes from `race_trophy` (`event_type = 1`) and the first-clear reward is the character's racing outfit (`first_clear_item_category_1 = 102` → dress).

> **Note:** Unique skill animations and winning animations are not stored in `master.mdb` (they are game assets). Those are expected to be provided by the consuming app (e.g. a curated YouTube playlist keyed by `charaId`/`cardId`).

# Initial Setup
Under `UmaMusumeAPI/Properties/launchSettings.json`, set the `MARIA_CONNECTION_STRING` environment variable to your MariaDB database for "development" and on the hosting site's config variables section for "release".

Simplified `launchSettings.json` Example:

```json
"profiles": {
  "IIS Express": {
    "environmentVariables": {
      "ASPNETCORE_ENVIRONMENT": "Development",
      "MARIA_CONNECTION_STRING": "user id=;password=;host=;database=;character set=utf8mb4"
    }
  }
}
```

# Database Setup
Use the scripts in [`UmaMusumeAPI/SqlScripts`](https://github.com/SimpleSandman/UmaMusumeAPI/tree/master/UmaMusumeAPI/SqlScripts) to generate everything you need for the database. As mentioned before, if you want to load all of the data from the DMM version of this game, use my [loader app](https://github.com/SimpleSandman/UmaMusumeLoadSqlData) and point the connection string to your new database.

Make sure the MariaDB database and all of its objects have the character set of `utf8mb4` and collation of `utf8mb4_general_ci` as that is the official UTF-8 specification. There are not only so many articles on this topic, but the devs from the [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) project recommends this personally and [in this issue](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1427). I'm using their EF Core library to help scaffold the models and controllers since it's far more active and stable than the Oracle equivalent.

# Scaffold Commands
*In case if you need to scaffold anything, here are some commands that may be useful*

## Models and DbContext

This is a single-line command using the "Package Manager Console" in Visual Studio that allows you to generate **ALL** of the models and the DbContext class.
```powershell
Scaffold-DbContext 'User Id=;Password=;Host=;Database=;Character Set=utf8mb4' Pomelo.EntityFrameworkCore.MySql -OutputDir Models -ContextDir Context
```

If you only need the model and context of a **SINGLE** table, here's the single-line command for that.
```powershell
Scaffold-DbContext 'User Id=;Password=;Host=;Database=;Character Set=utf8mb4' Pomelo.EntityFrameworkCore.MySql -OutputDir Models -ContextDir Context -T <TABLE_NAME_HERE>
```
