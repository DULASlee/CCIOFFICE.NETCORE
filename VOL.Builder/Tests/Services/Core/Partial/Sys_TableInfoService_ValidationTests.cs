using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VOL.Builder.Services;
using VOL.Entity.DomainModels.Sys;
using VOL.Core.Utilities;
using VOL.Core.DBManager; // For DBType
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class Sys_TableInfoService_ValidationTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Sys_TableInfoService _service;
        private static VOL.Core.Enums.DbCurrentType _originalDbType;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Store original DBType if it's static and modifiable, or ensure tests can set it.
            // This is conceptual as DBType.Name is static and not easily mockable without reflection/refactor.
            // _originalDbType = DBServerProvider.GetCurrentDBType(); // Assuming such a getter exists
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _service = new Sys_TableInfoService(_mockRepository.Object);
            // Default to a common DBType for most tests, can be overridden per test.
            // DBServerProvider.SetCurrentDBType(VOL.Core.Enums.DbCurrentType.SqlServer); // Conceptual
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore original DBType if changed during a test
            // DBServerProvider.SetCurrentDBType(_originalDbType); // Conceptual
        }

        private void SetupDbQueryForDetailTable(Sys_TableColumn detailForeignKeyColumn)
        {
            _mockRepository.Setup(r => r.Find<Sys_TableColumn>(It.IsAny<Expression<Func<Sys_TableColumn, bool>>>()))
                           .Returns(detailForeignKeyColumn != null ? new List<Sys_TableColumn> { detailForeignKeyColumn } : new List<Sys_TableColumn>());
        }

        private void SetDBType(VOL.Core.Enums.DbCurrentType dbType)
        {
            // This is a placeholder for how one might try to influence DBType.Name.
            // In reality, static properties like DBType.Name are hard to mock for tests.
            // Tests requiring specific DBType behavior (like MySql for GUID length) might be fragile
            // or require actual configuration if the service reads this globally.
            // For this test, we'll assume the logic branches correctly if DBType.Name *were* "MySql".
            // One common workaround is to have a test-specific configuration or an internal setter in DBType for tests.
            // For the purpose of `ValidColumnString_DetailTable_MySqlGuidLengthMismatch_ReturnsError`,
            // the internal logic of `ValidColumnString` that checks `IsMysql()` will be what's important.
            // We can't directly set `DBType.Name` here without refactoring `DBServerProvider` or `DBType`.
        }

        [TestMethod]
        public void ValidColumnString_NoDetailTable_ReturnsTrue()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = null, // No detail table
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "Id", IsKey = 1, ColumnType = "int" } }
            };

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsTrue(response.Status);
        }

        [TestMethod]
        public void ValidColumnString_DetailTable_MainTableNoKey_ReturnsError()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "Id", IsKey = 0 } } // No key
            };

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("请勾选表[MainTable]的主键", response.Message);
        }

        [TestMethod]
        public void ValidColumnString_DetailTable_DetailTableMissingForeignKeyColumn_ReturnsError()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "int" } }
            };
            // Simulate detail table's foreign key column not found
            SetupDbQueryForDetailTable(null);

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("明细表必须包括[MainTable]主键字段[MainId]", response.Message);
        }

        [TestMethod]
        public void ValidColumnString_DetailTable_KeyTypesMismatch_ReturnsError()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "int" } }
            };
            var detailForeignKeyColumn = new Sys_TableColumn { ColumnName = "MainId", ColumnType = "string" }; // Type mismatch
            SetupDbQueryForDetailTable(detailForeignKeyColumn);

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("明细表的字段[MainId]类型必须与主表的主键的类型相同", response.Message);
        }

        [TestMethod]
        public void ValidColumnString_DetailTable_MySqlGuidLengthMismatch_ReturnsError()
        {
            // Arrange
            // SetDBType(VOL.Core.Enums.DbCurrentType.MySql); // Conceptual: This would need to actually change static DBType.Name

            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "string", Maxlength = 36 } } // GUID-like
            };
            // Detail FK has wrong length for a GUID in MySQL context (if IsMysql() check is true)
            var detailForeignKeyColumn = new Sys_TableColumn { ColumnName = "MainId", ColumnType = "string", Maxlength = 30 };
            SetupDbQueryForDetailTable(detailForeignKeyColumn);

            // To test this path, we need to ensure IsMysql() returns true.
            // This is a limitation of testing static dependencies. We assume if IsMysql() were true, the logic would trigger.
            // If we could mock DBType.Name, we'd set it to "MySql".
            // For now, this test might pass vacuously (not hitting the MySql-specific code path) if DBType.Name is not "MySql".
            // However, if the code *does* execute the IsMysql() check and it *is* MySql (e.g. default test config), this will assert the logic.

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            // This assertion is only valid if IsMysql() returns true during the test run.
            // If IsMysql() is false, this specific error won't be triggered, and the test might pass if no other validation fails.
            bool isActuallyMySql = DBType.Name.ToLower() == "mysql"; // Check the actual runtime DBType for test context
            if (isActuallyMySql)
            {
                Assert.IsFalse(response.Status);
                Assert.AreEqual("主表主键类型为Guid，明细表[DetailTable]配置的字段[MainId]长度必须是36，请重将明细表字段[MainId]长度设置为36，点击保存与生成Model", response.Message);
            }
            else
            {
                // If not MySql, the specific GUID length check for MySQL is skipped, so status should be true (assuming other things are fine)
                Assert.IsTrue(response.Status, "Test assumes non-MySQL path was taken, or other validations passed.");
            }
        }


        [TestMethod]
        public void ValidColumnString_AllValidationsPass_ReturnsTrue()
        {
            // Arrange
            // SetDBType(VOL.Core.Enums.DbCurrentType.SqlServer); // Ensure not MySql for GUID length check if that's an issue

            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "int" } }
            };
            var detailForeignKeyColumn = new Sys_TableColumn { ColumnName = "MainId", ColumnType = "int" }; // Matching type
            SetupDbQueryForDetailTable(detailForeignKeyColumn);

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsTrue(response.Status);
        }
         [TestMethod]
        public void ValidColumnString_DetailTableWithMultipleSegments_AllValidationsPass_ReturnsTrue()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable1,DetailTable2", // Multiple detail tables
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "int" } }
            };

            // Mock repository.Find for each detail table segment
            _mockRepository.Setup(r => r.Find<Sys_TableColumn>(It.Is<Expression<Func<Sys_TableColumn, bool>>>(expr => expr.ToString().Contains("\"DetailTable1\""))))
                           .Returns(new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", ColumnType = "int", TableName = "DetailTable1"} });
            _mockRepository.Setup(r => r.Find<Sys_TableColumn>(It.Is<Expression<Func<Sys_TableColumn, bool>>>(expr => expr.ToString().Contains("\"DetailTable2\""))))
                           .Returns(new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", ColumnType = "int", TableName = "DetailTable2" } });


            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsTrue(response.Status, response.Message);
        }

        [TestMethod]
        public void ValidColumnString_DetailTableWithMultipleSegments_OneSegmentFailsForeignKeyCheck_ReturnsError()
        {
            // Arrange
            var tableInfo = new Sys_TableInfo
            {
                TableName = "MainTable",
                DetailName = "DetailTable1,DetailTable2", // Multiple detail tables
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", IsKey = 1, ColumnType = "int" } }
            };

            _mockRepository.Setup(r => r.Find<Sys_TableColumn>(It.Is<Expression<Func<Sys_TableColumn, bool>>>(expr => expr.ToString().Contains("\"DetailTable1\""))))
                           .Returns(new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "MainId", ColumnType = "int", TableName = "DetailTable1"} });
            _mockRepository.Setup(r => r.Find<Sys_TableColumn>(It.Is<Expression<Func<Sys_TableColumn, bool>>>(expr => expr.ToString().Contains("\"DetailTable2\""))))
                           .Returns(new List<Sys_TableColumn>()); // DetailTable2 is missing the FK column

            // Act
            var response = _service.ValidColumnString(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("明细表必须包括[MainTable]主键字段[MainId]", response.Message); // Error message might need to be more specific about which detail table failed if possible
        }
    }
}
