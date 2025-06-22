<!--
 *Author：codesoft
 *Contact：971926469@qq.com
 -->
 <template>
  <div class="project-detail-container">
    <div class="detail-header">
      <h2>{{ projectName }}</h2>
    </div>
    <div class="detail-content">
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="!projectData || Object.keys(projectData).length === 0" class="empty">未找到项目信息</div>
      <div v-else class="detail-info">
        <div class="info-title-row">
          <h3>基本信息</h3>
          <span class="toggle-btn" @click="showBaseInfo = !showBaseInfo">
            <svg :style="{ transform: showBaseInfo ? 'rotate(90deg)' : 'rotate(0deg)' }" width="16" height="16" viewBox="0 0 16 16"><polyline points="4,6 8,10 12,6" fill="none" stroke="#666" stroke-width="2"/></svg>
          </span>
        </div>
        <el-descriptions v-show="showBaseInfo" :column="2" border>
          <el-descriptions-item v-for="(field, index) in detailFields" 
                              :key="index" 
                              :label="field.label">
            {{ projectData[field.key] }}
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </div>
    <div class="project-tabs-panel">
      <el-tabs v-model="activeTab">
        <el-tab-pane v-for="tab in tabs" :key="tab.name" :label="tab.label" :name="tab.name">
          <!-- 方案1：直接在 tab-pane 内渲染组件 -->
          <component 
            v-if="activeTab === tab.name && componentMap[tab.name]" 
            :is="componentMap[tab.name]" 
            :project-id="projectId" 
            :project-data="projectData" 
          />
        </el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, shallowRef, watch } from 'vue';
import { useRoute } from 'vue-router';
import http from '@/api/http.js';

const route = useRoute();
const projectId = ref(route.query.projectId);
const projectName = ref(route.query.projectName);

// 处理 sessionStorage（注意：在某些环境下可能不可用）
let cache = null;
try {
  if (typeof sessionStorage !== 'undefined') {
    cache = sessionStorage.getItem('SC_ProjectInfoDetailRow');
  }
} catch (e) {
  console.warn('sessionStorage 不可用:', e);
}

const projectData = ref(cache ? JSON.parse(cache) : {});
const loading = ref(false);

// 定义要显示的字段
const detailFields = [
  { label: '项目编号', key: 'ProjectID' },
  { label: '项目地址', key: 'Location' },
  { label: '街道', key: 'Street' },
  { label: '社区', key: 'Community' },
  { label: '开始日期', key: 'StartDate' },
  { label: '结束日期', key: 'EndDate' },
  { label: '总投资', key: 'TotalInvestment' },
  { label: '投资性质', key: 'InvestmentType' },
  { label: '项目状态', key: 'ProjectStatus' },
  { label: '项目分类', key: 'ProjectCategory' },
  { label: '所属行业', key: 'Industry' },
  { label: '项目地点', key: 'Address' }
];

// 获取项目详细信息
const loadProjectDetails = async () => {
  if (Object.keys(projectData.value).length > 0) {
    loading.value = false;
    return;
  }
  loading.value = true;
  if (!projectId.value && !projectName.value) {
    loading.value = false;
    return;
  }
  try {
    let data = null;
    if (projectId.value) {
      const wheres = [{ name: 'ProjectID', value: projectId.value }];
      const list = await http.get('/SC_ProjectInfo/', { wheres, page: 1, rows: 1 });
      if (list && list.rows && list.rows.length > 0) {
        data = list.rows[0];
      }
    }
    if ((!data || Object.keys(data).length === 0) && projectName.value) {
      const wheres = [{ name: 'ProjectName', value: projectName.value }];
      const list = await http.get('/SC_ProjectInfo/', { wheres, page: 1, rows: 1 });
      if (list && list.rows && list.rows.length > 0) {
        data = list.rows[0];
      }
    }
    projectData.value = data || {};
  } catch (error) {
    console.error('获取项目详情失败:', error);
  } finally {
    loading.value = false;
  }
};

// Tab配置
const tabs = [
  { label: '项目信息', name: 'SC_ProjectInfo' },
  { label: '参建单位', name: 'SC_Company' },
  { label: '班组信息', name: 'SC_Team' },
  { label: '项目工人', name: 'SC_Worker' },
  { label: '工地考勤', name: 'SC_Attendance' },
  { label: '工地考勤详细', name: 'SC_AttendanceDetail' },
  { label: '人脸抓拍', name: 'SC_FaceCapture' },
  { label: '自动考勤', name: 'SC_AutoAttendance' }
];

