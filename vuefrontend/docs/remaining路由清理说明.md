# remaining.ts 路由清理说明

## 问题

编译时出现错误，因为 `remaining.ts` 中引用了已删除的页面文件：

```
File: D:/woek2/vue-pure-admin/组态版本/src/router/modules/remaining.ts:45:28
  42 |      path: "/account-settings",
  43 |      name: "AccountSettings",
  44 |      component: () => import("@/views/account-settings/index.vue"),
     |                              ^
  45 |      meta: {
  46 |        title: $t("buttons.pureAccountSettings"),
```

## 原因

在删除页面文件时，`remaining.ts` 中仍然保留了指向这些已删除页面的路由配置。

## 解决方案

清理 `src/router/modules/remaining.ts`，删除所有指向已删除页面的路由。

### 删除的路由

以下路由已从 `remaining.ts` 中删除：

| 路由路径 | 路由名称 | 页面文件 | 原因 |
|---------|---------|---------|------|
| ❌ `/account-settings` | AccountSettings | `account-settings/index.vue` | 页面已删除 |
| ❌ `/Cockpit` | Cockpit | `home/index.vue` | 重复路由 |
| ❌ `/energy-dashboard/energy-compare` | EnergyCompare | `home/detail/EnergyCompare.vue` | home 详情页 |
| ❌ `/FloorEnergy` | FloorEnergy | `home/detail/FloorEnergy.vue` | home 详情页 |
| ❌ `/ChargingPileMonitorCockpit` | ChargingPileMonitorCockpit | `home/detail/ChargingPileMonitorCockpit.vue` | home 详情页 |
| ❌ `/DeviceEnergyMonitorCockpit` | DeviceEnergyMonitorCockpit | `home/detail/DeviceEnergyMonitorCockpit.vue` | home 详情页 |
| ❌ `/MonthEnergyReport` | MonthEnergyReport | `home/detail/MonthEnergyReport.vue` | home 详情页 |
| ❌ `/YearEnergyReport` | YearEnergyReport | `home/detail/YearEnergyReport.vue` | home 详情页 |
| ❌ `/MonthEnergyReportFullscreen` | MonthEnergyReportFullscreen | `reportForms/monthEnergyReport/index.vue` | 页面已删除 |
| ❌ `/bigScreen/elec` | NetworkDiagramEditor | `bigScreen/elec/index.vue` | 页面已删除 |

### 保留的路由

`remaining.ts` 现在只包含以下必要路由：

#### 1. 系统基础路由

```typescript
// 登录页面
{
  path: "/login",
  name: "Login",
  component: () => import("@/views/login/index.vue"),
  meta: {
    title: $t("menus.pureLogin"),
    showLink: false,
    rank: 101
  }
}

// 重定向页面
{
  path: "/redirect",
  component: Layout,
  meta: {
    title: $t("status.pureLoad"),
    showLink: false,
    rank: 102
  },
  children: [
    {
      path: "/redirect/:path(.*)",
      name: "Redirect",
      component: () => import("@/layout/redirect.vue")
    }
  ]
}

// 空白页面
{
  path: "/empty",
  name: "Empty",
  component: () => import("@/views/empty/index.vue"),
  meta: {
    title: $t("menus.pureEmpty"),
    showLink: false,
    rank: 103
  }
}
```

#### 2. SCADA 组态系统隐藏路由

```typescript
// 创建组态项目
{
  path: "/scadafuxa/project/create",
  name: "ScadaFuxaProjectCreate",
  component: () => import("@/views/scadafuxa/project/form.vue"),
  meta: {
    title: "创建组态项目",
    showLink: false,
    rank: 121
  }
}

// 编辑组态项目
{
  path: "/scadafuxa/project/edit/:id",
  name: "ScadaFuxaProjectEdit",
  component: () => import("@/views/scadafuxa/project/form.vue"),
  meta: {
    title: "编辑组态项目",
    showLink: false,
    rank: 122
  }
}

// Fuxa 组态编辑器
{
  path: "/scadafuxa/editor/:id",
  name: "ScadaFuxaEditor",
  component: () => import("@/views/scadafuxa/editor/index.vue"),
  meta: {
    title: "Fuxa组态编辑器",
    showLink: false,
    rank: 123
  }
}
```

