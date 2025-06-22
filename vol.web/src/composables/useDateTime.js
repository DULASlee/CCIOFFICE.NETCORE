// ===== composables/useDateTime.js =====
import { ref, onUnmounted } from 'vue'

export function useDateTime(proxy) {
  const indexDate = ref('')
  let interval
  
  const startDateTime = () => {
    indexDate.value = proxy.base.getDate(true)
    interval = setInterval(() => {
      indexDate.value = proxy.base.getDate(true)
    }, 1000)
  }
  
  onUnmounted(() => {
    if (interval) {
      clearInterval(interval)
    }
  })
  
  return {
    indexDate,
    startDateTime
  }
}