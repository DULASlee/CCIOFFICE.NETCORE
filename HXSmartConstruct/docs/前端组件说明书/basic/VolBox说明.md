# VolBox 弹窗组件说明

## 功能概述
VolBox是模态弹窗组件，提供：
- 多种弹窗类型支持
- 动画效果与拖拽功能
- 全屏/可调节尺寸
- 嵌套表单与复杂内容

## 文件结构
```
VolBox/
├── index.js           // 组件入口
├── VolBox.vue         // 主组件
└── style.less        // 样式文件
```

## 核心功能

### 1. 基础弹窗
```vue
<VolBox
  v-model="visible"
  title="提示"
  @confirm="handleConfirm"
>
  <p>弹窗内容</p>
</VolBox>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| title | String | '' | 弹窗标题 |
| width | String | '50%' | 弹窗宽度 |
| fullscreen | Boolean | false | 是否全屏 |

### 3. 插槽说明
| 插槽名 | 说明 |
|--------|------|
| default | 主体内容 |
| footer | 底部按钮区 |
| title | 自定义标题 |

## 高级功能

### 1. 嵌套表单
```vue
<VolBox>
  <VolForm :form-options="formOptions"/>
</VolBox>
```

### 2. 异步关闭
```javascript
async handleConfirm(done) {
  const valid = await validateForm();
  if (valid) {
    done(); // 手动关闭弹窗
  }
}
```

## 使用示例

### 1. 消息确认弹窗
```javascript
this.$VolBox.confirm({
  title: '删除确认',
  content: '确定要删除吗？',
  onConfirm: () => {
    // 删除逻辑
  }
})
```

### 2. 复杂内容弹窗
```vue
<VolBox
  title="用户详情"
  :visible.sync="detailVisible"
  width="800px"
>
  <UserDetail :user-id="currentId"/>
</VolBox>
```

## 注意事项
1. 避免在弹窗中嵌套过多层级
2. 移动端需特殊处理全屏样式
3. 动态内容需要手动处理响应式
4. 多个弹窗需注意z-index管理
