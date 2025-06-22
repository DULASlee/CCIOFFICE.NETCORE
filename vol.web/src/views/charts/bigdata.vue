<template>
  <div class="bigdata-container">
    <!-- 头部 -->
    <header class="header">
      <div class="header-tabs">
        <div v-for="tab in tabs" :key="tab.id"
             :class="['tab-item', { active: activeTab === tab.id }]"
             @click="switchTab(tab.id)">
          {{ tab.name }}
        </div>
      </div>
      <h1 class="header-title">智慧工地驾驶舱</h1>
      <div class="header-time">{{ currentTime }} 星期{{ weekDay }}</div>
    </header>

    <!-- 主体 -->
    <main class="main-container">
      <!-- 左侧面板 -->
      <aside class="left-panel">
        <!-- 企业综合数据 -->
        <section class="module">
          <div class="enterprise-header">
            <div class="enterprise-icon">🏢</div>
            <div class="enterprise-title">企业综合数据</div>
          </div>
          <div class="stat-grid">
            <div v-for="stat in enterpriseStats" :key="stat.label" class="stat-card">
              <div class="stat-value">{{ stat.value }}<span class="stat-unit">{{ stat.unit }}</span></div>
              <div class="stat-label">{{ stat.label }}</div>
            </div>
          </div>
          <div class="person-stats">
            <div v-for="person in personStats" :key="person.label" class="person-card">
              <div class="person-icon">{{ person.icon }}</div>
              <div class="person-label">{{ person.label }}</div>
              <div class="person-value">{{ person.value }}</div>
            </div>
          </div>
        </section>

        <!-- 今日出勤和合同签署 -->
        <div class="attendance-contract-row">
          <!-- 今日出勤 -->
          <section class="attendance-module">
            <div class="module-header">今日出勤</div>
            <div class="contract-section">
              <div class="contract-stats">
                <div class="contract-item">
                  <span class="contract-label">出勤人数</span>
                  <span class="contract-value">94,068人</span>
                </div>
                <div class="contract-item">
                  <span class="contract-label">出勤率</span>
                  <span class="contract-value">98.5%</span>
                </div>
              </div>
              <div class="contract-circle" style="animation: none;">
                <div class="circle-value">98.5%</div>
                <div class="circle-label">出勤率</div>
              </div>
            </div>
          </section>

          <!-- 合同签署 -->
          <section class="contract-module">
            <div class="module-header">合同签署</div>
            <div class="contract-section">
              <div class="contract-stats">
                <div class="contract-item">
                  <span class="contract-label">签署人数</span>
                  <span class="contract-value">91,236人</span>
                </div>
                <div class="contract-item">
                  <span class="contract-label">签署率</span>
                  <span class="contract-value">97%</span>
                </div>
              </div>
              <div class="contract-circle">
                <div class="circle-value">97%</div>
                <div class="circle-label">签署率</div>
              </div>
            </div>
          </section>
        </div>

        <!-- 安全管理和质量管理 -->
        <div class="management-row">
          <section class="safety-module">
            <div class="module-header">安全管理</div>
            <div class="ring-chart-container">
              <div class="ring-chart-item">
                <div ref="safetyChart1El" class="ring-chart"></div>
                <div class="ring-label">孟加拉湾</div>
              </div>
            </div>
          </section>

          <section class="quality-module">
            <div class="module-header">质量管理</div>
            <div class="ring-chart-container">
              <div class="ring-chart-item">
                <div ref="safetyChart2El" class="ring-chart"></div>
                <div class="ring-label">皮达曼湾</div>
              </div>
            </div>
          </section>
        </div>
      </aside>

      <!-- 中间面板 -->
      <div class="center-panel">
        <!-- 地图 -->
        <div class="map-wrapper">
          <div class="map-container">
            <div ref="mapChartEl" id="mapChart"></div>
          </div>
        </div>

        <!-- 最近三十天考勤统计 -->
        <div class="module attendance-wrapper">
          <div class="module-header">最近三十天考勤统计</div>
          <div ref="attendanceChartEl" class="chart-container"></div>
        </div>
      </div>

      <!-- 右侧面板 -->
      <aside class="right-panel">
        <!-- 智慧工地开通项目数 -->
        <section class="module" style="height: 280px;">
          <div class="module-header">智慧工地开通项目数</div>
          <div class="smart-grid">
            <div v-for="item in smartItems" :key="item.label" class="smart-item">
              <div class="smart-icon">{{ item.icon }}</div>
              <div class="smart-content">
                <div v-if="item.value" class="smart-value">{{ item.value }}</div>
                <div class="smart-label">{{ item.label }}</div>
              </div>
            </div>
          </div>
        </section>

        <!-- 最近三十天工地预警统计 -->
        <section class="module" style="height: 230px;">
          <div class="module-header">最近三十天工地预警统计</div>
          <div class="warning-content">
            <div ref="warningChartEl" class="warning-chart"></div>
            <div class="warning-legend">
              <div v-for="item in warningLegend" :key="item.name" class="legend-item">
                <span class="legend-dot" :style="{backgroundColor: item.color}"></span>
                <span>{{ item.name }}</span>
                <span style="margin-left: auto; color: #00d4ff;">{{ item.value }}%</span>
              </div>
            </div>
          </div>
        </section>

        <!-- 今日人员预警 -->
        <section class="module" style="flex: 1;">
          <div class="module-header">今日人员预警</div>
          <div class="warning-grid">
            <div v-for="warning in todayWarnings" :key="warning.label" class="warning-card">
              <div class="warning-icon">{{ warning.icon }}</div>
              <div class="warning-count">{{ warning.count }} 次</div>
              <div class="warning-text">{{ warning.label }}</div>
            </div>
          </div>
        </section>
      </aside>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import * as echarts from 'echarts';

