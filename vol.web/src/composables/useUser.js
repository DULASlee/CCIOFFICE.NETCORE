// ===== composables/useUser.js =====
import { reactive, computed } from 'vue'

export function useUser(store, router, dataConfig, drawer_model, open) {
  const userInfo = dataConfig.userInfo
  
  const userDropItems = reactive([
    { text: '消息管理', icon: 'el-icon-bell', hidden: true },
    { text: '个人中心', path: '/userInfo', icon: 'el-icon-user' },
    {
      text: '基础设置',
      icon: 'el-icon-setting',
      click: () => {
        drawer_model.value = true
      }
    },
    { text: '安全退出', path: '/login', icon: 'el-icon-switch-button' }
  ])
  
  const icons = computed(() => {
    return userDropItems.filter(x => !x.hidden)
  })
  
  const linkClick = (item) => {
    // 1. 检查自定义 onClick
    if (item.onClick && typeof item.onClick === 'function') {
      item.onClick()
      return
    }
    
    // 2. 处理内部路由
    if (item.path && item.path.startsWith('/')) {
      // 使用传入的 open 函数（标签页系统）
      open({
        name: item.name || item.text,
        path: item.path,
        id: item.id || item.path,
        ...item
      }, false)
      return
    }
    
    // 3. 处理外部链接
    if (item.path && item.path.startsWith('http')) {
      window.open(item.path, '_blank')
      return
    }

    if (item.click) {
      item.click()
      return
    }
    if (!item.path) {
      item.path = ''
    }
    if (item.path.indexOf('http') != -1) {
      window.open(item.path)
      return
    }
    if (typeof item == 'string' || item.path == '/login') {
      if (item == '/login' || item.path == '/login') {
        store.commit('clearUserInfo', '')
        window.location.reload()
        return
      }
      router.push({ path: item })
      return
    }
    if (item.path == '#') return
    open(item)

    
  }





  
  const getErrorImg = ($e) => {
    $e.target.src = userInfo.errorImg
  }
  
  return {
    userInfo,
    userDropItems,
    icons,
    linkClick,
    getErrorImg
  }
}