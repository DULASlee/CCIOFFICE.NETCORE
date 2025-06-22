# Vue页面代码生成器开发手册

## 目录
1. [功能概述](#功能概述)
2. [核心文件结构](#核心文件结构)
3. [实现流程](#实现流程)
4. [关键代码解析](#关键代码解析)
5. [使用说明](#使用说明)

## 功能概述

代码生成器是一个用于快速生成Vue页面、Model类和业务类的工具。它能够根据数据库表结构自动生成对应的前端页面和后端代码，大大提高开发效率。主要功能包括：

- 生成Vue页面
- 生成移动端页面
- 生成Model类
- 生成业务类
- 同步表结构
- 管理代码配置信息

## 核心文件结构

```
src/
├── views/
│   └── builder/
│       ├── coder.vue            # 代码生成器主界面
│       └── builderData.jsx      # 代码生成器配置数据
├── api/
│   └── http.js                  # API请求封装
└── components/
    └── basic/
        ├── VolForm.vue          # 表单组件
        └── VolTable.vue         # 表格组件
```

## 实现流程

### 1. 页面初始化流程

1. 加载表结构树
2. 初始化命名空间
3. 初始化配置信息

```javascript
// coder.vue
created() {
    // 获取表结构树
    this.http.post('/api/builder/GetTableTree', {}, false).then((x) => {
        let list = JSON.parse(x.list)
        // 处理父子关系
        list.forEach((c) => {
            if (c.parentId && !list.some((a) => { return a.id == c.parentId })) {
                c.parentId = 0
                c.pId = 0
            }
        })
        this.tree = list
        
        // 初始化命名空间
        let nameSpace = JSON.parse(x.nameSpace)
        let nameSpaceArr = []
        nameSpace.forEach(space => {
            nameSpaceArr.push({ key: space, value: space })
        })
        
        // 设置命名空间选项
        this.initNamespaceOptions(nameSpaceArr)
    })
}
```

### 2. 生成Vue页面流程

生成Vue页面的核心流程如下：

1. 验证表单信息
2. 获取Vue文件路径
3. 调用后端接口生成代码

```javascript
// coder.vue
ceateVuePage(isApp) {
    // 验证表单信息
    this.validateTableInfo(() => {
        let vuePath
        if (!isApp) {
            // 获取Vue页面路径
            vuePath = localStorage.getItem('vuePath')
            if (!vuePath) {
                return this.$message.error('请先设置Vue项目对应Views的绝对路径,然后再保存!')
            }
        } else {
            // 获取App页面路径
            vuePath = localStorage.getItem('appPath')
            if (!vuePath) {
                return this.$message.error('请先设置app路径,然后再保存!')
            }
        }

        // 调用后端生成页面接口
        let url = `/api/builder/createVuePage?vuePath=${vuePath}&vite=1&v3=1&app=${isApp || 0}`
        this.http.post(url, this.tableInfo, true).then((x) => {
            this.$Message.info(x)
        })
    })
}
```

### 3. 数据校验流程

在生成代码之前，需要对表结构进行严格的校验：

```javascript
validateTableInfo(callback) {
    this.$refs.form.validate(() => {
        // 基础验证
        if (!this.tableInfo) {
            this.$message.error('请先加载数据')
            return false
        }
        
        // 主键验证
        if (this.data && this.data.length > 0) {
            let keyInfo = this.data.find((x) => {
                return x.isKey
            })
            if (!keyInfo) {
                this.$message.error('请勾选设置主键')
                return false
            }
            if (keyInfo.isNull == 1) {
                this.$message.error('主键【可为空】必须设置为否')
                return false
            }
            // 非自增主键验证
            if (keyInfo.columnType != 'int' && 
                keyInfo.columnType != 'bigint' && 
                !this.layOutOptins.fields.sortName) {
                this.$message.error('主键非自增类型,请设置上面表单的【排序字段】')
                return false
            }
        }

        // 更新表信息
        for (const key in this.tableInfo) {
            if (this.layOutOptins.fields.hasOwnProperty(key)) {
                this.tableInfo[key] = this.layOutOptins.fields[key]
            }
        }
        callback()
    })
}
```

## 关键代码解析

### 1. 表结构同步

```javascript
syncTable() {
    if (!this.layOutOptins.fields.tableName) {
        return this.$Message.error('请选模块')
    }
    
    // 调用同步接口
    this.http.post(
        '/api/builder/syncTable?tableName=' + this.layOutOptins.fields.tableName,
        {},
        true
    ).then((x) => {
        if (!x.status) {
            return this.$Message.error(x.message)
        }
        this.$Message.info(x.message)
        // 重新加载表信息
        this.loadTableInfo(this.layOutOptins.fields.table_Id)
    })
}
```

### 2. 配置信息保存

```javascript
save() {
    // 保存路径配置
    localStorage.setItem('vuePath', this.layOutOptins.fields.vuePath || '')
    localStorage.setItem('appPath', this.layOutOptins.fields.appPath || '')

    // 主键验证
    if (this.tableInfo && 
        this.tableInfo.tableColumns && 
        this.tableInfo.tableColumns.length &&
        this.tableInfo.tableColumns.filter(x => x.isKey == 1).length > 1) {
        return this.$Message.error('表结构只能勾选一个主键字段')
    }

    // 保存配置信息
    this.validateTableInfo(() => {
        this.http.post('/api/builder/Save', this.tableInfo, true).then((x) => {
            if (!x.status) {
                this.$Message.error(x.message)
                return
            }
            
            // 更新树节点信息
            this.updateTreeNode(x.data)
            
            // 更新表单数据
            this.updateFormData(x.data)
        })
    })
}
```

## 使用说明

### 1. 基本步骤

1. 选择或创建模块
2. 配置表信息
3. 设置Vue页面路径
4. 生成代码

### 2. 注意事项

- 修改表结构后需要点击"同步表结构"
- 确保已正确设置Vue项目路径
- 主键必须正确配置
- 保存配置后再生成代码

### 3. 常见问题

1. 生成失败
   - 检查路径配置
   - 确认表结构完整性
   - 验证主键设置

2. 页面不完整
   - 检查表字段配置
   - 确认模板文件存在
   - 查看生成日志

## 结语

代码生成器是提高开发效率的重要工具，正确使用可以大大减少重复性工作。建议在使用过程中注意保存配置，并在生成代码后进行必要的调整和优化。