// 响应式数据
const currentTime = ref('');
const weekDay = ref('');
const timer = ref(null);
const charts = ref({});
const activeTab = ref('production');

// Tab标签数据
const tabs = ref([
    { id: 'production', name: '生产安全' },
    { id: 'equipment', name: '设备检测' },
    { id: 'attendance', name: '考勤管理' },
    { id: 'statistics', name: '数据统计' }
]);

// 企业综合数据
const enterpriseStats = ref([
    { label: '项目总数', value: '1732', unit: '个' },
    { label: '客户', value: '281', unit: '个' },
    { label: '完工/竣工', value: '1053', unit: '个' },
    { label: '立项', value: '714', unit: '个' },
    { label: '在建', value: '5020', unit: '个' },
    { label: '停工', value: '64', unit: '个' }
]);

// 人员统计
const personStats = ref([
    { icon: '👷', label: '累计实名登记', value: '3060937' },
    { icon: '👨‍💼', label: '在岗人员', value: '133308' },
    { icon: '👔', label: '管理人员', value: '11471' },
    { icon: '🏛️', label: '党员人数', value: '4223' }
]);

// 智慧工地项目 - 18个项目
const smartItems = ref([
    { icon: '📱', value: '1664', label: '考勤机' },
    { icon: '🏗️', value: '84', label: '塔吊监测' },
    { icon: '🏢', value: '11', label: '升降机监测' },
    { icon: '🎥', value: '279', label: '视频监控' },
    { icon: '🤖', value: '19', label: 'AI识别' },
    { icon: '🌡️', value: '220', label: '环境监测' },
    { icon: '💧', value: '125', label: '智能水电' },
    { icon: '⛑️', value: '58', label: '智能安全帽' },
    { icon: '🗺️', value: '23', label: '无人机全景' },
    { icon: '🚧', value: '86', label: '全景模拟' },
    { icon: '⚙️', value: '', label: '疫情防控' },
    { icon: '🛡️', value: '40', label: '安全监测' },
    { icon: '📦', value: '82', label: '质量监测' },
    { icon: '🏗️', value: '0', label: '高支模' },
    { icon: '🌫️', value: '31', label: '深基坑' },
    { icon: '🗑️', value: '', label: '智能烟感' },
    { icon: '💡', value: '', label: 'LED屏' },
    { icon: '🚗', value: '78', label: '车辆冲洗' }
]);

