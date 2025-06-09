# VolImageViewer.vue 代码说明

## 文件位置
`src/components/basic/VolImageViewer.vue`

## 组件功能
这是一个基于Element Plus的图片查看器组件，主要功能包括：
1. 支持多图片查看
2. 支持指定初始显示图片
3. 支持图片切换
4. 集成关闭功能

## 核心代码结构

### 1. 模板结构
```html
<template>
  <el-image-viewer 
    v-if="showImageViewer"
    :initial-index="initialIndex"
    :url-list="imageViewerList"
    @close="closeViewer"
  />
</template>
```

### 2. 主要API

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| show() | 显示图片查看器 | (imgs, index) | void |
| closeViewer() | 关闭图片查看器 | - | void |
| initialIndex | 初始显示图片索引 | - | Number |
| imageViewerList | 图片URL列表 | - | Array |
| showImageViewer | 是否显示查看器 | - | Boolean |

## 关键实现细节

### 1. 图片查看器控制
```javascript
setup() {
  const initialIndex = ref(0);
  const imageViewerList = ref([]);
  const showImageViewer = ref(false);

  const show = (imgs, index) => {
    initialIndex.value = index || 0;
    imageViewerList.value = Array.isArray(imgs) ? imgs : [imgs];
    showImageViewer.value = true;
  };

  const closeViewer = () => {
    showImageViewer.value = false;
  };

  return {
    initialIndex,
    imageViewerList,
    showImageViewer,
    closeViewer,
    show
  };
}
```

### 2. 组件使用方式
```javascript
// 在父组件中
<vol-image-viewer ref="viewer"/>

// 调用显示
this.$refs.viewer.show(images, startIndex);
```

## 使用示例

### 基本使用
```javascript
// 模板
<vol-image-viewer ref="viewer"/>

// 脚本
methods: {
  previewImages() {
    const images = [
      'https://example.com/image1.jpg',
      'https://example.com/image2.jpg'
    ];
    this.$refs.viewer.show(images, 0);
  }
}
```

### 单张图片查看
```javascript
this.$refs.viewer.show('https://example.com/single-image.jpg');
```

## 注意事项
1. 需要Element Plus的el-image-viewer组件支持
2. 图片URL需要完整的可访问路径
3. 初始索引从0开始计算
4. 点击遮罩或关闭按钮会自动关闭查看器
5. 支持键盘左右箭头切换图片
