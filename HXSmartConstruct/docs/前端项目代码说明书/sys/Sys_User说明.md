# 用户管理模块说明 (Sys_User.jsx)

## 模块概述
本文件定义了用户管理模块的前端扩展配置，主要功能包括：
- 用户管理界面组件扩展
- 自定义按钮配置
- 特殊逻辑处理方法

## 核心配置

### 1. 组件扩展点
```javascript
components: {
  gridHeader: '',    // 表格头部扩展组件
  gridBody: '',      // 表格内容扩展组件 
  gridFooter: '',    // 表格底部扩展组件
  modelHeader: '',   // 弹窗头部扩展组件
  modelBody: '',     // 弹窗内容扩展组件
  modelFooter: ''    // 弹窗底部扩展组件
}
```

### 2. 标准API接口
通过http.js调用的标准接口：
- `POST /api/user/getPageData` 获取用户分页数据
- `POST /api/user/add` 新增用户
- `POST /api/user/update` 修改用户
- `POST /api/user/delete` 删除用户

## 使用示例

### 1. 获取用户列表
```javascript
import http from '@/api/http'

http.post('/api/user/getPageData', {
  page: 1,
  rows: 10,
  wheres: []
}).then(data => {
  // 处理用户数据
})
```

### 2. 扩展表格组件
```javascript
// 在扩展配置中
components: {
  gridBody: '@/extension/sys/custom/UserGridBody.vue'
}
```

## 注意事项
1. 用户密码字段需要加密传输
2. 管理员权限才能操作用户数据
3. 扩展组件需实现标准接口
4. 生产环境需严格校验权限