// 预警图例
const warningLegend = ref([
    { name: 'AI识别预警', value: 10, color: '#8b5cf6' },
    { name: '塔吊监测预警', value: 45, color: '#3b82f6' },
    { name: '升降机预警', value: 8, color: '#10b981' },
    { name: '环境监测预警', value: 15, color: '#f59e0b' },
    { name: '质量监测预警', value: 12, color: '#ef4444' },
    { name: '安全监测预警', value: 10, color: '#ec4899' }
]);

// 今日人员预警
const todayWarnings = ref([
    { icon: '⚠️', count: 114, label: '管理人员出勤预警' },
    { icon: '📵', count: 29, label: '手机定位关闭预警' },
    { icon: '💻', count: 933, label: '手机进程终止预警' },
    { icon: '🚫', count: 12, label: '人证不相符预警' }
]);

// DOM引用
const mapChartEl = ref(null);
const attendanceChartEl = ref(null);
const warningChartEl = ref(null);
const safetyChart1El = ref(null);
const safetyChart2El = ref(null);

// 切换Tab
const switchTab = (tabId) => {
    activeTab.value = tabId;
    console.log('切换到:', tabId);
};

// 更新时间
const updateTime = () => {
    const now = new Date();
    const weeks = ['日', '一', '二', '三', '四', '五', '六'];
    currentTime.value = now.toLocaleString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
    weekDay.value = weeks[now.getDay()];
};

// 初始化地图
const initMapChart = () => {
    if (!mapChartEl.value) return;
    const chartInstance = echarts.init(mapChartEl.value);
    charts.value.map = chartInstance;

    const zhejiangCities = [];
    for (let i = 0; i < 50; i++) {
        const lng = 118.0 + Math.random() * 4;
        const lat = 27.5 + Math.random() * 3.5;
        const value = 50 + Math.random() * 100;
        zhejiangCities.push([lng, lat, value]);
    }

    const option = {
        backgroundColor: 'transparent',
        geo: {
            map: 'china',
            roam: true,
            center: [120.19, 29.08],
            zoom: 7,
            label: { show: false },
            itemStyle: {
                areaColor: 'rgba(0, 10, 52, 0.8)',
                borderColor: 'rgba(0, 132, 255, 0.5)',
                borderWidth: 1,
                shadowColor: 'rgba(0, 132, 255, 0.3)',
                shadowBlur: 8
            },
            emphasis: {
                itemStyle: {
                    areaColor: 'rgba(0, 50, 120, 0.9)',
                    borderColor: '#00d4ff',
                    borderWidth: 2,
                    shadowColor: 'rgba(0, 212, 255, 0.5)',
                    shadowBlur: 15
                }
            }
        },
        series: [{
            type: 'effectScatter',
            coordinateSystem: 'geo',
            data: zhejiangCities.map((item, index) => ({ name: `项目${index + 1}`, value: item })),
            symbolSize: val => Math.max(Math.min(val[2] / 10, 20), 5),
            showEffectOn: 'render',
            rippleEffect: { brushType: 'stroke', scale: 3, period: 4 },
            itemStyle: { color: '#00d4ff', shadowBlur: 10, shadowColor: '#00d4ff' },
            zlevel: 1
        }, {
            type: 'scatter',
            coordinateSystem: 'geo',
            data: zhejiangCities.map((item, index) => ({ name: `项目${index + 1}`, value: item })),
            symbolSize: val => Math.max(Math.min(val[2] / 20, 10), 3),
            itemStyle: { color: '#00ffcc', shadowBlur: 5, shadowColor: '#00ffcc' },
            zlevel: 2
        }]
    };

    fetch('https://geo.datav.aliyun.com/areas_v3/bound/100000_full.json')
        .then(response => response.json())
        .then(chinaJson => {
            echarts.registerMap('china', chinaJson);
            chartInstance.setOption(option);
        })
        .catch(() => console.error('地图加载失败'));
};

