using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using VOL.Builder.Services;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using VOL.Builder.IRepositories;

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class SysTableInfoService_SaveEidtTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Sys_TableInfoService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _service = new Sys_TableInfoService { repository = _mockRepository.Object };
        }

        private Sys_TableInfo CreateBasicTableInfo(int tableId = 1, int? parentId = null)
        {
            return new Sys_TableInfo
            {
                Table_Id = tableId,
                ParentId = parentId,
                TableName = "TestTable",
                TableColumns = new List<Sys_TableColumn>()
            };
        }

        private Sys_TableColumn CreateColumn(string columnName, string dropNo = null)
        {
            return new Sys_TableColumn
            {
                ColumnName = columnName,
                DropNo = dropNo
            };
        }

        [TestMethod]
        public void SaveEidt_ParentIdIsSelf_ReturnsErrorResponse()
        {
            // Arrange
            var tableInfo = CreateBasicTableInfo(tableId: 1, parentId: 1);

            // Act
            WebResponseContent response = _service.SaveEidt(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("父级id不能为自己", response.Message);
        }

        [TestMethod]
        public void SaveEidt_ExpressFieldIsDropDown_ReturnsErrorResponse()
        {
            // Arrange
            var tableInfo = CreateBasicTableInfo();
            tableInfo.ExpressField = "ConflictingField";
            tableInfo.TableColumns.Add(CreateColumn("ConflictingField", dropNo: "SomeDropDown"));
            tableInfo.TableColumns.Add(CreateColumn("OtherField"));

            // Act
            WebResponseContent response = _service.SaveEidt(tableInfo);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("不能将字段【ConflictingField】设置为快捷编辑,因为已经设置了数据源", response.Message);
        }

        [TestMethod]
        public void SaveEidt_ValidInput_CallsRepositoryUpdateRangeAndReturnsSuccess()
        {
            // Arrange
            var tableInfo = CreateBasicTableInfo(tableId: 1, parentId: 2);
            tableInfo.TableColumns.Add(CreateColumn("Field1"));
            tableInfo.TableColumns.Add(CreateColumn("Field2", dropNo: "")); // Ensure DropNo is not null but empty for valid case if needed

            _mockRepository.Setup(repo => repo.UpdateRange<Sys_TableColumn>(
                                     It.IsAny<Sys_TableInfo>(), true, true, null, null, true))
                           .Returns(new WebResponseContent(true));

            // Act
            WebResponseContent response = _service.SaveEidt(tableInfo);

            // Assert
            Assert.IsTrue(response.Status, $"SaveEidt failed with message: {response.Message}");
            _mockRepository.Verify(repo => repo.UpdateRange<Sys_TableColumn>(tableInfo, true, true, null, null, true), Times.Once());
        }

        [TestMethod]
        public void SaveEidt_TableColumnsAreProcessedCorrectly()
        {
            // Arrange
            var tableInfo = CreateBasicTableInfo();
            tableInfo.TableName = "MyAwesomeTable";
            var col1 = CreateColumn("Column1");
            var col2 = CreateColumn("Column2");
            col2.IsReadDataset = 1; // Explicitly set one to ensure nulls are defaulted
            tableInfo.TableColumns.Add(col1);
            tableInfo.TableColumns.Add(col2);

            _mockRepository.Setup(repo => repo.UpdateRange<Sys_TableColumn>(
                                     It.IsAny<Sys_TableInfo>(), true, true, null, null, true))
                           .Returns(new WebResponseContent(true));

            // Act
            _service.SaveEidt(tableInfo);

            // Assert
            Assert.AreEqual("MyAwesomeTable", col1.TableName);
            Assert.AreEqual("MyAwesomeTable", col2.TableName);
            Assert.AreEqual(0, col1.IsReadDataset, "IsReadDataset should default to 0 if null.");
            Assert.AreEqual(1, col2.IsReadDataset, "IsReadDataset should retain its value if not null.");
        }
    }
}
