using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VOL.Builder.Services;
using VOL.Core.Repositories;
using VOL.Entity.DomainModels;
using VOL.Core.DBManager; // Required for DBServerProvider if used by ProjectPath
using VOL.Core.Utilities; // Required for WebResponseContent if used indirectly
using VOL.Builder.IRepositories; // Required for ISys_TableInfoRepository

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class SysTableInfoService_GetTableTreeTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Sys_TableInfoService _service;
        private Mock<VOL.Core.BaseProvider.IServices> _baseServicesMock; // Mock for base dependencies if any

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _baseServicesMock = new Mock<VOL.Core.BaseProvider.IServices>(); // Example, adjust if Sys_TableInfoService has other deps

            // Assuming Sys_TableInfoService has a constructor that accepts its specific repository
            // If it relies on a generic repository or other services, adjust instantiation.
            // This instantiation is based on common patterns, actual constructor might differ.
            // For example, if it takes IRepository<Sys_TableInfo>, then mock that.
            _service = new Sys_TableInfoService
            {
                repository = _mockRepository.Object
                // Other dependencies might need to be mocked or setup here if the constructor requires them
                // or if they are set as public properties.
            };

            // Mocking static ProjectPath if its methods are called within GetTableTree and affect output.
            // This is complex. For now, we assume ProjectPath.GetProjectFileName and WebProject property work as expected
            // or their specific output for these tests is not critical beyond being non-empty.
        }

        private List<Sys_TableInfo> GetMockTableInfoList(params Sys_TableInfo[] items)
        {
            return new List<Sys_TableInfo>(items);
        }

        [TestMethod]
        public async Task GetTableTree_WhenRepositoryIsEmpty_ShouldReturnEmptyJsonArray()
        {
            // Arrange
            var emptyList = GetMockTableInfoList();
            _mockRepository.Setup(repo => repo.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(emptyList.AsQueryable());

            // Act
            var (jsonResult, projectName) = await _service.GetTableTree();

            // Assert
            Assert.AreEqual("[]", jsonResult, "JSON result should be an empty array when repository is empty.");
        }

        [TestMethod]
        public async Task GetTableTree_WhenSingleNode_ShouldSetIsParentToFalse()
        {
            // Arrange
            var singleNodeList = GetMockTableInfoList(new Sys_TableInfo { Table_Id = 1, ColumnCNName = "Node1", ParentId = null });
            _mockRepository.Setup(repo => repo.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(singleNodeList.AsQueryable());

            // Act
            var (jsonResult, _) = await _service.GetTableTree();
            var resultList = JsonConvert.DeserializeObject<List<dynamic>>(jsonResult);

            // Assert
            Assert.IsNotNull(resultList);
            Assert.AreEqual(1, resultList.Count);
            Assert.IsFalse((bool)resultList[0].isParent, "Single node should not be a parent.");
        }

        [TestMethod]
        public async Task GetTableTree_WhenNodeHasChildren_ShouldSetIsParentToTrue()
        {
            // Arrange
            var parentAndChildList = GetMockTableInfoList(
                new Sys_TableInfo { Table_Id = 1, ColumnCNName = "Parent", ParentId = null },
                new Sys_TableInfo { Table_Id = 2, ColumnCNName = "Child", ParentId = 1 }
            );
            _mockRepository.Setup(repo => repo.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(parentAndChildList.AsQueryable());

            // Act
            var (jsonResult, _) = await _service.GetTableTree();
            var resultList = JsonConvert.DeserializeObject<List<dynamic>>(jsonResult);

            // Assert
            Assert.IsNotNull(resultList);
            var parentNode = resultList.FirstOrDefault(n => n.id == 1);
            var childNode = resultList.FirstOrDefault(n => n.id == 2);

            Assert.IsNotNull(parentNode, "Parent node should exist.");
            Assert.IsTrue((bool)parentNode.isParent, "Parent node with a child should have isParent = true.");
            Assert.IsNotNull(childNode, "Child node should exist.");
            Assert.IsFalse((bool)childNode.isParent, "Child node without children should have isParent = false.");
        }

        [TestMethod]
        public async Task GetTableTree_ComplexHierarchy_ShouldSetAllParentFlagsCorrectly()
        {
            // Arrange
            var complexList = GetMockTableInfoList(
                new Sys_TableInfo { Table_Id = 1, ColumnCNName = "Root", ParentId = null },
                new Sys_TableInfo { Table_Id = 2, ColumnCNName = "Child1_L1", ParentId = 1 },
                new Sys_TableInfo { Table_Id = 3, ColumnCNName = "Child2_L1", ParentId = 1 },
                new Sys_TableInfo { Table_Id = 4, ColumnCNName = "GrandChild1_L2", ParentId = 2 },
                new Sys_TableInfo { Table_Id = 5, ColumnCNName = "Child3_L1_NoChildren", ParentId = null }
            );
             _mockRepository.Setup(repo => repo.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(complexList.AsQueryable());

            // Act
            var (jsonResult, _) = await _service.GetTableTree();
            var resultList = JsonConvert.DeserializeObject<List<dynamic>>(jsonResult);

            // Assert
            Assert.IsNotNull(resultList);
            var node1 = resultList.FirstOrDefault(n => n.id == 1); // Root
            var node2 = resultList.FirstOrDefault(n => n.id == 2); // Child1_L1
            var node3 = resultList.FirstOrDefault(n => n.id == 3); // Child2_L1
            var node4 = resultList.FirstOrDefault(n => n.id == 4); // GrandChild1_L2
            var node5 = resultList.FirstOrDefault(n => n.id == 5); // Child3_L1_NoChildren

            Assert.IsNotNull(node1); Assert.IsTrue((bool)node1.isParent, "Node 1 should be parent.");
            Assert.IsNotNull(node2); Assert.IsTrue((bool)node2.isParent, "Node 2 should be parent.");
            Assert.IsNotNull(node3); Assert.IsFalse((bool)node3.isParent, "Node 3 should not be parent.");
            Assert.IsNotNull(node4); Assert.IsFalse((bool)node4.isParent, "Node 4 should not be parent.");
            Assert.IsNotNull(node5); Assert.IsFalse((bool)node5.isParent, "Node 5 should not be parent.");
        }
    }
}