// 初始化考勤统计
const initAttendanceChart = () => {
    if (!attendanceChartEl.value) return;
    const chartInstance = echarts.init(attendanceChartEl.value);
    charts.value.attendance = chartInstance;

    const dates = [];
    const values = [];
    for (let i = 0; i < 30; i++) {
        const date = new Date();
        date.setDate(date.getDate() - 29 + i);
        dates.push(`${date.getMonth() + 1}-${date.getDate()}`);
        const baseValue = 120000;
        const dayOfWeek = date.getDay();
        let value = (dayOfWeek === 0 || dayOfWeek === 6) ? baseValue * 0.5 + Math.random() * 10000 : baseValue + Math.random() * 20000 - 10000;
        values.push(Math.floor(value));
    }

    const option = {
        backgroundColor: 'transparent',
        grid: { top: 10, left: 50, right: 20, bottom: 30 },
        tooltip: { trigger: 'axis', backgroundColor: 'rgba(0, 20, 50, 0.8)', borderColor: '#00d4ff', textStyle: { color: '#fff' } },
        xAxis: { type: 'category', data: dates, axisLine: { lineStyle: { color: '#0084ff' } }, axisLabel: { color: '#8899aa', interval: 4, rotate: 30, fontSize: 10 } },
        yAxis: { type: 'value', min: 50000, max: 150000, interval: 25000, axisLine: { lineStyle: { color: '#0084ff' } }, axisLabel: { color: '#8899aa', formatter: value => (value / 10000).toFixed(0) + '万', fontSize: 10 }, splitLine: { lineStyle: { color: 'rgba(0, 132, 255, 0.1)', type: 'dashed' } } },
        series: [{
            type: 'line',
            data: values,
            smooth: true,
            lineStyle: { color: '#00ff88', width: 3, shadowBlur: 10, shadowColor: '#00ff88' },
            areaStyle: { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: 'rgba(0, 255, 136, 0.3)' }, { offset: 1, color: 'rgba(0, 255, 136, 0.0)' }]) },
            itemStyle: { color: '#00ff88', borderWidth: 2, borderColor: '#fff' }
        }]
    };
    chartInstance.setOption(option);
};

// 初始化预警统计
const initWarningChart = () => {
    if (!warningChartEl.value) return;
    const chartInstance = echarts.init(warningChartEl.value);
    charts.value.warning = chartInstance;

    const option = {
        backgroundColor: 'transparent',
        tooltip: { trigger: 'item', backgroundColor: 'rgba(0, 20, 50, 0.8)', borderColor: '#00d4ff', textStyle: { color: '#fff' } },
        series: [{
            type: 'pie',
            radius: ['45%', '75%'],
            center: ['50%', '50%'],
            avoidLabelOverlap: false,
            label: { show: false },
            labelLine: { show: false },
            data: warningLegend.value.map(item => ({
                value: item.value,
                name: item.name,
                itemStyle: { color: item.color, shadowBlur: 10, shadowColor: item.color }
            }))
        }],
        graphic: [{
            type: 'text',
            left: 'center',
            top: '45%',
            style: { text: '23万次', textAlign: 'center', fill: '#fff', fontSize: 20, fontWeight: 'bold' }
        }, {
            type: 'text',
            left: 'center',
            top: '55%',
            style: { text: '总预警', textAlign: 'center', fill: '#8899aa', fontSize: 12 }
        }]
    };
    chartInstance.setOption(option);
};

// 初始化安全管理图表（环形图）
const initSafetyChart1 = () => {
    if (!safetyChart1El.value) return;
    const chartInstance = echarts.init(safetyChart1El.value);
    charts.value.safety1 = chartInstance;

    const option = {
        backgroundColor: 'transparent',
        series: [{
            type: 'pie',
            radius: ['60%', '85%'],
            center: ['50%', '50%'],
            startAngle: 90,
            avoidLabelOverlap: false,
            label: { show: false },
            labelLine: { show: false },
            data: [
                { value: 34, name: '待整改', itemStyle: { color: '#00d4ff' } },
                { value: 80, name: '已整改', itemStyle: { color: '#10b981' } }
            ]
        }],
        graphic: [{
            type: 'text',
            left: 'center',
            top: 'center',
            style: { text: '待整改', textAlign: 'center', fill: '#8899aa', fontSize: 12 }
        }]
    };
    chartInstance.setOption(option);
};

