ALTER TABLE `pvp`
DROP INDEX `ix_pokemon_id`;

ALTER TABLE `pvp` 
MODIFY COLUMN `pokemon_id` text NOT NULL;
