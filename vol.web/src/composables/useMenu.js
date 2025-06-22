// ===== composables/useMenu.js =====
import { ref, reactive } from 'vue'

export function useMenu(dataConfig, router, proxy) {
  const navKey = 'nav:id'
  
  // 从dataConfig获取数据
  const menuData = dataConfig.menuData
  const navMenuList = dataConfig.navMenuList
  const currentMenuId = dataConfig.currentMenuId
  const navCurrentMenuId = dataConfig.navCurrentMenuId
  const isCollapse = dataConfig.isCollapse
  const menuWidth = dataConfig.menuWidth
  const menuOptions = dataConfig.menuOptions
  
  // 初始化导航菜单
  navCurrentMenuId.value = localStorage.getItem(navKey) * 1 || -1
  
  // 获取选中菜单名称
  const getSelectMenuName = (id) => {
    return menuOptions.value.find(x => x.id == id)
  }
  
  // 菜单选择事件 - 注意这里需要在onSelect定义后才能使用
  const onSelect = (treeId) => {
    // 这里会在初始化后被重新赋值
  }
  
  // 顶部菜单点击
  const menuDataClick = (mItem, index) => {
    if (navCurrentMenuId.value === navMenuList[index].id) {
      return
    }
    
    navCurrentMenuId.value = navMenuList[index].id
    localStorage.setItem(navKey, navCurrentMenuId.value)
    menuData.splice(0)
    menuData.push(...navMenuList[index].children)
  }
  
  // 侧边栏折叠
  const toggleLeft = () => {
    isCollapse.value = !isCollapse.value
    menuWidth.value = isCollapse.value ? 63 : 200
  }
  
  return {
    menuData,
    navMenuList,
    currentMenuId,
    navCurrentMenuId,
    isCollapse,
    menuWidth,
    menuOptions,
    onSelect,
    menuDataClick,
    toggleLeft,
    getSelectMenuName
  }
}