// 初始化质量管理图表（环形图）
const initSafetyChart2 = () => {
    if (!safetyChart2El.value) return;
    const chartInstance = echarts.init(safetyChart2El.value);
    charts.value.safety2 = chartInstance;

    const option = {
        backgroundColor: 'transparent',
        series: [{
            type: 'pie',
            radius: ['60%', '85%'],
            center: ['50%', '50%'],
            avoidLabelOverlap: false,
            label: { show: false },
            labelLine: { show: false },
            data: [
                { value: 10, name: '无风险', itemStyle: { color: '#10b981' } },
                { value: 60, name: '低风险', itemStyle: { color: '#3b82f6' } },
                { value: 20, name: '较大风险', itemStyle: { color: '#f59e0b' } },
                { value: 10, name: '重大风险', itemStyle: { color: '#ef4444' } }
            ]
        }],
        graphic: [{
            type: 'text',
            left: 'center',
            top: 'center',
            style: { text: '已整改', textAlign: 'center', fill: '#8899aa', fontSize: 12 }
        }]
    };
    chartInstance.setOption(option);
};

// 初始化所有图表
const initAllCharts = () => {
    initMapChart();
    initAttendanceChart();
    initWarningChart();
    initSafetyChart1();
    initSafetyChart2();
};

// 窗口大小调整
const handleResize = () => {
    Object.values(charts.value).forEach(chart => {
        if (chart) chart.resize();
    });
};

// 生命周期
onMounted(() => {
    updateTime();
    timer.value = setInterval(updateTime, 1000);

    const container = document.querySelector('.bigdata-container');
    const setContainerSize = () => {
        if (container) {
          container.style.height = window.innerHeight + 'px';
          container.style.width = window.innerWidth + 'px';
        }
    };

    setContainerSize();
    window.addEventListener('resize', () => {
        setContainerSize();
        handleResize();
    });

    initAllCharts();
});

onUnmounted(() => {
    clearInterval(timer.value);
    window.removeEventListener('resize', handleResize);
    Object.values(charts.value).forEach(chart => {
        if (chart) chart.dispose();
    });
});
</script>

<style scoped>
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: 'Microsoft YaHei', Arial, sans-serif;
    background: #030829;
    color: #fff;
    overflow: hidden;
    width: 100vw;
    height: 100vh;
}

.bigdata-container {
    width: 100%;
    height: 100%;
    background: linear-gradient(135deg, #0a1a3e 0%, #030829 100%);
    position: relative;
    display: flex;
    flex-direction: column;
}

/* 头部样式 */
.header {
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 30px;
    position: relative;
    background: rgba(0, 20, 50, 0.5);
    border-bottom: 1px solid rgba(0, 212, 255, 0.3);
}

/* Tab标签样式 */
.header-tabs {
    display: flex;
    gap: 20px;
    align-items: center;
}

.tab-item {
    padding: 8px 20px;
    background: rgba(0, 132, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.3s ease;
    font-size: 14px;
    color: #00d4ff;
}

.tab-item:hover {
    background: rgba(0, 212, 255, 0.2);
    transform: translateY(-2px);
}

.tab-item.active {
    background: rgba(0, 212, 255, 0.3);
    border-color: #00d4ff;
    box-shadow: 0 0 10px rgba(0, 212, 255, 0.5);
}

.header-title {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    font-size: 32px;
    font-weight: bold;
    color: #00d4ff;
    text-shadow: 0 0 20px rgba(0, 212, 255, 0.5);
    letter-spacing: 5px;
}

.header-time {
    color: #fff;
    font-size: 14px;
}

/* 主体布局 */
.main-container {
    display: flex;
    flex: 1;
    padding: 10px 20px 20px;
    gap: 20px;
    overflow: hidden;
}

.left-panel,
.right-panel {
    width: 380px;
    display: flex;
    flex-direction: column;
    gap: 15px;
}

.center-panel {
    flex: 1;
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 15px;
}

/* 模块样式 */
.module {
    background: rgba(0, 20, 50, 0.3);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    padding: 15px;
    position: relative;
    display: flex;
    flex-direction: column;
    backdrop-filter: blur(5px);
}

.module::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent, #00d4ff, transparent);
    animation: scan-line 3s ease-in-out infinite;
}

@keyframes scan-line {
    0%, 100% { opacity: 0; }
    50% { opacity: 1; }
}

.module-header {
    display: flex;
    align-items: center;
    margin-bottom: 15px;
    font-size: 16px;
    color: #00d4ff;
}

.module-header::before {
    content: '◆';
    color: #00d4ff;
    margin-right: 8px;
    font-size: 12px;
}

.enterprise-header {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 15px;
}

.enterprise-icon {
    width: 40px;
    height: 40px;
    background: linear-gradient(135deg, rgba(0, 212, 255, 0.3), rgba(0, 132, 255, 0.3));
    border-radius: 4px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
    0%, 100% { transform: scale(1); }
    50% { transform: scale(1.05); }
}

.enterprise-title {
    font-size: 16px;
    color: #00d4ff;
}

.stat-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 10px;
}

