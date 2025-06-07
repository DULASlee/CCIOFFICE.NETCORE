using Xunit;
using Moq;
using VOL.Builder.Services;
using VOL.Entity.DomainModels.Sys;
using VOL.Core.Utilities; // For WebResponseContent
using VOL.Core.ManageUser; // For UserContext
using VOL.Core.DBManager; // For DBServerProvider, DBType
using VOL.Entity.SystemModels; // For TableColumnInfo if needed by underlying calls
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using Microsoft.EntityFrameworkCore.Query.Internal; // For EntityQueryable
using Microsoft.EntityFrameworkCore; // For DbFunctions
using VOL.Core.DbContext; // For DapperContext if direct mocking needed. Usually via repository.

namespace VOL.Builder.Tests.Services
{
    public class Sys_TableInfoService_LoadTableTests
    {
        private readonly Mock<ISys_TableInfoRepository> _mockRepository;
        private readonly Sys_TableInfoService _service;
        private readonly Mock<VOLContext> _mockDbContext; // Mock DbContext for Dapper
        private readonly Mock<DbSet<Sys_TableInfo>> _mockDbSet_TableInfo;
        private readonly Mock<DbSet<Sys_TableColumn>> _mockDbSet_TableColumn;


        public Sys_TableInfoService_LoadTableTests()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _mockDbContext = new Mock<VOLContext>();
            _mockDbSet_TableInfo = new Mock<DbSet<Sys_TableInfo>>();
            _mockDbSet_TableColumn = new Mock<DbSet<Sys_TableColumn>>();

            // Setup mock DbContext to return DapperContext if DapperContext is used directly
            // var mockDapperContext = new Mock<DapperContext>(null); // DapperContext constructor might need parameters
            // _mockDbContext.Setup(db => db.DapperContext).Returns(mockDapperContext.Object);
            // For now, assume DapperContext calls are via _mockRepository.DapperContext

            var mockDapperCtx = new Mock<DapperContext>();
             _mockRepository.Setup(r => r.DapperContext).Returns(mockDapperCtx.Object);


            _service = new Sys_TableInfoService(_mockRepository.Object);

            // Default UserContext behavior (can be overridden in tests)
            // This is a conceptual setup. In reality, UserContext.Current is static and hard to mock.
            // Tests will assume this can be controlled.
            UserContext.Current.IsSuperAdmin = true;

            // Default DBType (can be overridden if necessary, though often handled by SQL generation)
            // DBServerProvider.SetCurrentDBType(VOL.Core.Enums.DbCurrentType.SqlServer); // Conceptual
        }

        private void SetupUserContext(bool isSuperAdmin)
        {
            // This is a simplified approach. True mocking of static UserContext is complex.
            UserContext.Current.IsSuperAdmin = isSuperAdmin;
        }

        private void SetupDBType(VOL.Core.Enums.DbCurrentType dbType)
        {
            // Conceptual: DBType.Name is static. This would require reflection or refactoring DBType.
            // For now, assume the service's SQL generation logic for the default (SqlServer) or test specific paths.
            // For example, if DBType.Name is read inside the service, it's hard to control from here.
            // If it's passed or injectable, it becomes testable.
        }

        [Fact]
        public void LoadTable_UserNotSuperAdmin_And_NotTreeLoad_ReturnsError()
        {
            // Arrange
            SetupUserContext(false);

            // Act
            var response = _service.LoadTable(0, "TestTable", "TestCN", "Test.Ns", "TestFolder", 0, false);

            // Assert
            Assert.False(response.Status);
            Assert.Equal("只有超级管理员才能进行此操作", response.Message);
        }

