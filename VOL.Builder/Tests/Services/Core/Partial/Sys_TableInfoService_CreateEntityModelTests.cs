using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VOL.Builder.Services;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using VOL.Builder.IRepositories;
using VOL.Core.Dapper;
using VOL.Entity.SystemModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
// Required for InternalsVisibleTo if we were modifying project files
// using System.Runtime.CompilerServices;
// [assembly: InternalsVisibleTo("VOL.Builder.Tests")] // Would be in AssemblyInfo.cs or .csproj

namespace VOL.Builder.Tests.Services
{
    [TestClass]
    public class SysTableInfoService_CreateEntityModelTests
    {
        private Mock<ISys_TableInfoRepository> _mockRepository;
        private Mock<IDapperContext> _mockDapperContext;
        private Sys_TableInfoService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockRepository = new Mock<ISys_TableInfoRepository>();
            _mockDapperContext = new Mock<IDapperContext>();

            _mockRepository.Setup(repo => repo.DapperContext).Returns(_mockDapperContext.Object);

            _service = new Sys_TableInfoService { repository = _mockRepository.Object };
        }

        private Sys_TableInfo CreateBasicTableInfo(string tableName = "NewTestTable", string cnName = "新测试表")
        {
            return new Sys_TableInfo
            {
                TableName = tableName,
                TableTrueName = tableName,
                ColumnCNName = cnName,
                Namespace = "MyProject.Entity",
                FolderName = "Entities",
                TableColumns = new List<Sys_TableColumn>()
            };
        }

        private Sys_TableColumn CreateColumn(string name, string cnName, string colType, int isKey = 0, int isNull = 0, int maxLength = 0, int isColumnData = 1, int? enable = null, int? editRowNo = null)
        {
            return new Sys_TableColumn
            {
                ColumnName = name,
                ColumnCnName = cnName,
                ColumnType = colType,
                IsKey = isKey,
                IsNull = isNull,
                Maxlength = maxLength,
                IsColumnData = isColumnData,
                Enable = enable,
                EditRowNo = editRowNo
            };
        }

        private List<TableColumnInfo> CreateTableColumnInfoList(params TableColumnInfo[] items)
        {
            return new List<TableColumnInfo>(items);
        }

        // Helper to invoke the internal CreateEntityModel method
        private (string generatedCode, string errorMessage) InvokeInternalCreateEntityModel(List<Sys_TableColumn> sysColumns, Sys_TableInfo tableInfo, List<TableColumnInfo> tableColumnInfoList, int createType)
        {
            MethodInfo privateMethod = typeof(Sys_TableInfoService).GetMethod(
                "CreateEntityModel", BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(List<Sys_TableColumn>), typeof(Sys_TableInfo), typeof(List<TableColumnInfo>), typeof(int) }, null);

            if (privateMethod == null) throw new InvalidOperationException("Private/Internal CreateEntityModel method not found. Ensure it's internal and [InternalsVisibleTo] is set for the test project, or adjust reflection flags if it's private.");