.stat-card {
    background: linear-gradient(135deg, rgba(0, 40, 80, 0.4), rgba(0, 60, 120, 0.3));
    border: 1px solid rgba(0, 212, 255, 0.2);
    border-radius: 4px;
    padding: 10px 5px;
    text-align: center;
    transition: all 0.3s ease;
    position: relative;
    overflow: hidden;
}

.stat-card::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: 2px;
    background: linear-gradient(90deg, transparent, #00d4ff, transparent);
    transform: translateX(-100%);
    animation: slide 3s linear infinite;
}

@keyframes slide {
    to { transform: translateX(100%); }
}

.stat-card:hover {
    background: rgba(0, 212, 255, 0.1);
    transform: translateY(-2px);
    box-shadow: 0 5px 15px rgba(0, 212, 255, 0.3);
}

.stat-value {
    font-size: 20px;
    font-weight: bold;
    color: #00d4ff;
    margin-bottom: 3px;
    text-shadow: 0 0 10px rgba(0, 212, 255, 0.5);
}

.stat-label {
    font-size: 11px;
    color: #8899aa;
}

.stat-unit {
    font-size: 12px;
    color: #8899aa;
}

.person-stats {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 10px;
    margin-top: 10px;
}

.person-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 8px;
    background: rgba(0, 40, 80, 0.3);
    border-radius: 4px;
    border: 1px solid rgba(0, 212, 255, 0.2);
    transition: all 0.3s ease;
}

.person-card:hover {
    background: rgba(0, 212, 255, 0.1);
    transform: scale(1.05);
}

.person-icon {
    width: 35px;
    height: 35px;
    background: linear-gradient(135deg, rgba(0, 212, 255, 0.3), rgba(0, 255, 200, 0.3));
    border-radius: 50%;
    margin-bottom: 5px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;
}

.person-label {
    font-size: 10px;
    color: #8899aa;
    margin-bottom: 3px;
}

.person-value {
    font-size: 16px;
    font-weight: bold;
    color: #00ffcc;
    text-shadow: 0 0 8px rgba(0, 255, 200, 0.5);
}

.attendance-contract-row {
    display: flex;
    gap: 15px;
}

.attendance-module,
.contract-module {
    flex: 1;
    background: rgba(0, 20, 50, 0.3);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    padding: 15px;
    position: relative;
    backdrop-filter: blur(5px);
}

.contract-section {
    display: flex;
    gap: 15px;
    align-items: center;
}

.contract-stats {
    flex: 1;
}

.contract-item {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    font-size: 14px;
}

.contract-label {
    color: #8899aa;
}

.contract-value {
    color: #00d4ff;
    font-weight: bold;
}

.contract-circle {
    width: 80px;
    height: 80px;
    background: linear-gradient(135deg, #0084ff, #00d4ff);
    border-radius: 50%;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 30px rgba(0, 212, 255, 0.5);
    animation: rotate 3s linear infinite;
}

@keyframes rotate {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
}

.circle-value {
    font-size: 20px;
    font-weight: bold;
    color: #fff;
}

.circle-label {
    font-size: 11px;
    color: rgba(255, 255, 255, 0.9);
}

.management-row {
    display: flex;
    gap: 15px;
    flex: 1;
}

.safety-module,
.quality-module {
    flex: 1;
    background: rgba(0, 20, 50, 0.3);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    padding: 15px;
    position: relative;
    backdrop-filter: blur(5px);
    display: flex;
    flex-direction: column;
}

.ring-chart-container {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    gap: 20px;
}

.ring-chart-item {
    text-align: center;
}

.ring-chart {
    width: 120px;
    height: 120px;
}

.ring-label {
    margin-top: 10px;
    font-size: 14px;
    color: #00d4ff;
}

.map-wrapper {
    flex: 1;
    background: rgba(0, 20, 50, 0.3);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    position: relative;
    overflow: hidden;
    min-height: 400px;
}

.map-container, #mapChart {
    width: 100%;
    height: 100%;
    position: relative;
}

