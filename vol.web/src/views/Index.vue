<template>
  <!-- 模板部分完全保持不变 -->
  <vol-loading v-if="!permissionInited" center></vol-loading>
  <div
    id="vol-container"
    :class="['vol-theme-' + theme, layoutIsLeft() ? 'vol-layout-left' : '']"
    v-if="permissionInited"
  >
    <div class="vol-aside" :style="{ width: (isCollapse ? 63 : 200) + 'px' }">
      <div class="header">
        <div class="vol-aside-project-name" style="display: flex; align-items: center; position: relative;">
          <span class="logo-text">HXAloTOS</span>
          <span class="menu-toggle-btn" @click="toggleLeft" title="展开/收起菜单"
            :class="{ 'menu-toggle-fixed': isCollapse }">
            <svg width="20" height="20" viewBox="0 0 20 20" style="margin-left: 6px; cursor: pointer;"><rect y="3" width="20" height="2" rx="1" fill="#444"/><rect y="8.5" width="20" height="2" rx="1" fill="#444"/><rect y="14" width="20" height="2" rx="1" fill="#444"/></svg>
          </span>
        </div>
        <!-- 这里可以改为logo显示 -->
        <!-- <img  src="@/assets/imgs/logo.png" /> -->
      </div>
      <div class="vol-menu">
        <el-scrollbar style="height: 100%; flex: 1">
          <VolMenu
            :currentMenuId="currentMenuId"
            :on-select="onSelect"
            :enable="true"
            :open-select="false"
            :isCollapse="isCollapse"
            :list="menuData"
          ></VolMenu>
        </el-scrollbar>
      </div>
    </div>
    <div class="vol-container">
      <div class="vol-header">
        <!-- 这里可以放项目名称 -->
        <!-- <div class="project-name">xx管理平台</div> -->
        <div class="header-text">
         
          <div class="h-link" v-if="layout == 'top'">
            <a
              :class="[navCurrentMenuId === item.id ? 'h-link-a-acitve' : '']"
              @click="menuDataClick(item, index)"
              v-for="(item, index) in navMenuList"
              :key="index"
            >
              <i :class="item.icon"></i> <span> {{ $ts(item.name) }}</span>
            </a>
          </div>
          <div class="h-link">   
            <a @click="handleTopLinkClick(item)" v-for="(item, index) in links" :key="index">
              <i :class="item.icon"></i> <span> {{ item.text }}</span>
            </a>
          </div>
        </div>
        <div class="header-info">
          <!-- <div class="h-link" style="margin-right: 10px">
            <lang :color="color"></lang>
          </div> -->
          <div class="h-link">
            <vol-menu-filter :on-select="onSelect"></vol-menu-filter>
          </div>
          <div class="h-link h-link-icons">
            <a
              v-for="(item, index) in icons"
              @click="linkClick(item)"
              :key="index"
              :class="item.icon"
            ></a>
            <!-- <a><i class="el-icon-message-solid"></i></a> -->
          </div>
          
          <!--消息管理-->
          <div class="h-link">
            <message :list="messageList"></message>
            <!-- <a><i class="el-icon-message-solid"></i></a> -->
          </div>
    
          <div class="user-header-info">
            <el-dropdown trigger="hover">
              <div class="user-header-content">
                <img class="user-header-img" :src="userInfo.img" @error="getErrorImg" />
                <div class="user-header-content-right">
                  <div class="user-name">
                    {{ userInfo.name }}<i class="el-icon-arrow-down user-name-drop-icon"></i>
                  </div>
                  <div id="index-date" class="index-date">{{ indexDate }}</div>
                </div>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item v-for="(item, index) in userDropItems" :key="index">
                    <div @click="linkClick(item)">
                      <i :class="item.icon"></i> {{ $ts(item.text) }}
                    </div>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>
      </div>
      <div class="vol-path">
        <el-tabs
          @tab-click="selectNav"
          @tab-remove="removeNav"
          type="border-card"
          class="header-navigation"
          v-model="selectId"
          :strtch="false"
        >
          <el-tab-pane
            v-for="(item, navIndex) in navigation"
            type="card"
            :name="navIndex + ''"
            :closable="navIndex > 0"
            :key="navIndex"
            :label="$ts(item.name)"
          >
            <span style="display: none">{{ navIndex }}</span>
          </el-tab-pane>
        </el-tabs>
        <!-- 右键菜单 -->
        <div v-show="contextMenuVisible">
          <ul :style="{ left: menuLeft + 'px', top: menuTop + 'px' }" class="contextMenu">
            <li v-show="visibleItem.left">
              <el-button link @click="navCloseTabs('left')"
                ><i class="el-icon-back"></i>{{ $ts('关闭左边') }}</el-button
              >
            </li>
            <li v-show="visibleItem.right">
              <el-button link @click="navCloseTabs('right')">
                <i class="el-icon-right"></i>{{ $ts('关闭右边') }}</el-button
              >
            </li>
            <li v-show="visibleItem.other">
              <el-button link @click="navCloseTabs('other')"
                ><i class="el-icon-right"></i>{{ $ts('关闭其他') }}
              </el-button>
            </li>
            <li>
              <el-button link @click="navRefreshPage"
                ><i class="el-icon-refresh"></i>{{ $ts('刷新页面') }}
              </el-button>
            </li>
          </ul>
        </div>
      </div>
      <div class="vol-main" id="vol-main">
        <el-scrollbar style="height: 100%">
          <index-router-view></index-router-view>
        </el-scrollbar>
      </div>
    </div>
    <el-drawer
      :title="$ts('基础设置')"
      size="360px"
      v-model="drawer_model"
      direction="rtl"
      destroy-on-close
    >
      <home-setting @layoutChange="layoutChange"></home-setting>
    </el-drawer>
  </div>
