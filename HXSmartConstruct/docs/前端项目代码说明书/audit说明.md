# 审批流程组件说明

## 组件概述
审批流程组件提供统一的审批操作界面，主要特点包括：
- 标准化审批操作
- 多状态审批流程
- 审批记录追溯
- 移动端适配

## 核心组件

### 1. 基础审批组件 (Audit.vue)
```html
<vol-audit 
  @onAudit="onAudit"
  :data="auditData"
></vol-audit>
```

### 2. 表格审批集成 (ViewGridAudit.vue)
```html
<view-grid-audit
  ref="auditRef"
  @auditClick="saveAudit"
>
  <template #auditContent>
    <!-- 自定义审批内容 -->
  </template>
</view-grid-audit>
```

## 审批状态

### 1. 审批状态定义
| 状态值 | 说明 |
|--------|------|
| 0 | 待审批 |
| 1 | 审批通过 |
| 2 | 审批拒绝 |
| 3 | 驳回修改 |

### 2. 状态流转逻辑
```javascript
auditClick() {
  if (this.auditParam.value == -1) {
    return this.$message.error('请选择审批结果')
  }
  
  // 调用审批API
  let url = `api/${table}/audit?auditReason=${reason}&auditStatus=${status}`
  // 更新审批状态
  this.currentRows.forEach(row => {
    row.auditStatus = status
  })
}
```

## 审批操作流程

### 1. 审批操作步骤
1. 选择审批结果（通过/拒绝）
2. 填写审批意见
3. 提交审批
4. 状态更新和记录

### 2. 审批参数配置
```javascript
auditParam: {
  value: -1,       // 审批结果
  reason: '',      // 审批意见
  data: [          // 审批选项
    { value: 1, label: '通过' },
    { value: 2, label: '拒绝' }
  ],
  showViewButton: true // 显示查看审批记录按钮
}
```

## 审批记录查看

### 1. 审批记录表格
```javascript
columns: [
  { title: '审批人', field: 'auditor' },
  { title: '审批结果', field: 'auditStatus' },
  { title: '审批时间', field: 'auditDate' }
]
```

### 2. 审批流程展示
```html
<div class="step-item" v-for="step in auditSteps">
  <div class="step-text">审批人：{{ step.auditor }}</div>
  <div>状态：{{ getAuditStatus(step.auditStatus) }}</div>
  <div>审批时间：{{ step.auditDate || '待审批' }}</div>
</div>
```

## 使用示例

### 1. 初始化审批组件
```javascript
this.$refs.audit.load(orderId, 'SellOrder')
```

### 2. 自定义审批内容
```html
<vol-audit ref="audit">
  <template #auditContent>
    <div>自定义审批表单内容</div>
  </template>
</vol-audit>
```

## 注意事项
1. 审批状态字段需为`auditStatus`
2. 审批记录需包含`auditor`、`auditDate`字段
3. 移动端需使用`vol-audit`组件
4. 审批拒绝需填写原因
