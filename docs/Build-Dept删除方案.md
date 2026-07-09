# Buildinfo / Deptinfo 删除方案（B档：表+列全删）

> 生成日期：2026-07-09
> 决策前提：中台定位=解析+中转；本轮删 Build/Dept，Basicunitinfo 归多租户重构专题
> 证据来源：三个只读探子全量扫描 ~38 文件 / ~170+ 引用点
> 第一批纯删已完成：commit 88d9786，0 错误编译通过

---

## 一、核心发现：build/dept 引用分成"两类肉"，风险天差地别

| 类别 | 是什么 | 删除代价 | 符合中台定位？ |
|---|---|---|---|
| **甲类：Build/Dept 管理功能 + 数据打标** | 楼栋/部门 CRUD 页 + 把归属冗余抄进事件/设备记录 | 丢楼栋部门管理页 + 事件表少归属列 | ✅ 是上层业务，删对了 |
| **乙类：登录鉴权 + 行级数据隔离** | 登录构建权限树 + 按建筑/部门过滤查询作用域 | **丢失行级数据隔离，用户看到跨建筑/部门数据** | ⚠️ 是多租户重构要重写的同一套 |

**关键判断：乙类 = 档C 多租户重构要重写的同一块肉。** 本轮删乙类 = 先把现有隔离拆光，多租户专题再在废墟上重建。拆了再建，不如一起设计。

---

## 二、甲类清单（本轮删 · 安全独立 · 符合中台定位）

### 2.1 删除整表（Build/Dept 管理功能）
| 文件 | 动作 |
|---|---|
| IotModel/MEntity/Basic/Buildinfo.cs（类 BuildInfo） | 删 |
| IotModel/MEntity/Basic/Deptinfo.cs（类 DeptInfo） | 删 |
| IotModel/MDAO/Basic/BuildinfoDAO.cs（含树形级联逻辑） | 删 |
| IotModel/MDAO/Basic/DeptinfoDAO.cs | 删 |
| IotModel/FullEntity/BuildinfoEntity.cs（继承 BuildInfo） | 删 |
| IotModel/FullEntity/DeptinfoEntity.cs | 删 |
| IotWebApi/Areas/Basic/Controllers/BuildinfoController.cs | 删 |
| IotWebApi/Areas/Basic/Controllers/DeptinfoController.cs | 删 |
| IotModel/Expand/Expand_DeptBuild.cs（如仅此二表用） | 评估删 |
| IotWebApi/Areas/Basic/Models/DepartBuildNote.cs | 评估删 |

### 2.2 摘除数据打标列（事件/设备记录里的冗余归属）
| 文件 | 摘除内容 |
|---|---|
| IotModel/MEntity/Event/EventBase.cs | 删 BuildId/BuildName/DeptId/DeptName 四列（9 事件子类连带） |
| IotModel/MEntity/Device/DeviceInfo.cs | 删 BuildId/DeptId 两列 |
| IotModel/MEntity/Device/DeviceAlarmConfig.cs | 删 BuildId/DeptId |
| IotModel/MEntity/Device/DeviceParam.cs | 删 BuildId/DeptId |
| IotWebApi/Services/DataPointIngestService.cs | 删 FillEventBase 打标 + buildlist/deptlist（~25处） |
| IotWebApi/Services/AlarmLifecycleService.cs | 删打标透传（2处） |
| IotWebApi/Services/AlarmMaskService.cs | 删建筑级告警屏蔽 scope（L112，1处） |
| 各 Event/Device 控制器与 DTO | 删对应读写/回填/查询参数（EventAlarmDb/ControlDb/SignalDb/RunDb/ReportDay/PeakDay/EnergyAnalysis + Device 系列 + 报表 DataReportKit） |

---

## 三、乙类清单（建议并入多租户专题 · 高危 · 动鉴权）

| 文件 | 为什么危险 |
|---|---|
| IotWebApi/Common/OperatorCommon.cs（~20处） | 登录构建 build/dept 权限树 → _BuildInfoDic/_DeptInfoDic/_BuildIdList/_DeptIdList。删=改用户权限范围 |
| IotWebApi/Common/OperatorModel.cs | 持 List&lt;BuildInfo&gt;/List&lt;DeptInfo&gt;/Dictionary，删主表即编译失败 |
| IotWebApi/Filters/CustomActionFilterAttribute.cs（~12处） | 把权限树落成 SQL 查询条件，实现行级隔离。删=用户看到跨建筑/部门数据 |
| IotModel/MEntity/Admin/Sysrelated.cs（BuildIds/DeptCodes） | 用户建筑/部门数据权限表，鉴权语义 |
| IotWebApi/Areas/Scada/Controllers/ScadaProjectDataController.cs（L49-76） | 复制了一份同样的部门隔离逻辑 |
| IotWebApi/Areas/Admin/Controllers/SysrelatedController.cs | SysRelated 权限维护 |

**乙类特征：这 6 个文件是"一对多咬合"的鉴权整体（OperatorCommon 构建 → CustomActionFilter 消费 → SysRelated 存储），不能只删一端。而这正是多租户重构要用 tenant_id 重写的机制。**

---

## 四、三种执行路径对比

| 路径 | 本轮做什么 | 风险 | 一致性 |
|---|---|---|---|
| **① 甲乙全删（激进B档）** | 一次删光 38 文件 170 点位含鉴权隔离 | 高：拆掉行级隔离，且和多租户专题重复劳动 | 差：拆了再建 |
| **② 只删甲类（推荐）** | 删 Build/Dept 管理+打标列；乙类鉴权隔离原样留给多租户专题统一改 | 低：只丢楼栋部门管理页+事件归属列，隔离不动 | 好：一次到位不返工 |
| **③ 全缓，先出多租户设计** | 本轮不删，先写多租户重构详细设计 | 无 | 最稳但最慢 |

---

## 五、推荐

**路径②：本轮只删甲类。** 理由：
1. 甲类（管理功能+打标）是真正的上层业务，删除符合"解析+中转"的中台定位，且安全独立。
2. 乙类（鉴权+隔离）和多租户重构是同一套代码，本轮删=拆了专题再重建，白做一遍。
3. 符合 CLAUDE.md"精准修改、最小副作用、可验证"。

甲类删完后，`OperatorModel` 里 `List<BuildInfo>` 等类型引用仍需处理（因为 BuildInfo 实体被删）——这部分作为甲类删除的"必要收尾"一并改（比如临时保留空的权限载体或注释隔离逻辑），细节在动手时逐点确认。
