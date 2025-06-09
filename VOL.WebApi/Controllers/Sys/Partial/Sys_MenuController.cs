using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace VOL.Sys.Controllers
{
    public partial class Sys_MenuController
    {
        private readonly ILogger<Sys_MenuController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_MenuController(
            ISys_MenuService service,
            ILogger<Sys_MenuController> logger
        )
        : base(service)
        {
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet, HttpPost, Route("getTreeMenu")]
        // [ApiActionPermission("Sys_Menu", ActionPermissionOptions.Search)]
        public IActionResult GetTreeMenu()
        {
            _logger.LogInformation("GetTreeMenu called.");
            try
            {
                return Json(Service.GetCurrentMenuActionList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeMenu.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching tree menu." });
            }
        }
        [HttpPost, Route("getMenu")]
        [ApiActionPermission("Sys_Menu", ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetMenu()
        {
            _logger.LogInformation("GetMenu called.");
            try
            {
                return Json(await Service.GetMenu());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMenu.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching menu." });
            }
        }

        [HttpPost, Route("getTreeItem")]
        [ApiActionPermission("Sys_Menu", "1", ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetTreeItem(int menuId)
        {
            _logger.LogInformation("GetTreeItem called with menuId: {MenuId}", menuId);
            if (menuId <= 0)
            {
                _logger.LogWarning("GetTreeItem called with invalid menuId: {MenuId}", menuId);
                return new BadRequestObjectResult(new { status = false, message = "Invalid Menu ID." });
            }
            try
            {
                return Json(await Service.GetTreeItem(menuId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeItem for menuId: {MenuId}", menuId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching tree item." });
            }
        }

        //[ActionPermission("Sys_Menu", "1", ActionPermissionOptions.Add)]
        //只有角色ID为1的才能进行保存操作
        [HttpPost, Route("save"), ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<ActionResult> Save([FromBody] Sys_Menu menu)
        {
            _logger.LogInformation("Save (Sys_Menu) called for menu ID: {MenuId}, Name: {MenuName}", menu?.Menu_Id, menu?.MenuName);
            if (menu == null)
            {
                _logger.LogWarning("Save (Sys_Menu) called with null menu data.");
                return new BadRequestObjectResult(new { status = false, message = "Menu data cannot be null." });
            }
            try
            {
                return Json(await Service.Save(menu));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Save (Sys_Menu) for menu ID: {MenuId}, Name: {MenuName}", menu.Menu_Id, menu.MenuName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while saving menu data." });
            }
        }

        /// <summary>
        /// 限制只能超级管理员才删除菜单 
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost, Route("delMenu")]
        public async Task<ActionResult> DelMenu(int menuId)
        {
            _logger.LogInformation("DelMenu called for menuId: {MenuId}", menuId);
            if (menuId <= 0)
            {
                _logger.LogWarning("DelMenu called with invalid menuId: {MenuId}", menuId);
                return new BadRequestObjectResult(new { status = false, message = "Invalid Menu ID for deletion." });
            }
            try
            {
                return Json(await Service.DelMenu(menuId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DelMenu for menuId: {MenuId}", menuId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while deleting menu." });
            }
        }

    }
}
