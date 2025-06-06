using Microsoft.AspNetCore.Authorization;

namespace VOL.Core.Authorization.FieldLevel
{
    /// <summary>
    /// 代表对特定实体字段进行操作（如读取或写入）的授权需求。
    /// (Represents an authorization requirement for operating on a specific entity field, e.g., read or write.)
    /// </summary>
    public class FieldPermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// 获取字段名称。
        /// (Gets the field name.)
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// 获取所需的操作类型（例如 "Read", "Write"）。
        /// (Gets the required operation type (e.g., "Read", "Write").)
        /// </summary>
        public string OperationType { get; }

        /// <summary>
        /// 获取实体类型名称。
        /// (Gets the entity type name.)
        /// </summary>
        public string EntityTypeName { get; }

        /// <summary>
        /// 初始化一个新的 <see cref="FieldPermissionRequirement"/> 实例。
        /// (Initializes a new instance of the <see cref="FieldPermissionRequirement"/> class.)
        /// </summary>
        /// <param name="entityTypeName">实体类型的名称。 (The name of the entity type.)</param>
        /// <param name="fieldName">字段的名称。 (The name of the field.)</param>
        /// <param name="operationType">所需操作的类型 (例如 "Read", "Write")。 (The type of operation required (e.g., "Read", "Write").)</param>
        public FieldPermissionRequirement(string entityTypeName, string fieldName, string operationType)
        {
            EntityTypeName = entityTypeName;
            FieldName = fieldName;
            OperationType = operationType;
        }

        // Chinese Comment: ToString() 方法可以用于调试和日志记录。
        // (The ToString() method can be used for debugging and logging.)
        public override string ToString()
        {
            return $"Requirement: Entity='{EntityTypeName}', Field='{FieldName}', Operation='{OperationType}'";
        }
    }
}
