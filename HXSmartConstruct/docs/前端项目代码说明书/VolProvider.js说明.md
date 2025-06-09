# VolProvider组件说明文档

## 组件概述
VolProvider是项目的全局状态管理核心组件，主要功能包括：
- 全局状态存储与管理
- 表单数据处理与转换
- 字典数据管理
- 查询条件构建
- 通用工具方法

## 核心API说明

### 表单数据处理
```js
/**
 * 获取表单值(转换为接口所需格式)
 * @param {Object} formFields 表单字段对象
 * @param {Array} formOptions 表单配置项
 * @return {Object} 接口所需格式的表单值
 */
getFormValues(formFields, formOptions)

/**
 * 重置表单值
 * @param {Object} formFields 表单字段对象 
 * @param {Array} formOptions 表单配置项
 * @param {Object} data 重置数据(可选)
 */
resetForm(formFields, formOptions, data)

/**
 * 设置表单字段值
 * @param {Object} formFields 表单字段对象
 * @param {Array} formOptions 表单配置项 
 * @param {String} field 字段名
 * @param {Object} data 数据源
 */
setFormValue(formFields, formOptions, field, data)
```

### 字典数据管理
```js
/**
 * 获取表单字典配置
 * @param {Array} formOptions 表单配置项
 * @param {String} field 字段名
 * @return {Array} 字典数据
 */
getFormDicData(formOptions, field)

/**
 * 获取表格字典配置 
 * @param {Array} columns 表格列配置
 * @param {String} field 字段名
 * @return {Array} 字典数据
 */
getColumnDicData(columns, field)
```

### 状态管理
```js
/**
 * 设置全局状态值
 * @param {String} key 键名
 * @param {Any} value 值
 */
setItem(key, value)

/**
 * 获取全局状态值
 * @param {String} key 键名
 * @return {Any} 存储的值
 */ 
getItem(key)
```

## 使用示例

### 表单提交
```js
import VolProvider from '@/components/VolProvider/VolProvider'

const submit = () => {
  const formData = VolProvider.getFormValues(formFields, formOptions)
  // 调用接口提交formData
}
```

### 字典数据获取
```js
// 获取表单字段字典
const statusDic = VolProvider.getFormDicData(formOptions, 'status')

// 获取表格列字典
const genderDic = VolProvider.getColumnDicData(columns, 'gender')
```

## 注意事项
1. 表单字段值为数组时会被自动转换为逗号分隔字符串
2. 字典数据建议在页面初始化时加载
3. 全局状态存储在Vuex中，刷新页面会丢失
