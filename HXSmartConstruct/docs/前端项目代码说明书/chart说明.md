# 图表组件说明

## 组件概述
图表组件提供数据可视化功能，主要特点包括：
- 支持多种图表类型
- 响应式设计
- 丰富的交互功能
- 自定义样式配置

## 核心组件

### 1. 基础图表 (u-charts)
```html
<qiun-data-charts
  type="column"
  :chartData="chartData"
  :opts="chartOptions"
/>
```

### 2. ECharts集成
```javascript
import * as echarts from 'echarts';
const chart = echarts.init(dom);
chart.setOption(options);
```

## 图表类型

### 1. 支持的图表类型
| 类型 | 说明 | 适用场景 |
|------|------|----------|
| line | 折线图 | 趋势分析 |
| column | 柱状图 | 数据对比 |
| pie | 饼图 | 占比分析 |
| radar | 雷达图 | 多维数据 |
| gauge | 仪表盘 | 进度监控 |

### 2. 多图表组合
```javascript
options = {
  series: [
    {type: 'line', ...},
    {type: 'bar', ...}
  ]
}
```

## 数据配置

### 1. 基本数据格式
```javascript
chartData = {
  categories: ['一月', '二月', '三月'],
  series: [
    {
      name: '销量',
      data: [120, 200, 150]
    }
  ]
}
```

### 2. 多系列数据
```javascript
series: [
  {
    name: '2022',
    data: [120, 132, 101]
  },
  {
    name: '2023', 
    data: [220, 182, 191]
  }
]
```

## 样式配置

### 1. 基础样式
```javascript
opts = {
  color: ['#1890FF', '#13C2C2'],
  padding: [15, 15, 0, 15],
  legend: {
    show: true,
    position: 'bottom'
  }
}
```

### 2. 自定义样式
```javascript
series: [{
  type: 'line',
  smooth: true,
  lineStyle: {
    width: 3,
    shadowColor: 'rgba(0,0,0,0.3)',
    shadowBlur: 10
  }
}]
```

## 交互功能

### 1. 事件监听
```javascript
chart.on('click', params => {
  console.log(params.dataIndex);
});
```

### 2. 数据更新
```javascript
// 更新数据
chart.setOption({
  series: [{
    data: newData
  }]
});

// 重绘图表
chart.resize();
```

## 使用示例

### 1. 基本使用
```javascript
export default {
  data() {
    return {
      chartData: {...},
      chartOptions: {
        title: {text: '销售数据'},
        tooltip: {...}
      }
    }
  }
}
```

### 2. 动态更新
```javascript
fetchData().then(res => {
  this.chartData = processData(res);
  this.$refs.chart.update();
});
```

## 注意事项
1. 大数据量建议使用数据采样
2. 移动端需注意性能优化
3. 复杂交互建议使用ECharts原生API
4. 注意图表容器的宽高设置
