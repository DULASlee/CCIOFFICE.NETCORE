# HXSmartConstruct 前端项目

## 项目概述
这是一个基于Vue 3 + Element Plus的前端工程项目，包含了一系列可复用的高质量组件，助力快速开发企业级管理系统。

## 核心特性
- 🏗️ 模块化组件设计
- 📊 丰富的数据展示组件
- 🖥️ 可视化表单设计器
- 📁 强大的文件上传管理
- ✍️ 集成富文本编辑器
- 🎨 统一的UI风格

## 组件文档

### 基础组件
| 组件 | 说明 | 文档链接 |
|------|------|----------|
| VolTable | 动态表格组件 | [查看文档](./docs/前端项目代码说明书/VolTable.vue说明.md) |
| VolBox | 弹窗对话框组件 | [查看文档](./docs/前端项目代码说明书/VolBox.vue说明.md) |
| VolFormDraggable | 表单设计器 | [查看文档](./docs/前端项目代码说明书/VolFormDraggable.vue说明.md) |
| VolUpload | 文件上传组件 | [查看文档](./docs/前端项目代码说明书/VolUpload.vue说明.md) |

### 编辑器组件
| 组件 | 说明 | 文档链接 |
|------|------|----------|
| VolWangEditor | 富文本编辑器 | [查看文档](./docs/前端项目代码说明书/VolWangEditor.vue说明.md) |

### 工具组件
| 组件 | 说明 | 文档链接 |
|------|------|----------|
| VolImageViewer | 图片查看器 | [查看文档](./docs/前端项目代码说明书/VolImageViewer.vue说明.md) |

## 快速开始

### 安装依赖
```bash
npm install
```

### 开发模式
```bash
npm run dev
```

### 生产构建
```bash
npm run build
```

## 项目结构
```
HXSmartConstruct/
├── docs/                       # 项目文档
├── public/                     # 静态资源
├── src/
│   ├── assets/                 # 静态资源
│   ├── components/             # 公共组件
│   │   ├── basic/              # 基础组件
│   │   └── editor/             # 编辑器组件
│   ├── views/                  # 页面视图
│   └── main.js                 # 入口文件
└── README.md                   # 项目说明
```

## 最佳实践
1. 表格组件使用`VolTable`统一风格
2. 弹窗使用`VolBox`保持一致性
3. 表单设计使用`VolFormDraggable`可视化构建
4. 文件上传使用`VolUpload`统一管理

## 反馈与贡献
如有任何问题或建议，请提交Issue或Pull Request。

## License
MIT
