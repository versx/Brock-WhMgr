ALTER TABLE `subscriptions`
DROP COLUMN `distance`;

ALTER TABLE `subscriptions`
DROP COLUMN `latitude`;

ALTER TABLE `subscriptions`
DROP COLUMN `longitude`;

ALTER TABLE `subscriptions`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `pokemon`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `pvp`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `raids`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `quests`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `invasions`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `lures`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

ALTER TABLE `gyms`
ADD COLUMN `location` varchar(32) DEFAULT NULL;

CREATE TABLE `locations` (
    `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
    `subscription_id` int(11) NOT NULL DEFAULT 0,
    `guild_id` bigint(20) DEFAULT NULL,
    `user_id` bigint(20) DEFAULT NULL,
    `name` varchar(32) NOT NULL,
    `distance` int(11) DEFAULT 0,
    `latitude` double DEFAULT 0,
    `longitude` double DEFAULT 0,
    PRIMARY KEY (`id`),
    KEY `FK_location_subscriptions_subscription_id` (`subscription_id`),
    CONSTRAINT `FK_location_subscriptions_subscription_id` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
);

ALTER TABLE `subscriptions`
DROP COLUMN `enabled`;

ALTER TABLE `subscriptions`
ADD COLUMN `status` smallint(5) unsigned DEFAULT 127;
