# VolPermission组件说明文档

## 组件概述
VolPermission是项目的权限控制组件，主要功能包括：
- 按钮级权限控制
- 菜单权限控制
- 数据权限控制
- 权限验证与鉴权

## 核心API说明

### 权限验证
```js
/**
 * 验证按钮权限
 * @param {String} permission 权限标识
 * @return {Boolean} 是否有权限
 */
hasPermission(permission)

/**
 * 验证菜单权限
 * @param {String} menuPath 菜单路径
 * @return {Boolean} 是否有权限
 */ 
hasMenuPermission(menuPath)
```

### 权限控制
```js
/**
 * 注册权限按钮
 * @param {Array} buttons 按钮配置数组
 * @param {Object} permissions 权限集合
 */
registerButtons(buttons, permissions)

/**
 * 过滤无权限按钮
 * @param {Array} buttons 按钮配置数组
 * @return {Array} 过滤后的按钮数组
 */
filterButtons(buttons)
```

## 使用示例

### 按钮权限控制
```vue
<template>
  <el-button 
    v-if="$permission.hasPermission('user:add')"
    @click="addUser"
  >
    添加用户
  </el-button>
</template>
```

### 动态注册按钮权限
```js
created() {
  this.$permission.registerButtons(this.buttons, this.$store.getters.getPermissions)
}
```

## 权限配置格式
权限标识采用`资源:操作`格式，例如：
- `user:add` 用户添加权限
- `user:edit` 用户编辑权限
- `menu:system` 系统菜单权限

## 注意事项
1. 权限数据需要在登录后从接口获取并存入Vuex
2. 按钮权限建议在页面初始化时注册
3. 菜单权限由路由守卫自动验证
