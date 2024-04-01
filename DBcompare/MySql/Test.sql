-- ----------------------------
-- Table structure for compareLog
-- ----------------------------
DROP TABLE IF EXISTS `compareLog`;
CREATE TABLE `compareLog` (
  `seq` bigint NOT NULL AUTO_INCREMENT,
  `connectionString1` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `connectionString2` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `tableName` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `isDifferent` tinyint(1) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`seq`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ----------------------------
-- Table structure for dumpLog
-- ----------------------------
DROP TABLE IF EXISTS `dumpLog`;
CREATE TABLE `dumpLog` (
  `seq` int NOT NULL AUTO_INCREMENT,
  `connectionString` varchar(255) NOT NULL,
  `tableName` varchar(60) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`seq`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb3;