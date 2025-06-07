using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VOL.Builder.Services;
using VOL.Entity.DomainModels.Sys;
using VOL.Core.Utilities;
using VOL.Core.DBManager; // For DBType
using VOL.Entity.SystemModels;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal; // For EntityQueryable for older EF Core versions if needed
using VOL.Core.DbContext; // For DapperContext & VOLContext

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class Sys_TableInfoService_SyncTableTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Sys_TableInfoService _service;
        private Mock<VOLContext> _mockDbContext;
        private Mock<DbSet<Sys_TableColumn>> _mockDbSetTableColumn;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _mockDbContext = new Mock<VOLContext>();
            _mockDbSetTableColumn = new Mock<DbSet<Sys_TableColumn>>();

            _mockDbContext.Setup(c => c.Set<Sys_TableColumn>()).Returns(_mockDbSetTableColumn.Object);
            _mockRepository.Setup(r => r.DbContext).Returns(_mockDbContext.Object);

            var mockDapperCtx = new Mock<DapperContext>(); // Assuming DapperContext can be newed up or mocked simply
            _mockRepository.Setup(r => r.DapperContext).Returns(mockDapperCtx.Object);


            _service = new Sys_TableInfoService(_mockRepository.Object);

            // Default DBType to SqlServer for tests if not specified
            // DBServerProvider.SetCurrentDBType(VOL.Core.Enums.DbCurrentType.SqlServer); // Conceptual
        }

        [TestMethod]
        public async Task SyncTable_TableNameIsEmpty_ReturnsOkWithMessage()
        {
            // Arrange
            string tableName = "";

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsTrue(response.Status);
            Assert.AreEqual("表名不能为空", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_TableInfoNotFound_ReturnsError()
        {
            // Arrange
            string tableName = "NonExistentTable";
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo>().AsQueryable().BuildMock());

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual($"未获取到【{tableName}】的配置信息，请使用新建功能", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_DBSchemaQueryReturnsNoColumns_ReturnsError()
        {
            // Arrange
            string tableName = "TableWithNoSchema";
            var tableInfo = new Sys_TableInfo { TableName = tableName, TableColumns = new List<Sys_TableColumn>() };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(new List<Sys_TableColumn>());

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsFalse(response.Status);
            Assert.AreEqual($"未获取到【{tableName}】表结构信息，请确认表是否存在", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_NoChangesDetected_ReturnsAppropriateMessage()
        {
            // Arrange
            string tableName = "StableTable";
            var existingColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int", Maxlength = 0, IsNull = 0 },
                new Sys_TableColumn { ColumnName = "Name", ColumnType = "string", Maxlength = 100, IsNull = 1 }
            };
            var tableInfo = new Sys_TableInfo { TableName = tableName, TableColumns = new List<Sys_TableColumn>(existingColumns) }; // Clone list

            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(new List<Sys_TableColumn>(existingColumns)); // DB returns identical columns

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsFalse(response.Status); // No changes means "error" or specific status
            Assert.AreEqual($"【{tableName}】表结构未发生变化", response.Message);
            _mockRepository.Verify(r => r.DbContext.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task SyncTable_NewColumnsInDB_IdentifiesAndAddsColumns()
        {
            // Arrange
            string tableName = "TableWithNewFields";
            var repoColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" }
            };
            var tableInfo = new Sys_TableInfo { TableName = tableName, Table_Id = 1, TableColumns = repoColumns };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());

            var dbColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" },
                new Sys_TableColumn { ColumnName = "NewField", ColumnType = "string", Maxlength = 50, IsNull = 1 }
            };
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(dbColumns);
            _mockRepository.Setup(r => r.AddRange(It.IsAny<List<Sys_TableColumn>>()));
             _mockRepository.Setup(r => r.DbContext.SaveChangesAsync(default)).ReturnsAsync(1);


            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsTrue(response.Status);
            _mockRepository.Verify(r => r.AddRange(It.Is<List<Sys_TableColumn>>(cols => cols.Count == 1 && cols[0].ColumnName == "NewField")), Times.Once);
            _mockRepository.Verify(r => r.DbContext.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
             Assert.AreEqual("新加字段【1】个,删除字段【0】,修改字段【0】", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_ColumnsRemovedFromDB_IdentifiesAndRemovesColumns()
        {
            // Arrange
            string tableName = "TableWithRemovedFields";
            var repoColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" },
                new Sys_TableColumn { ColumnName = "OldField", ColumnType = "string" }
            };
            var tableInfo = new Sys_TableInfo { TableName = tableName, TableColumns = repoColumns };
             _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());

            var dbColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" }
            };
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(dbColumns);
            _mockDbSetTableColumn.Setup(s => s.RemoveRange(It.IsAny<IEnumerable<Sys_TableColumn>>()));
            _mockRepository.Setup(r => r.DbContext.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsTrue(response.Status);
            _mockDbSetTableColumn.Verify(s => s.RemoveRange(It.Is<IEnumerable<Sys_TableColumn>>(cols => cols.Count() == 1 && cols.First().ColumnName == "OldField")), Times.Once);
            _mockRepository.Verify(r => r.DbContext.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            Assert.AreEqual("新加字段【0】个,删除字段【1】,修改字段【0】", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_ColumnsModifiedInDB_IdentifiesAndUpdateColumns()
        {
            // Arrange
            string tableName = "TableWithModifiedFields";
            var repoColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int", Maxlength = 0, IsNull = 0 },
                new Sys_TableColumn { ColumnName = "Name", ColumnType = "string", Maxlength = 100, IsNull = 1 }
            };
            var tableInfo = new Sys_TableInfo { TableName = tableName, TableColumns = repoColumns };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());

            var dbColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int", Maxlength = 0, IsNull = 0 }, // Unchanged
                new Sys_TableColumn { ColumnName = "Name", ColumnType = "varchar", Maxlength = 200, IsNull = 0 } // Changed
            };
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(dbColumns);
            _mockRepository.Setup(r => r.UpdateRange(It.IsAny<List<Sys_TableColumn>>(), It.IsAny<Expression<Func<Sys_TableColumn, object>>[]>()));
            _mockRepository.Setup(r => r.DbContext.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsTrue(response.Status);
            _mockRepository.Verify(r => r.UpdateRange(
                It.Is<List<Sys_TableColumn>>(cols => cols.Count == 1 && cols[0].ColumnName == "Name" && cols[0].ColumnType == "varchar" && cols[0].Maxlength == 200 && cols[0].IsNull == 0),
                It.IsAny<Expression<Func<Sys_TableColumn, object>>[]>()), Times.Once);
            _mockRepository.Verify(r => r.DbContext.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            Assert.AreEqual("新加字段【0】个,删除字段【0】,修改字段【1】", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_MixedChanges_AllOperationsCorrectlyApplied()
        {
            // Arrange
            string tableName = "ComplexChangeTable";
            var repoColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" }, // Unchanged
                new Sys_TableColumn { ColumnName = "FieldToRemove", ColumnType = "string" }, // To be removed
                new Sys_TableColumn { ColumnName = "FieldToUpdate", ColumnType = "int", Maxlength = 10 } // To be updated
            };
            var tableInfo = new Sys_TableInfo { TableName = tableName, Table_Id = 1, TableColumns = repoColumns };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());

            var dbColumns = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" }, // Unchanged
                new Sys_TableColumn { ColumnName = "FieldToUpdate", ColumnType = "bigint", Maxlength = 20 }, // Updated
                new Sys_TableColumn { ColumnName = "NewFieldToAdd", ColumnType = "datetime" } // Added
            };
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(dbColumns);

            _mockRepository.Setup(r => r.AddRange(It.IsAny<List<Sys_TableColumn>>()));
            _mockDbSetTableColumn.Setup(s => s.RemoveRange(It.IsAny<IEnumerable<Sys_TableColumn>>()));
            _mockRepository.Setup(r => r.UpdateRange(It.IsAny<List<Sys_TableColumn>>(), It.IsAny<Expression<Func<Sys_TableColumn, object>>[]>()));
            _mockRepository.Setup(r => r.DbContext.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var response = await _service.SyncTable(tableName);

            // Assert
            Assert.IsTrue(response.Status);
            _mockRepository.Verify(r => r.AddRange(It.Is<List<Sys_TableColumn>>(cols => cols.Count == 1 && cols[0].ColumnName == "NewFieldToAdd")), Times.Once);
            _mockDbSetTableColumn.Verify(s => s.RemoveRange(It.Is<IEnumerable<Sys_TableColumn>>(cols => cols.Count() == 1 && cols.First().ColumnName == "FieldToRemove")), Times.Once);
            _mockRepository.Verify(r => r.UpdateRange(
                It.Is<List<Sys_TableColumn>>(cols => cols.Count == 1 && cols[0].ColumnName == "FieldToUpdate" && cols[0].ColumnType == "bigint"),
                It.IsAny<Expression<Func<Sys_TableColumn, object>>[]>()), Times.Once);
            _mockRepository.Verify(r => r.DbContext.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            Assert.AreEqual("新加字段【1】个,删除字段【1】,修改字段【1】", response.Message);
        }

        [TestMethod]
        public async Task SyncTable_TableTrueNameIsUsed_WhenAvailable()
        {
            // Arrange
            string tableName = "AliasTable";
            string tableTrueName = "RealPhysicalTable";
            var tableInfo = new Sys_TableInfo { TableName = tableName, TableTrueName = tableTrueName, TableColumns = new List<Sys_TableColumn>() };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo> { tableInfo }.AsQueryable().BuildMock());

            var dbColumns = new List<Sys_TableColumn> { new Sys_TableColumn { ColumnName = "Id", ColumnType = "int" }};
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.Is<object>(o => o.GetType().GetProperty("tableName").GetValue(o).ToString() == tableTrueName), null))
                           .Returns(dbColumns); // Ensure Dapper is queried with the true name

            // Act
            await _service.SyncTable(tableName);

            // Assert
            _mockRepository.Verify(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.Is<object>(o => o.GetType().GetProperty("tableName").GetValue(o).ToString() == tableTrueName), null), Times.Once);
        }
    }

    // Minimalistic IQueryProvider and IAsyncEnumerable for mocking, adjust as needed for full async behavior
    public static class MockQueryableExtensions
    {
        public static IQueryable<T> BuildMock<T>(this IQueryable<T> data) where T : class
        {
            var mock = new Mock<IAsyncQueryProvider>();
            mock.Setup(m => m.CreateQuery<TElement>(It.IsAny<Expression>()))
                .Returns<Expression>(e => new TestAsyncEnumerable<TElement>(e));

            var queryableMock = new Mock<IQueryable<T>>();
            queryableMock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

            queryableMock.Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
            queryableMock.Setup(m => m.Expression).Returns(data.Expression);
            queryableMock.Setup(m => m.ElementType).Returns(data.ElementType);
            queryableMock.Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            return queryableMock.Object;
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
        public object Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, System.Threading.CancellationToken cancellationToken) => Task.FromResult(Execute<TResult>(expression));
        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, System.Threading.CancellationToken cancellationToken) => Execute<TResult>(expression);
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public void Dispose() => _inner.Dispose(); // Ensure Dispose is implemented if needed by your specific .NET version or test setup
        public ValueTask DisposeAsync() { _inner.Dispose(); return new ValueTask(Task.CompletedTask); }
        public T Current => _inner.Current;
        public Task<bool> MoveNext(System.Threading.CancellationToken cancellationToken) => Task.FromResult(_inner.MoveNext());
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    }
}
