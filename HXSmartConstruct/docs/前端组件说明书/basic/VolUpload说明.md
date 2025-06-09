# VolUpload 上传组件说明

## 功能概述
VolUpload是增强型文件上传组件，提供：
- 多文件/分片上传
- 文件类型/大小限制
- 上传进度显示
- 预览与下载功能

## 文件结构
```
VolUpload/
├── index.js           // 组件入口
├── VolUpload.vue      // 主组件
├── utils.js           // 工具方法
└── style.less        // 样式文件
```

## 核心功能

### 1. 基础使用
```vue
<VolUpload
  action="/api/upload"
  v-model="fileList"
  :limit="3"
/>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| action | String | - | 上传地址 |
| accept | String | - | 接受文件类型 |
| chunk | Boolean | false | 是否分片上传 |

## 高级功能

### 1. 分片上传
```javascript
// 分片配置
{
  chunk: true,
  chunkSize: 2 * 1024 * 1024, // 2MB
  parallel: 3 // 并发数
}
```

### 2. 自定义请求
```javascript
customRequest(file) {
  const formData = new FormData();
  formData.append('file', file);
  return axios.post('/custom-upload', formData);
}
```

## 使用示例

### 1. 图片上传
```vue
<VolUpload
  action="/api/upload"
  accept="image/*"
  :preview="true"
  :max-size="5"
/>
```

### 2. 文件管理
```javascript
// 文件列表控制
fileList: [
  {
    name: 'document.pdf',
    url: '/files/document.pdf',
    status: 'done'
  }
]
```

## 注意事项
1. 大文件必须启用分片上传
2. 服务端需对应支持分片/合并
3. 跨域上传需要配置CORS
4. 生产环境建议添加权限验证