.attendance-wrapper {
    height: 220px;
    background: rgba(0, 20, 50, 0.3);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 4px;
    padding: 15px;
    backdrop-filter: blur(5px);
    position: relative;
    display: flex;
    flex-direction: column;
}

.attendance-wrapper .chart-container {
    flex: 1;
    width: 100%;
    min-height: 150px;
}

.smart-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
    height: 215px;
    overflow-y: auto;
    padding-right: 5px;
}

.smart-item {
    background: linear-gradient(135deg, rgba(0, 40, 80, 0.4), rgba(0, 60, 120, 0.3));
    border: 1px solid rgba(0, 212, 255, 0.2);
    border-radius: 4px;
    padding: 5px;
    cursor: pointer;
    transition: all 0.3s ease;
    text-align: center;
    height: 60px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
}

.smart-item:hover {
    background: rgba(0, 212, 255, 0.1);
    transform: scale(1.05);
    box-shadow: 0 4px 12px rgba(0, 212, 255, 0.4);
}

.smart-icon {
    font-size: 18px;
    color: #00d4ff;
    margin-bottom: 3px;
}

.smart-content {
    text-align: center;
}

.smart-value {
    font-size: 14px;
    color: #00d4ff;
    font-weight: bold;
    margin-bottom: 1px;
}

.smart-label {
    font-size: 10px;
    color: #8899aa;
}

.warning-content {
    display: flex;
    gap: 15px;
    height: 160px;
}

.warning-chart {
    flex: 1;
    min-width: 0;
}

.warning-legend {
    width: 140px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    font-size: 11px;
    overflow-y: auto;
    padding-right: 5px;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 5px;
    padding: 3px;
    background: rgba(0, 40, 80, 0.2);
    border-radius: 4px;
    transition: all 0.3s ease;
    font-size: 10px;
}

.legend-item:hover {
    background: rgba(0, 212, 255, 0.1);
}

.legend-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    box-shadow: 0 0 5px;
}

.warning-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 10px;
}

.warning-card {
    background: linear-gradient(135deg, rgba(0, 40, 80, 0.4), rgba(0, 60, 120, 0.3));
    border: 1px solid rgba(0, 212, 255, 0.2);
    border-radius: 4px;
    padding: 10px;
    text-align: center;
    transition: all 0.3s ease;
    cursor: pointer;
}

.warning-card:hover {
    background: rgba(0, 212, 255, 0.1);
    transform: translateY(-3px);
    box-shadow: 0 5px 15px rgba(0, 212, 255, 0.4);
}

.warning-icon {
    width: 40px;
    height: 40px;
    margin: 0 auto 5px;
    background: linear-gradient(135deg, rgba(255, 100, 100, 0.3), rgba(255, 50, 50, 0.2));
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    animation: pulse-warning 2s ease-in-out infinite;
}

@keyframes pulse-warning {
    0%, 100% {
        transform: scale(1);
        box-shadow: 0 0 0 0 rgba(255, 100, 100, 0.7);
    }
    50% {
        transform: scale(1.05);
        box-shadow: 0 0 0 10px rgba(255, 100, 100, 0);
    }
}

.warning-count {
    font-size: 18px;
    font-weight: bold;
    color: #ff6b6b;
    margin: 3px 0;
    text-shadow: 0 0 10px rgba(255, 100, 100, 0.5);
}

.warning-text {
    font-size: 11px;
    color: #8899aa;
}

.chart-container {
    flex: 1;
    width: 100%;
}

::-webkit-scrollbar {
    width: 6px;
    height: 6px;
}

::-webkit-scrollbar-track {
    background: rgba(0, 20, 50, 0.3);
}

::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.3);
    border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
    background: rgba(0, 212, 255, 0.5);
}
</style>