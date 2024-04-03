-- ----------------------------
-- Table structure for tblCompareLog
-- ----------------------------
DROP TABLE IF EXISTS `tblCompareLog`;
CREATE TABLE `tblCompareLog` (
  `seq` bigint NOT NULL AUTO_INCREMENT,
  `userName` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `connectionString1` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `connectionString2` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `tableName` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `isDifferent` tinyint(1) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`seq`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ----------------------------
-- Table structure for tblDumpLog
-- ----------------------------
DROP TABLE IF EXISTS `tblDumpLog`;
CREATE TABLE `tblDumpLog` (
  `seq` int NOT NULL AUTO_INCREMENT,
  `userName` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `connectionString` varchar(255) NOT NULL,
  `tableName` varchar(60) NOT NULL,
  `time` datetime NOT NULL,
  PRIMARY KEY (`seq`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb3;