ALTER TABLE `pokemon` 
MODIFY COLUMN `pokemon_id` text NOT NULL;

ALTER TABLE `pokemon`
DROP INDEX `ix_pokemon_id`;