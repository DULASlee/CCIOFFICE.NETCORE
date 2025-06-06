using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using VOL.Core.ManageUser; // For UserContext (or an equivalent way to get user claims/roles)
using VOL.Core.Services; // For Logger
using VOL.Core.Enums;   // For LogLevel, LogEvent

namespace VOL.Core.Authorization.FieldLevel
{
    /// <summary>
    /// 处理 <see cref="FieldPermissionRequirement"/> 授权需求的处理器。
    /// (Handler for <see cref="FieldPermissionRequirement"/> authorization requirements.)
    /// </summary>
    public class FieldPermissionHandler : AuthorizationHandler<FieldPermissionRequirement, object> // 'object' can be the entity instance
    {
        /// <summary>
        /// 处理授权需求。
        /// (Handles the authorization requirement.)
        /// </summary>
        /// <param name="context">授权处理器上下文。 (The authorization handler context.)</param>
        /// <param name="requirement">字段权限需求。 (The field permission requirement.)</param>
        /// <param name="resource">正在访问的资源（例如实体实例）。 (The resource being accessed (e.g., the entity instance).)</param>
        /// <returns>一个表示异步操作的任务。 (A task that represents the asynchronous operation.)</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FieldPermissionRequirement requirement, object resource)
        {
            // Chinese Comment: 实际的权限检查逻辑会更复杂，可能涉及查询数据库或缓存中的用户权限。
            // 此处为示例，仅检查用户是否具有一个表示特定字段权限的通用声明。
            // (The actual permission checking logic would be more complex, potentially involving querying user permissions from a database or cache.
            // This is an example, only checking if the user has a generic claim representing the specific field permission.)

            // Example: Permission claim might look like "CanWriteUserEmail" or "CanReadProductPrice"
            // Or more generic: "FieldPermission.User.Email.Write"
            string requiredPermission = $"FieldPermission.{requirement.EntityTypeName}.{requirement.FieldName}.{requirement.OperationType}";

            // Chinese Comment: 从 UserContext 或 HttpContext 中获取当前用户信息。
            // (Get current user information from UserContext or HttpContext.)
            var currentUser = UserContext.Current; // Assuming UserContext provides claims or roles

            if (currentUser != null && context.User.HasClaim(c => c.Type == "permission" && c.Value == requiredPermission))
            {
                // Chinese Comment: 用户拥有所需权限。
                // (User has the required permission.)
                context.Succeed(requirement);
                Logger.Log(LogLevel.Debug, LogEvent.Authorization, $"用户 {currentUser.UserName} 对实体 {requirement.EntityTypeName} 的字段 {requirement.FieldName} 的 {requirement.OperationType} 操作授权成功。 (User {currentUser.UserName} authorized for {requirement.OperationType} on field {requirement.FieldName} of entity {requirement.EntityTypeName}.)");
            }
            else
            {
                // Chinese Comment: 用户没有所需权限。可以选择 Fail() 或不调用 Succeed() 来表示失败。
                // (User does not have the required permission. Can optionally call Fail() or just not call Succeed() to indicate failure.)
                // context.Fail(); // Explicitly fail
                Logger.Log(LogLevel.Warning, LogEvent.AuthorizationFailure, $"用户 {(currentUser?.UserName ?? "匿名")} 对实体 {requirement.EntityTypeName} 的字段 {requirement.FieldName} 的 {requirement.OperationType} 操作授权失败。 (User {(currentUser?.UserName ?? "Anonymous")} failed authorization for {requirement.OperationType} on field {requirement.FieldName} of entity {requirement.EntityTypeName}.)");
            }
            return Task.CompletedTask;
        }
    }
}
