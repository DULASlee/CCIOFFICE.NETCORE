// ===== composables/useTabs.js =====
export function useTabs(dataConfig, router, proxy) {
  // 这里假设IndexTabs已经被正确导入
  const IndexTabs = require('@/views/index/IndexTabs.js').default
  const tabsManager = IndexTabs(proxy, dataConfig, router)
  
  const closeTabsMenu = () => {
    dataConfig.contextMenuVisible.value = false
  }
  
  return {
    navigation: dataConfig.navigation,
    selectId: dataConfig.selectId,
    contextMenuVisible: dataConfig.contextMenuVisible,
    visibleItem: dataConfig.visibleItem,
    menuTop: dataConfig.menuTop,
    menuLeft: dataConfig.menuLeft,
    ...tabsManager,
    closeTabsMenu
  }
}