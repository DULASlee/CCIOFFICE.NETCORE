# API模块文档结构说明

## 文档目录结构

```
api/
├── README.md               # API文档说明
├── sys/                    # 系统管理模块
│   ├── user.js说明.md      # 用户管理API
│   ├── role.js说明.md      # 角色管理API
│   └── menu.js说明.md      # 菜单管理API
├── builder/                # 代码生成模块
│   ├── coder.js说明.md     # 代码生成API
├── workflow/               # 工作流模块
│   ├── flow.js说明.md      # 流程定义API
│   └── task.js说明.md      # 任务管理API
└── common/                 # 通用模块
    ├── upload.js说明.md    # 文件上传API
    └── dict.js说明.md      # 数据字典API
```

## 文档编写规范

每篇API文档应包含：
1. **模块概述** - 简要说明模块功能
2. **API列表** - 列出所有接口及其用途
3. **使用示例** - 调用示例代码
4. **注意事项** - 特殊参数或限制说明

## 示例文档结构

```markdown
# 用户管理API (sys/user.js)

## 模块概述
用户账号管理相关接口...

## API列表
1. `getUserList` - 获取用户列表
2. `addUser` - 新增用户
...

## 使用示例
```javascript
import userApi from '@/api/sys/user'

// 获取用户列表
userApi.getUserList({page:1,rows:10})
```
```

## 注意事项
1. 新增用户需管理员权限
2. 密码字段需要加密传输
...
