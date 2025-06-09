# Editor 富文本编辑器说明

## 功能概述
基于WangEditor的增强编辑器，提供：
- 丰富的文本编辑功能
- 自定义扩展能力
- 图片/视频上传集成
- 内容XSS防护

## 文件结构
```
editor/
├── VolWangEditor.vue       // 主组件
├── config.js               // 编辑器配置
├── extensions/             // 扩展插件
│   ├── formula-plugin.js   // 公式插件
│   └── attachment.js       // 附件插件
└── utils/                  // 工具类
```

## 核心功能

### 1. 基础集成
```vue
<template>
  <VolWangEditor v-model="content" />
</template>
```

### 2. 配置选项
```javascript
editorConfig: {
  menus: [
    'head', 'bold', 'italic', 'underline',
    'image', 'video', 'table', 'code'
  ],
  uploadImgServer: '/api/upload-img'
}
```

## 扩展功能

### 1. 自定义插件
```javascript
// 注册公式插件
editor.use(FormulaPlugin, {
  menuKey: 'formula',
  renderLatex: true
})
```

### 2. 内容过滤
```javascript
// XSS防护配置
{
  customFilter: (html) => {
    return filterXSS(html, {
      whiteList: {
        a: ['href', 'title'],
        img: ['src']
      }
    })
  }
}
```

## 最佳实践

### 1. 内容处理
```javascript
// 获取纯文本
const plainText = editor.getText()

// 获取带格式HTML
const htmlContent = editor.getHtml()
```

### 2. 图片处理
```javascript
// 自定义上传
{
  uploadImgHooks: {
    customInsert: (insertImgFn, result) => {
      insertImgFn(result.data.url)
    }
  }
}
```

## 注意事项
1. 避免直接渲染用户输入内容
2. 大图片需压缩后上传
3. 生产环境关闭调试模式
4. 定期更新依赖版本
