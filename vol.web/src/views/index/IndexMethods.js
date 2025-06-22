import MessageConfig from './MessageConfig.js'
export default function (proxy, dataConfig, router, onSelect) {
  const store = proxy.$store
  const userInfo = dataConfig.userInfo
  let _userInfo = store.getters.getUserInfo()
  if (_userInfo) {
    userInfo.name = _userInfo.userName
    if (_userInfo.img) {
      userInfo.img = proxy.base.getImgSrc(_userInfo.img, proxy.http.ipAddress)
    }
  }

  proxy.http.get('api/menu/getTreeMenu', {}, false).then((result) => {
    const navMenuList = dataConfig.navMenuList
    const navCurrentMenuId = dataConfig.navCurrentMenuId
    const menuOptions = dataConfig.menuOptions
    const selectId = dataConfig.selectId
    const menuData = dataConfig.menuData

    let data = result
    if (dataConfig.layout.value != 'classics') {
      navMenuList.push(
        ...data.filter((c) => {
          return !c.parentId
        })
      )
      
      // 修改"智建管理"为"智慧建造"并加图标，并为每个一级菜单分配不同icon
      const iconList = [
        'el-icon-s-home',
        'el-icon-s-custom',
        'el-icon-s-operation',
        'el-icon-s-flag',
        'el-icon-s-order',
        'el-icon-s-platform',
        'el-icon-s-tools',
        'el-icon-s-data',
        'el-icon-s-marketing',
        'el-icon-s-finance',
        'el-icon-s-management',
        'el-icon-s-promotion',
        'el-icon-s-release',
        'el-icon-s-ticket',
        'el-icon-s-opportunity',
        'el-icon-s-claim',
        'el-icon-s-check',
        'el-icon-s-comment',
        'el-icon-s-cooperation',
        'el-icon-s-shop',
        'el-icon-s-goods',
        'el-icon-s-open',
        'el-icon-s-grid',
      ];
      
      navMenuList.forEach((item, idx) => {
        if (item.name === '智建管理' || item.name === '智慧建造') {
          item.name = '智慧建造';
          item.icon = 'el-icon-s-home';
        } else {
          item.icon = iconList[idx % iconList.length];
        }
        
        // 添加点击事件处理 - 新增
        item.onClick = () => {
          console.log('一级菜单点击:', item.name, item.id);
          if (onSelect && typeof onSelect === 'function') {
            onSelect(item.id);
          } else {
            console.error('onSelect 函数未定义或不是函数');
          }
        };
      });
      
      // 添加二级菜单图标映射 - 需要定义 childIconMap
      const childIconMap = {
        '项目信息': 'el-icon-folder',
        '项目配置': 'el-icon-setting',
        '工时配置': 'el-icon-time',
        '任务管理': 'el-icon-s-order',
        '人员管理': 'el-icon-s-custom',
        // 根据实际的二级菜单名称添加更多映射
      };
      
      navMenuList.forEach((item) => {
        if (item.children && item.children.length) {
          item.children.forEach((child) => {
            child.icon = childIconMap[child.name] || 'el-icon-menu';
          });
        }
      });
    }
    
    data.push({ id: '1', name: '首页', url: '/home' }) // 为了获取选中id使用

    initQueryParams(data)

    store.dispatch('setPermission', data)
    if (navMenuList.length) {
      navMenuList.forEach((m) => {
        m.children = data.filter((c) => {
          return c.parentId == m.id
        })
        m.children.forEach((c) => {
          c.parentId = 0
        })
        for (let index = 0; index < m.children.length; index++) {
          const mItem = m.children[index]
          let mChildrenItems = data.filter((c) => {
            return c.parentId == mItem.id
          })
          m.children.push(...mChildrenItems)
        }
      })
      let navMenuIndex = navMenuList.findIndex((c) => {
        return c.id === dataConfig.navCurrentMenuId.value
      })
      if (navMenuIndex == -1) {
        navCurrentMenuId.value = navMenuList[0].id
        menuData.push(...navMenuList[0].children)
      } else {
        menuData.push(...navMenuList[navMenuIndex].children)
      }
    } else {
      menuData.push(...data)
    }

    menuOptions.value = data
    dataConfig.permissionInited.value = true

    //开启消息推送（main.js中设置是否开启signalR)
    if (proxy.$global.signalR) {
      MessageConfig(proxy.http, (result) => {
       // messageList.unshift(result)
        //    console.log(result)
      })
    }

    //当前刷新是不是首页
    if (router.currentRoute.value.path != dataConfig.navigation[0].path) {
      //查找系统菜单
      let item = menuOptions.value.find((x) => {
        return x.url && x.url == router.currentRoute.value.fullPath
      })
      if (!item) {
        item = menuOptions.value.find((x) => {
          return x.path == router.currentRoute.value.path
        })
      }
      if (item) return onSelect(item.id)
      //查找顶部快捷连接
      item = dataConfig.links.value.find((x) => {
        return x.path == router.currentRoute.value.path
      })
      //查找最后一次跳转的页面
      if (!item) {
        item = getItem(proxy, router)
      }
      if (item) {
        return proxy.$tabs.open(item, false)
      }
    }
    selectId.value = '1'

    // 初始化链接 - 增强版本
    const topLinks = [
      {
        text: '数字大屏',
        name: '数字大屏',
        path: '/bigscreen',
        id: -2,
        icon: 'el-icon-monitor',
        left: true,      
       
      },
      {
        text: 'App移动端',
        name: 'App移动端',
        path: 'http://app.volcore.xyz/',
        id: -1,
        icon: 'el-icon-mobile',
        left: true,
        
      }
    ];
    
    dataConfig.links.value.push(...topLinks);
    
    // 调试信息
    console.log('菜单初始化完成:');
    console.log('- 一级菜单数量:', navMenuList.length);
    console.log('- 顶部链接数量:', dataConfig.links.value.length);
    console.log('- onSelect 函数类型:', typeof onSelect);
    console.log('- 当前路由路径:', router.currentRoute.value.path);
  })
}

const getItem = (proxy, router) => {
  let item =
    router.options.routes[0].children.find((x) => {
      return x.path == router.currentRoute.value.path;
    }) || {};
  //生成的编辑页面tabs名称
  if (item.meta && item.meta.name) {
    let name = item.meta.name;
    if (item.meta.edit) {
      name =
        proxy.$ts(name) +
        (router.currentRoute.value.query.id
          ? "(" + proxy.$ts("编辑") + ")"
          : "(" + proxy.$ts("新建") + ")");
    }
    item = {
      name: name,
      path: router.currentRoute.value.path,
      query: router.currentRoute.value.query,
    };
    return proxy.$tabs.open(item, false);
  } else {
    let nav = localStorage.getItem(window.location.origin + "_tabs");
    return nav ? JSON.parse(nav) : null;
  }
};

const initQueryParams = (data) => {
  for (let index = 0; index < data.length; index++) {
    const d = data[index]
    if (d.url && d.url.indexOf('?') != -1) {
      let _arr = d.url.split('?')
      d.path = _arr[0]
      _arr = _arr[1].split('&')
      let queryObj = {}
      for (let i = 0; i < _arr.length; i++) {
        // 遍历参数
        if (_arr[i].indexOf('=') != -1) {
          // 如果参数中有值
          let str = _arr[i].split('=')
          queryObj[str[0]] = str[1]
        }
      }
      d.query = queryObj
    } else {
      d.path = d.url
    }
    d.to = d.url
  }
}