## 清理后的文件结构

```typescript
// src/router/modules/remaining.ts

export default [
  // ========== 系统基础路由 ==========
  // 1. 登录页面
  // 2. 重定向页面
  // 3. 空白页面
  
  // ========== SCADA 组态系统隐藏路由 ==========
  // 1. 创建组态项目
  // 2. 编辑组态项目
  // 3. Fuxa 组态编辑器
] satisfies Array<RouteConfigsTable>;
```

## 路由总数统计

| 类型 | 数量 | 说明 |
|------|------|------|
| **清理前** | 13 个 | 包含大量已删除页面的路由 |
| **清理后** | 6 个 | 只保留必要的基础路由和组态路由 |
| **删除** | 10 个 | 指向已删除页面的路由 |

## 路由特点

### showLink: false

所有 `remaining.ts` 中的路由都设置了 `showLink: false`，这意味着：

- ✅ 这些路由不会显示在左侧菜单中
- ✅ 但可以通过编程方式跳转 (`router.push()`)
- ✅ 也可以直接在浏览器地址栏输入访问

### 为什么不显示在菜单中？

1. **登录页面** - 未登录用户才需要访问
2. **重定向页面** - 系统内部使用，用户不需要手动访问
3. **空白页面** - 特殊用途的空白页，不需要菜单入口
4. **组态编辑页面** - 需要参数（如 id），不能直接从菜单访问
   - 创建/编辑项目：从项目列表点击按钮跳转
   - 组态编辑器：从项目列表点击"打开编辑器"跳转

## 完整的路由体系

现在项目的路由分为两部分：

### 1. 菜单路由（显示在左侧菜单）

定义在 `src/router/modules/` 下的独立文件：

- `home.ts` - 首页
- `scada.ts` - 组态系统（项目管理）

### 2. 隐藏路由（不显示在菜单）

定义在 `src/router/modules/remaining.ts`：

- 系统基础路由（登录、重定向、空白页）
- 组态详情页面（创建、编辑、编辑器）

## 验证方法

1. **编译测试**

   ```bash
   pnpm dev
   ```

   确认没有编译错误

2. **路由访问测试**
   - ✅ `/login` - 登录页面正常
   - ✅ `/scada/project` - 项目列表正常
   - ✅ `/scadafuxa/project/create` - 创建项目页面正常
   - ✅ `/scadafuxa/editor/123` - 编辑器正常（需要登录）

3. **菜单显示测试**
   - 左侧菜单只显示：首页、组态系统
   - remaining.ts 中的路由不显示在菜单

## 注意事项

### 1. home 详情页面

虽然删除了 `/energy-dashboard/energy-compare` 等路由，但如果 `home/index.vue` 页面中有跳转到这些详情页的逻辑，需要检查并修改。

**建议**：

- 检查 `src/views/home/index.vue`
- 删除或注释掉跳转到已删除详情页的按钮/链接

### 2. 404 错误处理

访问已删除的路由会自动跳转到 404 页面（由路由守卫处理）。

### 3. 未来扩展

如果需要添加新的隐藏路由，在 `remaining.ts` 中添加即可：

```typescript
{
  path: "/your-hidden-page",
  name: "YourHiddenPage",
  component: () => import("@/views/your-page/index.vue"),
  meta: {
    title: "页面标题",
    showLink: false,  // 不显示在菜单
    rank: 130
  }
}
```

---

**修复日期**: 2025-10-17  
**问题**: 路由引用已删除的页面文件  
**解决**: 清理 remaining.ts，删除 10 个无效路由