            // The refactored method is expected to return (string, string)
            var result = privateMethod.Invoke(_service, new object[] { sysColumns, tableInfo, tableColumnInfoList, createType });
            return ((string, string))result;
        }


        [TestMethod]
        public void CreateEntityModel_Public_ValidInput_ReturnsSuccessMessage()
        {
            var tableInfo = CreateBasicTableInfo();
            tableInfo.TableColumns.Add(CreateColumn("Id", "主键", "int", 1));
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "Id", ColumnType = "int" });
            _mockDapperContext.Setup(db => db.QueryList<TableColumnInfo>(It.IsAny<string>(), It.IsAny<object>())).Returns(dbSchema);

            // Mock FileHelper.WriteFile behavior - for this test, we assume it doesn't throw.
            // In a real scenario with IFileService, you'd mock WriteFile.
            // Here, we rely on the refactored public method to call the internal one.
            // The internal one's output is tested elsewhere.

            string result = _service.CreateEntityModel(tableInfo);
            Assert.AreEqual("Model创建成功!", result);
        }

        [TestMethod]
        public void CreateEntityModel_Public_MissingTableColumns_ReturnsErrorMessage()
        {
            var tableInfo = CreateBasicTableInfo();
            tableInfo.TableColumns = null;
            string result = _service.CreateEntityModel(tableInfo);
            Assert.AreEqual("提交的配置数据不完整", result);
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithKeyColumn_GeneratesKeyAttributeAndCorrectType()
        {
            var tableInfo = CreateBasicTableInfo("KeyTable");
            var columns = new List<Sys_TableColumn> { CreateColumn("MyKey", "主键列", "int", 1) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "MyKey", ColumnType = "int" });

            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);

            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("[Key]"), "Should contain [Key] attribute.");
            Assert.IsTrue(code.Contains("public int MyKey { get; set; }"), "Should generate correct property for Key.");
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithNullableValueTypeColumn_GeneratesNullableProperty()
        {
            var tableInfo = CreateBasicTableInfo("NullableTable");
            var columns = new List<Sys_TableColumn> { CreateColumn("OptionalAge", "可选年龄", "int", 0, 1) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "OptionalAge", ColumnType = "int" });

            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("public int? OptionalAge { get; set; }"), "Should generate nullable int property.");
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithStringColumnWithMaxLength_GeneratesMaxLengthAttribute()
        {
            var tableInfo = CreateBasicTableInfo("MaxLengthTable");
            var columns = new List<Sys_TableColumn> { CreateColumn("ProductName", "产品名称", "string", 0, 0, 150) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "ProductName", ColumnType = "nvarchar" });

            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("[MaxLength(150)]"), "Should generate [MaxLength(150)] attribute.");
            Assert.IsTrue(code.Contains("public string ProductName { get; set; }"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithRequiredColumn_GeneratesRequiredAttribute()
        {
            var tableInfo = CreateBasicTableInfo("RequiredTable");
            var columns = new List<Sys_TableColumn> { CreateColumn("RequiredField", "必填字段", "string", 0, 0, 50) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "RequiredField", ColumnType = "nvarchar" });

            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("[Required(AllowEmptyStrings=false)]"), "Should generate [Required] attribute.");
        }

        [TestMethod]
        public void InternalCreateEntityModel_ColumnTypeMapping_Int()
        {
            var tableInfo = CreateBasicTableInfo("TypeMapInt");
            var columns = new List<Sys_TableColumn> { CreateColumn("Age", "年龄", "int") };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "Age", ColumnType = "int" });
            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("public int Age { get; set; }"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_ColumnTypeMapping_String()
        {
            var tableInfo = CreateBasicTableInfo("TypeMapString");
            var columns = new List<Sys_TableColumn> { CreateColumn("Notes", "备注", "string", 0, 1, 0) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "Notes", ColumnType = "nvarchar" }); // nvarchar(max) due to MaxLength=0
            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("public string? Notes { get; set; }"));
            Assert.IsTrue(code.Contains("[Column(TypeName=\"nvarchar(max)\")]"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_ColumnTypeMapping_DateTime()
        {
            var tableInfo = CreateBasicTableInfo("TypeMapDateTime");
            var columns = new List<Sys_TableColumn> { CreateColumn("EventDate", "事件日期", "DateTime", 0, 1) };
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "EventDate", ColumnType = "datetime" });
            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("public DateTime? EventDate { get; set; }"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_ColumnTypeMapping_GuidFromUniqueidentifier()
        {
            var tableInfo = CreateBasicTableInfo("TypeMapGuid");
            var columns = new List<Sys_TableColumn> { CreateColumn("Identifier", "标识符", "uniqueidentifier", 0, 0, 36) }; // ColumnType on Sys_TableColumn could be "string" or "uniqueidentifier"
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "Identifier", ColumnType = "uniqueidentifier" });
            var (code, err) = InvokeInternalCreateEntityModel(columns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");
            Assert.IsTrue(code.Contains("public Guid Identifier { get; set; }"));
            Assert.IsTrue(code.Contains("[Column(TypeName=\"uniqueidentifier\")]"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_ColumnTypeMapping_GuidFromStringLength36ForMySql()
        {
            var originalDbType = VOL.Core.DBManager.DBType.Name;
            VOL.Core.DBManager.DBType.Name = DbCurrentType.MySql.ToString();

            var tableInfo = CreateBasicTableInfo("GuidMySql");
            tableInfo.TableColumns.Add(CreateColumn("MyGuid", "MySQL GUID", "string", 0, 0, 36));
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "MyGuid", ColumnType = "varchar" });

            var (code, err) = InvokeInternalCreateEntityModel(tableInfo.TableColumns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");

            Assert.IsTrue(code.Contains("public Guid MyGuid { get; set; }"), "Should be Guid type in C# for MySQL string(36)");
            Assert.IsTrue(code.Contains("[Column(TypeName=\"uniqueidentifier\")]"), "Should map to uniqueidentifier in DB attributes for consistency.");

            VOL.Core.DBManager.DBType.Name = originalDbType;
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithDetailTable_GeneratesListOfDetailTypeAndForeignKeyAttribute()
        {
            var tableInfo = CreateBasicTableInfo("MasterTable");
            tableInfo.TableColumns.Add(CreateColumn("MasterId", "主表ID", "int", 1));
            tableInfo.DetailName = "DetailTable"; // Single detail table name
            tableInfo.DetailCnName = "明细表";

            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "MasterId", ColumnType = "int" });

            var (code, err) = InvokeInternalCreateEntityModel(tableInfo.TableColumns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");

            Assert.IsTrue(code.Contains("[ForeignKey(\"MasterId\")]"));
            Assert.IsTrue(code.Contains("public List<DetailTable> DetailTable { get; set; }"));
        }

        [TestMethod]
        public void InternalCreateEntityModel_WithMultipleDetailTables_GeneratesListOfDetailTypesAndForeignKeyAttributes()
        {
            var tableInfo = CreateBasicTableInfo("MasterTableMulti");
            tableInfo.TableColumns.Add(CreateColumn("MasterId", "主表ID", "int", 1));
            tableInfo.DetailName = "DetailTableOne,DetailTableTwo"; // Multiple detail tables
            tableInfo.DetailCnName = "明细表一,明细表二";

            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "MasterId", ColumnType = "int" });

            var (code, err) = InvokeInternalCreateEntityModel(tableInfo.TableColumns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");

            Assert.IsTrue(code.Contains("[ForeignKey(\"MasterId\")]"));
            Assert.IsTrue(code.Contains("public List<DetailTableOne> DetailTableOne { get; set; }"));
            Assert.IsTrue(code.Contains("public List<DetailTableTwo> DetailTableTwo { get; set; }"));
        }


        [TestMethod]
        public void InternalCreateEntityModel_NonColumnData_GeneratesJsonIgnoreAttribute()
        {
            var tableInfo = CreateBasicTableInfo("JsonIgnoreTest");
            tableInfo.TableColumns.Add(CreateColumn("CalculatedField", "计算字段", "string", 0, 1, 0, isColumnData: 0));
            var dbSchema = CreateTableColumnInfoList(new TableColumnInfo { ColumnName = "CalculatedField", ColumnType = "nvarchar" });

            var (code, err) = InvokeInternalCreateEntityModel(tableInfo.TableColumns, tableInfo, dbSchema, 1);
            Assert.IsTrue(string.IsNullOrEmpty(err), $"Error generating code: {err}");

            Assert.IsTrue(code.Contains("[JsonIgnore]"));
            Assert.IsTrue(code.Contains("public string? CalculatedField { get; set; }"));
        }
    }
}
