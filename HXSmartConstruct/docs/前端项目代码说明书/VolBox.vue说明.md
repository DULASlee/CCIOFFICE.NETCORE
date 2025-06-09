# VolBox.vue 代码说明

## 文件位置
`src/components/basic/VolBox.vue`

## 组件功能
这是一个高度可配置的弹窗对话框组件，主要功能包括：
1. 支持多种尺寸和布局的弹窗
2. 支持全屏模式
3. 内置滚动条
4. 支持拖拽功能
5. 支持自定义头部和底部

## 核心代码结构

### 1. 模板结构
```html
<template>
  <div class="vol-dialog">
    <el-dialog
      v-model="vmodel"
      :width="width"
      :fullscreen="fullscreen"
      :draggable="draggable"
    >
      <!-- 自定义头部 -->
      <template #header>
        <i :class="icon"></i> {{ title }}
        <slot name="header"></slot>
      </template>
      
      <!-- 内容区域 -->
      <el-scrollbar :max-height="contentHeight">
        <div class="srcoll-content">
          <slot name="content"></slot>
          <slot></slot>
        </div>
      </el-scrollbar>
      
      <!-- 自定义底部 -->
      <template #footer>
        <div class="dia-footer">
          <slot name="footer"></slot>
        </div>
      </template>
    </el-dialog>
  </div>
</template>
```

### 2. 主要属性配置

| 属性 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| modelValue | 控制弹窗显示 | Boolean | false |
| title | 弹窗标题 | String | '基本信息' |
| width | 弹窗宽度 | Number | 650 |
| height | 内容高度 | Number | 0 |
| icon | 标题图标 | String | 'el-icon-warning-outline' |
| draggable | 是否可拖拽 | Boolean | false |
| fullscreen | 是否全屏 | Boolean | false |
| destroyOnClose | 关闭时销毁内容 | Boolean | false |

## 关键实现细节

### 1. 弹窗控制逻辑
```javascript
// 控制弹窗显示/隐藏
const vmodel = ref(false);
watch(() => props.modelValue, (newVal) => {
  vmodel.value = newVal;
});

// 关闭弹窗处理
const handleClose = (done, iconClose) => {
  let result = props.onModelClose(!!iconClose);
  if (result === false) return;
  vmodel.value = false;
  context.emit("update:modelValue", false);
  done && done();
};
```

### 2. 全屏切换功能
```javascript
const fullscreen = ref(false);

const handleFullScreen = () => {
  fullscreen.value = !fullscreen.value;
  context.emit("fullscreen", fullscreen.value);
};
```

### 3. 高度自适应计算
```javascript
// 计算内容高度
const clientHeight = document.body.clientHeight * 0.95 - 60;
const contentHeight = ref(props.height || clientHeight);
```

## 使用示例

### 基本使用
```javascript
<vol-box v-model="dialogVisible" title="用户信息">
  <template #content>
    <p>这里是弹窗内容</p>
  </template>
</vol-box>
```

### 带底部按钮
```javascript
<vol-box v-model="dialogVisible" title="编辑用户">
  <template #content>
    <user-form :user="currentUser"/>
  </template>
  
  <template #footer>
    <el-button @click="saveUser">保存</el-button>
    <el-button @click="dialogVisible=false">取消</el-button>
  </template>
</vol-box>
```

### 全屏弹窗
```javascript
<vol-box 
  v-model="dialogVisible" 
  title="全屏查看" 
  :fullscreen="true"
  :showFull="false"
>
  <large-content/>
</vol-box>
```

## 注意事项
1. 需要配合Element Plus组件库使用
2. 内容高度超过视口高度时会自动显示滚动条
3. 全屏模式下会隐藏全屏切换按钮
4. 关闭弹窗前可以通过onModelClose拦截
5. 使用slot插槽可以高度自定义内容