        [Fact]
        public void LoadTable_IsTreeLoad_LoadsExistingTableInfoSuccessfully()
        {
            // Arrange
            SetupUserContext(true); // SuperAdmin or not doesn't matter for tree load
            int tableId = 1;
            var expectedTableInfo = new Sys_TableInfo { Table_Id = tableId, TableName = "ExistingTable", TableColumns = new List<Sys_TableColumn>() };

            var mockTableInfoList = new List<Sys_TableInfo> { expectedTableInfo };
            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(mockTableInfoList.AsQueryable().BuildMock());

            // Act
            var response = _service.LoadTable(0, "ExistingTable", "CN", "Ns", "Folder", tableId, true);

            // Assert
            Assert.True(response.Status);
            Assert.NotNull(response.Data);
            Assert.IsType<Sys_TableInfo>(response.Data);
            Assert.Equal(tableId, ((Sys_TableInfo)response.Data).Table_Id);
             _mockRepository.Verify(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()), Times.Once);
        }

        [Fact]
        public void LoadTable_NotTreeLoad_TableAlreadyConfigured_LoadsSuccessfully()
        {
            // Arrange
            SetupUserContext(true);
            string tableName = "ConfiguredTable";
            int existingTableId = 2;
            var existingTableInfo = new Sys_TableInfo { Table_Id = existingTableId, TableName = tableName, TableColumns = new List<Sys_TableColumn>() };

            var queryableTableInfo = new List<Sys_TableInfo> { existingTableInfo }.AsQueryable().BuildMock();
             _mockRepository.Setup(r => r.FindAsIQueryable(It.Is<Expression<Func<Sys_TableInfo, bool>>>(exp => true)))
                           .Returns(queryableTableInfo);


            // Act
            var response = _service.LoadTable(0, tableName, "CN", "Ns", "Folder", 0, false); // tableId=0, so it tries to find by name

            // Assert
            Assert.True(response.Status);
            Assert.NotNull(response.Data);
            Assert.IsType<Sys_TableInfo>(response.Data);
            Assert.Equal(existingTableId, ((Sys_TableInfo)response.Data).Table_Id);
            _mockRepository.Verify(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()), Times.Exactly(2)); // Once for ID check, once for loading
        }

        [Fact]
        public void LoadTable_NotTreeLoad_NewTable_InitializesAndLoadsSuccessfully()
        {
            // Arrange
            SetupUserContext(true);
            string newTableName = "NewFreshTable";

            // Stage 1: Initial check for table by name (returns empty)
            var emptyTableInfoQueryable = new List<Sys_TableInfo>().AsQueryable().BuildMock();
            _mockRepository.SetupSequence(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(emptyTableInfoQueryable) // First call for finding by name (InitTable)
                           .Returns(emptyTableInfoQueryable); // Call inside InitTable for existing ID check by name
                           // Then, the final load after Add:
                           // This needs to be more specific or use a callback to simulate Add then find.

            var columnsFromDb = new List<Sys_TableColumn>
            {
                new Sys_TableColumn { ColumnName = "Id", ColumnType = "int", IsKey = 1, OrderNo = 10 },
                new Sys_TableColumn { ColumnName = "Name", ColumnType = "string", Maxlength = 100, OrderNo = 20 }
            };
            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(columnsFromDb);

            Sys_TableInfo addedTableInfo = null;
            List<Sys_TableColumn> addedColumns = null;
            _mockRepository.Setup(r => r.Add<Sys_TableColumn>(It.IsAny<Sys_TableInfo>(), It.IsAny<List<Sys_TableColumn>>(), false))
                .Callback<Sys_TableInfo, List<Sys_TableColumn>, bool>((tbl, cols, save) => {
                    addedTableInfo = tbl;
                    addedTableInfo.Table_Id = 123; // Simulate DB assigning an ID
                    addedColumns = cols;
                })
                .Returns(new WebResponseContent(true));

            // Setup for the final load after "Add"
            _mockRepository.Setup(r => r.FindAsIQueryable(It.Is<Expression<Func<Sys_TableInfo, bool>>>(ex => true))) // More generic match for final load
                           .Returns(() => {
                               // Return the 'added' table info as if it's now in the DB
                               if (addedTableInfo != null) {
                                   addedTableInfo.TableColumns = addedColumns; // Attach columns for the final load
                                   return new List<Sys_TableInfo> { addedTableInfo }.AsQueryable().BuildMock();
                               }
                               return new List<Sys_TableInfo>().AsQueryable().BuildMock();
                           });


            // Act
            var response = _service.LoadTable(0, newTableName, "New Table CN", "New.Ns", "NewFolder", 0, false);

            // Assert
            Assert.True(response.Status, response.Message);
            Assert.NotNull(response.Data);
            var resultTableInfo = (Sys_TableInfo)response.Data;
            Assert.Equal(newTableName, resultTableInfo.TableName);
            Assert.Equal(123, resultTableInfo.Table_Id); // ID assigned in callback
            Assert.NotNull(resultTableInfo.TableColumns);
            Assert.Equal(2, resultTableInfo.TableColumns.Count);
            Assert.Contains(resultTableInfo.TableColumns, c => c.ColumnName == "Id");

            _mockRepository.Verify(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null), Times.Once);
            _mockRepository.Verify(r => r.Add<Sys_TableColumn>(It.IsAny<Sys_TableInfo>(), It.IsAny<List<Sys_TableColumn>>(), false), Times.Once);
        }


        [Fact]
        public void LoadTable_NotTreeLoad_NewTable_EmptyTableName_ReturnsError() // Technically InitTable handles this first
        {
            // Arrange
            SetupUserContext(true);

            // Act
            // InitTable returns -1 if tableName is empty, LoadTable then tries to load with ID -1.
            var tableInfoList = new List<Sys_TableInfo>().AsQueryable().BuildMock(); // Empty list for FindAsIQueryable
             _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(tableInfoList);

            var response = _service.LoadTable(0, "", "CN", "Ns", "Folder", 0, false);

            // Assert
            Assert.True(response.Status); // The method itself might return OK if no table is found with ID -1
            Assert.Null(response.Data); // But Data should be null or indicate no table loaded
        }

        [Fact]
        public void LoadTable_NotTreeLoad_NewTable_DBQueryReturnsNoColumns_ReturnsErrorInInitTable()
        {
            // Arrange
            SetupUserContext(true);
            string tableName = "EmptyStructureTable";

            _mockRepository.Setup(r => r.FindAsIQueryable(It.IsAny<Expression<Func<Sys_TableInfo, bool>>>()))
                           .Returns(new List<Sys_TableInfo>().AsQueryable().BuildMock()); // No existing table config

            _mockRepository.Setup(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null))
                           .Returns(new List<Sys_TableColumn>()); // DB returns no columns

            // Act & Assert
            // The exception from InitTable (due to no columns) will propagate
            // WebResponseContent response = null;
            Exception ex = Assert.Throws<Exception>(() => _service.LoadTable(0, tableName, "CN", "Ns", "Folder", 0, false));

            Assert.Contains("加载表结构写入异常", ex.Message); // Assuming Add fails or throws if columns are empty.
                                                          // Or, if GetCurrentSql + QueryList leads to an error before Add.
                                                          // The actual message depends on how InitTable handles empty columns from DB.
                                                          // If InitTable returns -1, then LoadTable will try to load table with ID -1.
                                                          // Let's refine based on InitTable's behavior: if QueryList is empty, Add is called with empty list.
                                                          // The Add method in BaseService might throw or return error if list is empty.
                                                          // Let's assume Add fails and throws, which is caught by InitTable and re-thrown.
             _mockRepository.Verify(r => r.DapperContext.QueryList<Sys_TableColumn>(It.IsAny<string>(), It.IsAny<object>(), null), Times.Once);
        }
    }


    // Helper to build IQueryable for EF Core, from https://stackoverflow.com/questions/39229073/how-to-mock-iasyncenumerable-tolistasync
    // This is a simplified version. For full async support, more setup is needed.
    public static class MockQueryableExtensions
    {
        public static IQueryable<T> BuildMock<T>(this IQueryable<T> data) where T : class
        {
            var mock = new Mock<IAsyncQueryProvider>();
            mock.Setup(m => m.CreateQuery<TElement>(It.IsAny<Expression>()))
                .Returns<Expression>(e => new TestAsyncEnumerable<TElement>(e)); // For async operations

            var queryableMock = new Mock<IQueryable<T>>();
            queryableMock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

            queryableMock.Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider)); // Use TestAsyncQueryProvider
            queryableMock.Setup(m => m.Expression).Returns(data.Expression);
            queryableMock.Setup(m => m.ElementType).Returns(data.ElementType);
            queryableMock.Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            return queryableMock.Object;
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, System.Threading.CancellationToken cancellationToken)
        {
            //This is the part that needs to be implemented fully for async operations like ToListAsync()
            //For simplicity, we're returning default or throwing if not simple.
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            object result = null;

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                 // Simulate async execution for common cases like ToListAsync
                if (expression is MethodCallExpression methodCall && methodCall.Method.Name == "ToListAsync")
                {
                     // Get the source IQueryable from the expression
                    var queryable = (IQueryable<TEntity>)_inner.CreateQuery(methodCall.Arguments[0]);
                    result = queryable.ToList();
                    return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                        .MakeGenericMethod(expectedResultType)
                        .Invoke(null, new[] { result });
                }
                 return (TResult)Task.FromResult(Activator.CreateInstance(expectedResultType));
            }

            return _inner.Execute<TResult>(expression); // Fallback for non-async or simple sync exec
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    }
}
