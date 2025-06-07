using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VOL.Builder.Services;
using VOL.Entity.DomainModels.Sys;
using VOL.Core.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using System.Threading.Tasks; // For Task

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class Sys_TableInfoService_DelTreeTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Sys_TableInfoService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _service = new Sys_TableInfoService(_mockRepository.Object);
        }

        [TestMethod]
        public async Task DelTree_TableIdIsZero_ReturnsError()
        {
            // Arrange
            int tableId = 0;

            // Act
            var response = await _service.DelTree(tableId);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("没有传入参数", response.Message);
        }

        [TestMethod]
        public async Task DelTree_TableInfoNotFound_ReturnsOk()
        {
            // Arrange
            int tableId = 1;
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo>().AsQueryable().BuildMock()); // Simulate not found

            // Act
            var response = await _service.DelTree(tableId);

            // Assert
            Assert.IsTrue(response.Status);
            Assert.IsNull(response.Message); // Or specific OK message if any
        }

        [TestMethod]
        public async Task DelTree_TableHasColumns_ReturnsError()
        {
            // Arrange
            int tableId = 2;
            var tableInfoWithColumns = new Sys_TableInfo
            {
                Table_Id = tableId,
                TableName = "TableWithCols",
                TableColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "Col1" } }
            };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfoWithColumns }.AsQueryable().BuildMock());

            // Act
            var response = await _service.DelTree(tableId);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("当前删除的节点存在表结构信息,只能删除空节点", response.Message);
        }

        [TestMethod]
        public async Task DelTree_TableHasChildNodes_ReturnsError()
        {
            // Arrange
            int tableId = 3;
            var tableInfoNoColumns = new Sys_TableInfo { Table_Id = tableId, TableName = "ParentTable", TableColumns = new List<Sys_TableColumn>() };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfoNoColumns }.AsQueryable().BuildMock());
            _mockRepository.Setup(r => r.Exists(It.Is<Expression<Func<Sys_TableInfo, bool>>>(expr => true))) // Simplified, refine if needed
                           .Returns(true); // Simulate child nodes exist

            // Act
            var response = await _service.DelTree(tableId);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual("当前删除的节点存在子节点，不能删除", response.Message);
        }

        [TestMethod]
        public async Task DelTree_ValidLeafNode_DeletesSuccessfullyAndReturnsOk()
        {
            // Arrange
            int tableId = 4;
            var validLeafTableInfo = new Sys_TableInfo { Table_Id = tableId, TableName = "LeafTable", TableColumns = new List<Sys_TableColumn>() };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { validLeafTableInfo }.AsQueryable().BuildMock());
            _mockRepository.Setup(r => r.Exists(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(false); // No child nodes
            _mockRepository.Setup(r => r.Delete(It.IsAny<Sys_TableInfo>(), true))
                           .Returns(new WebResponseContent { Status = true });


            // Act
            var response = await _service.DelTree(tableId);

            // Assert
            Assert.IsTrue(response.Status);
            _mockRepository.Verify(r => r.Delete(It.Is<Sys_TableInfo>(tbl => tbl.Table_Id == tableId), true), Times.Once);
        }
    }

    // Re-use or ensure MockQueryableExtensions is available if FindAsIQueryable needs async mocking.
    // For these tests, it seems the async nature of FindAsIQueryable is handled by .Result or similar,
    // but for consistency and future-proofing, it's good to have the async mocking helpers.
    // If not already present from other test files in the same assembly, include them:
    // public static class MockQueryableExtensions { ... }
    // internal class TestAsyncQueryProvider<TEntity> { ... }
    // internal class TestAsyncEnumerable<T> { ... }
    // internal class TestAsyncEnumerator<T> { ... }
    // (Assuming these are already available from Sys_TableInfoService_SyncTableTests.cs or similar in the test project)
}
