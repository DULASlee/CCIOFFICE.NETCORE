# 前端组件文档索引

## 文档结构
```
docs/
├── 前端组件说明书/
│   ├── README.md           ← 当前文件
│   ├── basic/              # 基础组件
│   │   ├── ViewGrid说明.md
│   │   ├── VolForm说明.md
│   │   ├── VolTable说明.md
│   │   ├── VolBox说明.md
│   │   └── VolUpload说明.md
│   ├── editor/             # 编辑器
│   │   └── Editor说明.md
│   ├── lang/               # 国际化
│   │   └── Lang说明.md
│   ├── redirect/           # 路由相关
│   │   └── Redirect说明.md
│   ├── VolProvider/        # 状态管理
│   │   └── VolProvider说明.md
│   └── workflow/           # 工作流
│       └── Workflow说明.md
```

## 快速开始

### 1. 基础组件使用
```javascript
// 引入组件
import { VolForm, VolTable } from '@/components'

// 注册组件
app.use(VolForm).use(VolTable)
```

### 2. 状态管理初始化
```javascript
import VolProvider from '@/components/VolProvider'

const store = new VolProvider({
  state: {...},
  modules: {...}
})
```

## 文档更新指南
1. 组件重大变更时需同步更新文档
2. 新增组件需创建对应说明文件
3. 文档路径需与组件位置保持一致

## 注意事项
1. 所有组件文档必须包含基础使用示例
2. 复杂组件需提供典型场景解决方案
3. 保持文档与代码实现同步更新
