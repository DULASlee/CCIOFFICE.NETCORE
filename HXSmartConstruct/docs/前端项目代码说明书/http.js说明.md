# HTTP请求封装说明 (http.js)

## 文件概述
本文件封装了项目所有HTTP请求逻辑，主要功能包括：
- 统一的axios实例配置
- 请求/响应拦截处理
- 全局加载状态管理
- 自动Token刷新
- 文件下载功能

## 核心配置

### 1. 多环境配置
```javascript
if (process.env.NODE_ENV == 'development') {
  axios.defaults.baseURL = 'http://localhost:9991/'
} else if (process.env.NODE_ENV == 'production') {
  axios.defaults.baseURL = 'http://api.volcore.xyz/' 
}
```

### 2. 请求拦截器
```javascript
axios.interceptors.request.use(config => {
  // 设置Authorization头
  config.headers.Authorization = store.getters.getToken()
  return config
})
```

### 3. 响应拦截器
```javascript
axios.interceptors.response.use(
  response => {
    // 检查Token过期
    if(response.headers.vol_exp == '1') {
      replaceToken()
    }
    return response
  },
  error => {
    // 统一错误处理
    handleError(error)
  }
)
```

## 主要API说明

### 1. post请求
```javascript
/**
 * @param {String} url 请求地址
 * @param {Object} params 请求参数
 * @param {Boolean|String} loading 加载提示
 * @param {Object} config 额外配置
 */
post(url, params, loading, config)
```

### 2. get请求
```javascript
get(url, params, loading, config)
```

### 3. 文件下载
```javascript
download(url, params, fileName, loading, callback)
```

## 特殊功能实现

### 1. Token自动刷新
```javascript
function replaceToken() {
  ajax({
    url: '/api/User/replaceToken',
    success: (res) => {
      store.commit('setUserInfo', {
        ...store.getters.getUserInfo(),
        token: res.data.token
      })
    }
  })
}
```

### 2. 全局加载状态
```javascript
function showLoading(loading) {
  loadingInstance = Loading.service({
    text: typeof loading == 'string' ? loading : 'loading...'
  })
}
```

## 使用示例

### 1. 普通请求
```javascript
import http from '@/api/http'

http.post('/api/user/list', {page:1}).then(res => {
  // 处理结果
})
```

### 2. 文件下载
```javascript
http.download('/api/export', {id:1}, 'report.xlsx', '正在导出...')
```

## 注意事项
1. 所有API请求都应使用此封装
2. 生产环境需确保baseURL配置正确
3. 401错误会自动跳转登录页
4. 大文件下载需要特殊处理
