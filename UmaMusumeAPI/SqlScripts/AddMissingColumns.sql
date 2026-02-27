--
-- Add missing columns to existing tables
-- Run this in Azure Data Studio to update your schema
-- WARNING: This is a comprehensive fix - run only once!
--
SET NAMES 'utf8mb4';
USE umamusume;

-- =====================================================
-- DROP AND RECREATE problematic tables with full schema
-- =====================================================

-- jobs_reward
DROP TABLE IF EXISTS jobs_reward;
CREATE TABLE jobs_reward (
  id BIGINT NOT NULL,
  limited_schedule_id BIGINT NOT NULL DEFAULT 0,
  PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- main_story_data: add is_custom_load
ALTER TABLE main_story_data 
ADD COLUMN IF NOT EXISTS is_custom_load BIGINT NOT NULL DEFAULT 0;

-- mini_bg: add env_id  
ALTER TABLE mini_bg 
ADD COLUMN IF NOT EXISTS env_id BIGINT NOT NULL DEFAULT 0;

-- paid_gacha_button_type
ALTER TABLE paid_gacha_button_type 
ADD COLUMN IF NOT EXISTS draw_guarantee_type BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS draw_guarantee_num BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS draw_count BIGINT NOT NULL DEFAULT 0;

-- race_bgm_cutin: add is_jingle
ALTER TABLE race_bgm_cutin 
ADD COLUMN IF NOT EXISTS is_jingle BIGINT NOT NULL DEFAULT 0 AFTER exclusive_control;

-- single_mode_chara_effect_buff: add many columns
ALTER TABLE single_mode_chara_effect_buff 
ADD COLUMN IF NOT EXISTS start_turn BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS end_turn BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS turf_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS dirt_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS short_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS mile_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS middle_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS long_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS nige_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS senko_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS sashi_add BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS oikomi_add BIGINT NOT NULL DEFAULT 0;

-- single_mode_route_race: add alt_determine_race, alt_determine_race_flag
ALTER TABLE single_mode_route_race 
ADD COLUMN IF NOT EXISTS alt_determine_race BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS alt_determine_race_flag BIGINT NOT NULL DEFAULT 0;

-- single_mode_story_data: add many columns
ALTER TABLE single_mode_story_data 
ADD COLUMN IF NOT EXISTS gallery_gruop_id BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS gallery_sort BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS gallery_condition BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS gallery_suggest_event BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS available_gallery_key BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS past_race_id BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS past_race_id_2 BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS past_race_id_3 BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS past_race_id_4 BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS force_use_race_dress BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS event_category BIGINT NOT NULL DEFAULT 0;

-- single_mode_training: add voice_trigger_type
ALTER TABLE single_mode_training 
ADD COLUMN IF NOT EXISTS voice_trigger_type BIGINT NOT NULL DEFAULT 0;

-- story_event_mission_top_chara: add dress_color, chara_dress_color_set_id
ALTER TABLE story_event_mission_top_chara 
ADD COLUMN IF NOT EXISTS dress_color BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS chara_dress_color_set_id BIGINT NOT NULL DEFAULT 0;

-- story_event_top_chara: add dress_color, chara_dress_color_set_id
ALTER TABLE story_event_top_chara 
ADD COLUMN IF NOT EXISTS dress_color BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS chara_dress_color_set_id BIGINT NOT NULL DEFAULT 0;

-- story_extra_movie_data: add movie_type
ALTER TABLE story_extra_movie_data 
ADD COLUMN IF NOT EXISTS movie_type BIGINT NOT NULL DEFAULT 0;

-- support_card_data: add multiple columns
ALTER TABLE support_card_data 
ADD COLUMN IF NOT EXISTS exchange_item_num BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS disp_order BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS outing_max BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS effect_id BIGINT NOT NULL DEFAULT 0;

-- support_card_team_score_bonus: add multiple columns
ALTER TABLE support_card_team_score_bonus 
ADD COLUMN IF NOT EXISTS bonus_group BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS rarity BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS limit_break_count BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS score_rate_per_card BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS start_date BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS end_date BIGINT NOT NULL DEFAULT 0;

-- team_building_data: add ticket_item_id, scout_pt_item_id
ALTER TABLE team_building_data 
ADD COLUMN IF NOT EXISTS ticket_item_id BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS scout_pt_item_id BIGINT NOT NULL DEFAULT 0;

-- topics: add end_date
ALTER TABLE topics 
ADD COLUMN IF NOT EXISTS end_date VARCHAR(255) NOT NULL DEFAULT '';

-- ultimate_race_contents: add is_hidden_race, unlock_condition_id
ALTER TABLE ultimate_race_contents 
ADD COLUMN IF NOT EXISTS is_hidden_race BIGINT NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS unlock_condition_id BIGINT NOT NULL DEFAULT 0;

-- ultimate_race_data: add archive_start_date
ALTER TABLE ultimate_race_data 
ADD COLUMN IF NOT EXISTS archive_start_date BIGINT NOT NULL DEFAULT 0;

-- ultimate_race_npc: add skill_set_id_3
ALTER TABLE ultimate_race_npc 
ADD COLUMN IF NOT EXISTS skill_set_id_3 BIGINT NOT NULL DEFAULT 0;

SELECT 'All missing columns added successfully!' AS Result;