</template>

<style lang="less" scoped>
@import './index/index.less';
@import './index/aside.less';
.logo-text {
  font-family: 'Segoe UI', 'Arial Rounded MT Bold', 'Arial', 'sans-serif';
  font-size: 1.3em;
  font-weight: 700;
  letter-spacing: 2px;
  color: #1a73e8;
  text-shadow: 0 2px 8px #e3eaff;
  user-select: none;
  font-style: italic;
}
.menu-toggle-btn {
  display: flex;
  align-items: center;
  margin-left: 6px;
  cursor: pointer;
  transition: color 0.2s;
  z-index: 1002;
}
.menu-toggle-btn:hover svg rect {
  fill: #1a73e8;
}
.menu-toggle-fixed {
  position: absolute;
  left: 8px;
  top: 8px;
  background: linear-gradient(135deg, #e3eaff 0%, #f6faff 100%);
  border-radius: 50%;
  box-shadow: 0 4px 16px #e3eaff;
  padding: 2px;
  width: 32px;
  height: 32px;
  justify-content: center;
  align-items: center;
  border: 1.5px solid #e0e7ef;
  transition: box-shadow 0.2s, background 0.2s;
}
.menu-toggle-fixed:hover {
  background: linear-gradient(135deg, #d0e6ff 0%, #e6f0fa 100%);
  box-shadow: 0 6px 20px #b3d8fd;
}
.menu-toggle-fixed svg {
  margin-left: 0 !important;
}
.menu-toggle-fixed svg rect {
  fill: #1a73e8;
  transition: fill 0.2s;
}
.menu-toggle-fixed:hover svg rect {
  fill: #409eff;
}
</style>

<script setup>
// ========== 组件导入 ==========
import VolLoading from '@/components/basic/VolLoading'
import VolMenuFilter from '@/components/basic/VolMenuFilter.vue'
import VolMenu from '@/components/basic/VolElementMenu.vue'
import Message from './index/Message.vue'
import HomeSetting from './index/Setting.vue'
import IndexRouterView from './index/IndexRouterView'

// ========== 外部模块导入 ==========
import IndexDataConfig from './index/IndexDataConfig.js'
import IndexTabs from './index/IndexTabs.js'
import inintMenu from './index/IndexMethods.js'

// ========== Vue相关导入 ==========
import { reactive, ref, watch, onMounted, onUnmounted, getCurrentInstance, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import store from '../store/index'

// ========== Composables导入 ==========
import { useMenu } from '@/composables/useMenu'
import { useTheme } from '@/composables/useTheme'
import { useUser } from '@/composables/useUser'
import { useTabs } from '@/composables/useTabs'
import { useDateTime } from '@/composables/useDateTime'

// ========== 初始化 ==========
const router = useRouter()
const $route = useRoute()
const { proxy, appContext } = getCurrentInstance()

// 获取数据配置
const dataConfig = IndexDataConfig()

// ========== 标签页功能（需要先初始化） ==========
const {
  navigation,
  selectId,
  contextMenuVisible,
  visibleItem,
  menuTop,
  menuLeft,
  open,
  close,
  selectNav,
  removeNav,
  navCloseTabs,
  navRefreshPage,
  closeTabsMenu
} = useTabs(dataConfig, router, proxy)

// ========== 菜单功能 ==========
const {
  menuData,
  navMenuList,
  currentMenuId,
  navCurrentMenuId,
  isCollapse,
  menuWidth,
  menuOptions,
  menuDataClick,
  toggleLeft,
  getSelectMenuName
} = useMenu(dataConfig, router, proxy)

// 定义onSelect（需要使用open函数）
const onSelect = (treeId) => {
  const item = getSelectMenuName(treeId)
  open(item, false)
}

// ========== 主题功能 ==========
const {
  theme,
  layout,
  color,
  drawer_model,
  layoutIsLeft,
  layoutChange,
  initTheme
} = useTheme(proxy, dataConfig)

// ========== 用户功能 ==========
const {
  userInfo,
  userDropItems,
  icons,
  linkClick,
  getErrorImg
} = useUser(store, router, dataConfig, drawer_model, open)

// ========== 时间功能 ==========
const { indexDate, startDateTime } = useDateTime(proxy)

// ========== 其他数据 ==========
const permissionInited = dataConfig.permissionInited
const links = dataConfig.links
const messageList = reactive([])

// ========== 初始化逻辑 ==========
// 初始化首页导航
navigation.push({ orderNo: '0', id: '1', name: '首页', path: '/home' })

// 初始化主题
initTheme()

// 初始化菜单（调用后台服务）
inintMenu(proxy, dataConfig, router, onSelect)

// ========== 全局方法注册 ==========
// 注册全局菜单方法
appContext.config.globalProperties.menu = {
  show() {
    toggleLeft()
  },
  hide() {
    toggleLeft()
  }
}

// 注册全局标签页方法
Object.assign(proxy.$tabs, { open: open, close: close })

// ========== 顶部链接点击处理函数 ==========
const handleTopLinkClick = (item) => {
  console.log('顶部链接点击:', item.text, item)
  
  // 特殊处理数字大屏
  if (item.path === '/bigscreen') {
    console.log('处理数字大屏导航')
    
    // 确保数字大屏在菜单选项中
    if (!menuOptions.value.find(x => x.id === 'bigscreen')) {
      menuOptions.value.push({
        id: 'bigscreen',
        name: '数字大屏',
        path: '/bigscreen',
        url: '/bigscreen',
        parentId: null
      })
    }
    
    // 使用 onSelect 打开（这是菜单系统使用的方法）
    onSelect('bigscreen')
    return
  }
  
  // 调用 linkClick 处理其他链接
  linkClick(item)
}

// ========== 监听器 ==========
// 监听右键菜单
watch(
  () => contextMenuVisible.value,
  (newVal) => {
    if (newVal) {
      document.body.addEventListener('click', closeTabsMenu)
    } else {
      document.body.removeEventListener('click', closeTabsMenu)
    }
  }
)

// ========== 生命周期 ==========
onMounted(() => {
  startDateTime()
  
  // 调试信息
  console.log('组件已挂载')
  console.log('顶部链接列表:', links.value)
  console.log('菜单选项:', menuOptions.value.length, '项')
})
</script>

<style>
.horizontal-collapse-transition {
  transition: 0s width ease-in-out, 0s padding-left ease-in-out, 0s padding-right ease-in-out;
}
</style>