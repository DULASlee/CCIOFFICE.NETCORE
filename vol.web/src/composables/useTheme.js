// ===== composables/useTheme.js =====
import { ref } from 'vue'

export function useTheme(proxy, dataConfig) {
  const theme = ref(proxy.$global.theme || '')
  const layout = dataConfig.layout
  const color = ref('')
  const drawer_model = dataConfig.drawer_model
  
  const layoutIsLeft = () => {
    return layout.value === 'left'
  }
  
  const getColor = () => {
    color.value = layoutIsLeft() || theme.value === 'dark' ? '#000' : '#ffff'
  }
  
  const initTheme = () => {
    // 布局初始化
    layout.value = localStorage.getItem('vol-layout')
    if (!layout.value) {
      layout.value = proxy.$global.layout || 'top'
    }
    
    // 主题初始化
    theme.value = localStorage.getItem('vol-theme')
    if (!theme.value) {
      if (layoutIsLeft()) {
        theme.value = proxy.$global.theme + '-aside'
      } else {
        theme.value = proxy.$global.theme
      }
    }
    
    getColor()
  }
  
  const layoutChange = (layoutValue, themeValue) => {
    layout.value = layoutValue
    theme.value = themeValue
    getColor()
  }
  
  return {
    theme,
    layout,
    color,
    drawer_model,
    layoutIsLeft,
    layoutChange,
    initTheme,
    getColor
  }
}