const activeTab = ref(tabs[0].name);

// 导入所有子组件
import SC_ProjectInfoTab from './SC_ProjectInfoTab.vue';
import SC_ConTractings from './SC_ConTractings.vue';
import SC_Team from './SC_Team.vue';
import SC_Worker from './SC_Worker.vue';
import SC_Attendance from './SC_Attendance.vue';
import SC_AttendanceDetail from './SC_AttendanceDetail.vue';
import SC_FaceCapture from './SC_FaceCapture.vue';
import SC_AutoAttendance from './SC_AutoAttendance.vue';

// 使用 shallowRef 存储组件映射，避免深度响应
const componentMap = shallowRef({
  SC_ProjectInfo: SC_ProjectInfoTab,
  SC_Company: SC_ConTractings,
  SC_Team: SC_Team,
  SC_Worker: SC_Worker,
  SC_Attendance: SC_Attendance,
  SC_AttendanceDetail: SC_AttendanceDetail,
  SC_FaceCapture: SC_FaceCapture,
  SC_AutoAttendance: SC_AutoAttendance,
});

// 调试：打印当前激活的tab和组件
watch(activeTab, (newTab) => {
  console.log('当前激活的Tab:', newTab);
  console.log('对应的组件:', componentMap.value[newTab]);
}, { immediate: true });

const showBaseInfo = ref(true);

// 监听路由参数变化，动态切换项目详情
watch(
  () => route.query,
  (newQuery) => {
    projectId.value = newQuery.projectId;
    projectName.value = newQuery.projectName;
    let cache = null;
    try {
      if (typeof sessionStorage !== 'undefined') {
        cache = sessionStorage.getItem('SC_ProjectInfoDetailRow');
      }
    } catch (e) {
      console.warn('sessionStorage 不可用:', e);
    }
    projectData.value = cache ? JSON.parse(cache) : {};
    loadProjectDetails();
  }
);

onMounted(() => {
  if (Object.keys(projectData.value).length === 0) {
    loadProjectDetails();
  }
});
</script>

<style lang="less" scoped>
.project-detail-container {
  padding: 20px;
  height: 100%;
  background: #fff;

  .detail-header {
    margin-bottom: 24px;
    padding-bottom: 16px;
    border-bottom: 1px solid #f0f0f0;

    h2 {
      margin: 0;
      color: #1f2f3d;
    }
  }

  .detail-content {
    .loading {
      color: #888;
      font-size: 16px;
      padding: 40px 0;
      text-align: center;
    }
    .empty {
      color: #d9534f;
      font-size: 16px;
      padding: 40px 0;
      text-align: center;
    }
    .detail-info {
      margin-bottom: 32px;

      .info-title-row {
        display: flex;
        align-items: center;
        h3 {
          margin: 0 8px 0 0;
          font-size: 18px;
          font-weight: 500;
          color: #1f2f3d;
        }
        .toggle-btn {
          cursor: pointer;
          display: flex;
          align-items: center;
          user-select: none;
          margin-left: 4px;
          transition: color 0.2s;
          &:hover {
            color: #409eff;
          }
          svg {
            transition: transform 0.2s;
            display: block;
          }
        }
      }
    }
  }
}

.project-tabs-panel {
  margin-top: 32px;
  background: #fff;
  border-radius: 6px;
  box-shadow: 0 2px 8px #f0f1f2;
  padding: 16px 0 0 0;
  
  :deep(.el-tabs__header) {
    display: flex;
    justify-content: center;
  }
  
  :deep(.el-tabs__nav-wrap) {
    display: flex !important;
    justify-content: center !important;
  }
  
  :deep(.el-tabs__nav) {
    font-size: 16px !important;
    font-weight: bold !important;
    display: flex;
    justify-content: center;
    width: auto;
  }
  
  :deep(.el-tabs__content) {
    padding: 24px;
    min-height: 200px;
    background: #fafbfc;
    border-top: 1px solid #f0f0f0;
  }

  :deep(.el-tabs__item) {
    font-size: 16px !important;
    font-weight: bold !important;
    padding: 0 24px !important;
  }
}
</style>