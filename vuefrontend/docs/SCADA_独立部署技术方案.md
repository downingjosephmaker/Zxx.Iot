# SCADA组态编辑器独立部署技术方案

> **版本**: v1.0
> **日期**: 2025-01-18
> **作者**: 开发团队
> **项目**: EnergyPlatform SCADA组态编辑器

---

## 📋 目录

- [1. 方案概述](#1-方案概述)
- [2. 架构设计](#2-架构设计)
- [3. 数据库设计](#3-数据库设计)
- [4. 后端API设计](#4-后端api设计)
- [5. 前端实现方案](#5-前端实现方案)
- [6. 部署方案](#6-部署方案)
- [7. 安全策略](#7-安全策略)
- [8. 测试方案](#8-测试方案)

---

## 1. 方案概述

### 1.1 业务背景

SCADA组态编辑器作为独立产品，需要支持：
- **内部编辑模式**：完整的组态编辑能力，需要登录认证
- **第三方运行时模式**：只读展示，实时数据更新，可iframe嵌入
- **数据持久化**：支持大型项目（1000+组件）的高效存储

### 1.2 核心需求

| 需求类型 | 描述 | 优先级 |
|---------|------|--------|
| 项目保存/加载 | 支持数据库索引+本地文件存储 | P0 |
| 资源文件管理 | 图片、视频、SVG独立存储 | P0 |
| 运行时模式 | 第三方iframe嵌入只读展示 | P1 |
| 导入/导出 | .fuxa格式文件打包 | P1 |

### 1.3 技术选型

**存储方案**：数据库索引 + 本地文件系统

**原因**：
- ✅ 数据库存储元数据和索引，查询快速
- ✅ 本地文件存储项目详细数据，无大小限制
- ✅ 资源文件独立存储，减少数据库压力
- ✅ JSON文件格式，便于阅读和维护

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────┐
│                    前端应用层                         │
├─────────────────────────────────────────────────────┤
│  编辑模式 (/scada/editor)                            │
│  - 完整编辑工具栏                                     │
│  - 组件面板、属性面板                                │
│  - 保存/加载/导出功能                                │
│  - 需要登录认证                                      │
├─────────────────────────────────────────────────────┤
│  运行时模式 (/scada/runtime)                         │
│  - 只读画布展示                                      │
│  - 实时数据刷新                                      │
│  - iframe嵌入支持                                    │
│  - 可选token认证                                     │
└─────────────────────────────────────────────────────┘
                        ↓ HTTPS
┌─────────────────────────────────────────────────────┐
│                   后端服务层                         │
├─────────────────────────────────────────────────────┤
│  API Controllers                                     │
│  ├─ ScadaProjectController (项目管理)                │
│  ├─ ScadaResourceController (资源管理)               │
│  └─ ScadaRuntimeController (运行时)                  │
└─────────────────────────────────────────────────────┘
         ↓                           ↓
┌──────────────────┐      ┌──────────────────────────┐
│   数据库(索引)    │      │   本地文件系统(数据)      │
├──────────────────┤      ├──────────────────────────┤
│ ScadaProject     │      │ D:/ScadaProjects/        │
│ - Id             │      │  ├─ project_123/         │
│ - Name           │      │  │  ├─ project.json      │
│ - FilePath ━━━━━━╋━━━━━━╋→ │  ├─ thumbnail.png     │
│ - Status         │      │  │  └─ resources/        │
│ - UpdateTime     │      │  └─ ...                  │
└──────────────────┘      └──────────────────────────┘
```

### 2.2 数据流向

#### 保存流程

```
编辑器修改组件
    ↓
点击保存按钮
    ↓
构建项目JSON数据
    ↓
调用后端API: POST /api/scada/project/save-to-file
    ↓
后端处理:
  1. 写入文件 → project.json
  2. 更新数据库 → ScadaProject表(元数据)
    ↓
返回文件路径和大小
    ↓
前端显示保存成功
```

#### 加载流程

```
打开项目列表
    ↓
点击某个项目
    ↓
调用后端API: GET /api/scada/project/get-meta/{id}
    ↓
获取数据库元数据(FilePath)
    ↓
调用后端API: GET /api/scada/project/load-from-file?path={FilePath}
    ↓
读取本地文件 → project.json
    ↓
解析JSON数据
    ↓
渲染画布和组件
    ↓
更新最后打开时间
```

---

## 3. 数据库设计

### 3.1 ScadaProject 主表

**表名**: `ScadaProject`
**说明**: 存储项目元数据和文件索引

```sql
CREATE TABLE ScadaProject (
    -- 主键
    Id BIGINT PRIMARY KEY,                        -- 雪花ID

    -- 基本信息
    Name NVARCHAR(200) NOT NULL,                  -- 项目名称
    Description NVARCHAR(500),                    -- 项目描述

    -- 文件路径（核心字段）
    FilePath NVARCHAR(500) NOT NULL,              -- 项目文件夹路径（如: D:/ScadaProjects/project_123）
    FileName NVARCHAR(200) DEFAULT 'project.json', -- 主文件名

    -- 元数据
    Version NVARCHAR(50) DEFAULT '1.0.0',         -- 版本号
    Status INT NOT NULL DEFAULT 0,                -- 状态：0-草稿, 1-已发布, 2-归档
    ComponentCount INT DEFAULT 0,                 -- 组件数量（冗余字段，提高查询性能）
    FileSize BIGINT DEFAULT 0,                    -- 文件大小（字节）

    -- 缩略图
    ThumbnailPath NVARCHAR(500),                  -- 缩略图路径（可选）

    -- 审计字段
    CreatedBy BIGINT NOT NULL,                    -- 创建人ID
    ModifiedBy BIGINT,                            -- 最后修改人ID
    CreateTime DATETIME2 DEFAULT GETDATE(),       -- 创建时间
    UpdateTime DATETIME2,                         -- 更新时间
    LastOpenTime DATETIME2,                       -- 最后打开时间

    -- 软删除
    IsDeleted BIT DEFAULT 0,                      -- 是否删除

    -- 索引
    CONSTRAINT PK_ScadaProject PRIMARY KEY (Id),
    INDEX IX_ScadaProject_Status_IsDeleted (Status, IsDeleted),
    INDEX IX_ScadaProject_UpdateTime (UpdateTime DESC),
    INDEX IX_ScadaProject_CreatedBy (CreatedBy)
);

-- 全文索引（支持名称搜索）
CREATE FULLTEXT INDEX ON ScadaProject(Name, Description)
    KEY INDEX PK_ScadaProject;
```

### 3.2 数据字典

| 字段名 | 类型 | 长度 | 必填 | 说明 | 示例值 |
|-------|------|------|------|------|--------|
| Id | BIGINT | - | ✅ | 主键，雪花ID | 1747923456789012345 |
| Name | NVARCHAR | 200 | ✅ | 项目名称 | "工厂监控系统" |
| Description | NVARCHAR | 500 | ❌ | 项目描述 | "车间温度监控大屏" |
| FilePath | NVARCHAR | 500 | ✅ | 项目文件夹路径 | "D:/ScadaProjects/project_123" |
| FileName | NVARCHAR | 200 | ❌ | 主文件名，默认project.json | "project.json" |
| Version | NVARCHAR | 50 | ❌ | 版本号 | "1.0.0" |
| Status | INT | - | ✅ | 状态（0草稿/1发布/2归档） | 1 |
| ComponentCount | INT | - | ❌ | 组件数量 | 56 |
| FileSize | BIGINT | - | ❌ | 文件大小（字节） | 2458624 |
| ThumbnailPath | NVARCHAR | 500 | ❌ | 缩略图路径 | "D:/ScadaProjects/project_123/thumbnail.png" |
| CreatedBy | BIGINT | - | ✅ | 创建人ID | 1001 |
| ModifiedBy | BIGINT | - | ❌ | 修改人ID | 1002 |
| CreateTime | DATETIME2 | - | ❌ | 创建时间 | "2025-01-18 10:00:00" |
| UpdateTime | DATETIME2 | - | ❌ | 更新时间 | "2025-01-18 12:30:00" |
| LastOpenTime | DATETIME2 | - | ❌ | 最后打开时间 | "2025-01-18 15:00:00" |
| IsDeleted | BIT | - | ❌ | 是否删除，默认0 | 0 |

### 3.3 文件系统结构

**根目录**: `D:/ScadaProjects/` （可配置）

```
D:/ScadaProjects/
│
├─ project_123/                          # 项目文件夹（ID: 123）
│   ├─ project.json                      # 项目主文件（完整数据）
│   ├─ project.meta.json                 # 元数据文件（可选）
│   ├─ thumbnail.png                     # 缩略图（400x300）
│   │
│   └─ resources/                        # 资源文件夹
│       ├─ images/                       # 图片资源
│       │   ├─ logo.png
│       │   ├─ background.jpg
│       │   └─ icon_01.svg
│       ├─ videos/                       # 视频资源
│       │   └─ intro.mp4
│       └─ svgs/                         # SVG资源
│           └─ custom-gauge.svg
│
├─ project_124/                          # 另一个项目
│   └─ ...
│
└─ .config/                              # 全局配置
    ├─ templates/                        # 项目模板
    └─ shared-resources/                 # 共享资源库
```

### 3.4 project.json 文件结构

```json
{
  "version": "1.0.0",
  "info": {
    "id": 123,
    "name": "工厂监控系统",
    "description": "车间温度监控大屏",
    "created": "2025-01-18T10:00:00",
    "modified": "2025-01-18T12:30:00",
    "author": "admin",
    "tags": ["工厂", "监控", "温度"]
  },
  "settings": {
    "canvasWidth": 1920,
    "canvasHeight": 1080,
    "gridSize": 10,
    "showGrid": true,
    "snapToGrid": true,
    "backgroundColor": "#f5f5f5",
    "backgroundImage": ""
  },
  "views": [
    {
      "id": "view_1",
      "name": "主画面",
      "description": "主监控界面",
      "components": [
        {
          "id": "temp_gauge_1",
          "type": "gauge",
          "name": "温度仪表",
          "position": { "x": 100, "y": 100 },
          "size": { "width": 300, "height": 300 },
          "rotation": 0,
          "zIndex": 10,
          "properties": {
            "min": 0,
            "max": 100,
            "value": 25,
            "unit": "°C",
            "decimals": 1
          },
          "style": {
            "backgroundColor": "#ffffff",
            "borderColor": "#409eff",
            "borderWidth": 2,
            "borderRadius": 4
          },
          "dataBinding": {
            "sourceType": "mqtt",
            "deviceId": "device_001",
            "tagId": "temp_sensor_1",
            "propertyName": "value",
            "refreshInterval": 1000
          },
          "events": [
            {
              "id": "event_001",
              "type": "click",
              "enabled": true,
              "actions": [
                {
                  "type": "dialog",
                  "message": "当前温度: {value}°C"
                }
              ]
            }
          ]
        }
      ]
    }
  ],
  "devices": [
    {
      "id": "device_001",
      "name": "车间温度传感器",
      "type": "mqtt",
      "enabled": true,
      "connection": {
        "broker": "mqtt://192.168.1.100:1883",
        "username": "admin",
        "password": "encrypted_password",
        "clientId": "scada_client_001",
        "topics": ["factory/workshop/temp"]
      },
      "tags": [
        {
          "id": "temp_sensor_1",
          "name": "温度值",
          "address": "factory/workshop/temp",
          "dataType": "float",
          "unit": "°C",
          "min": -50,
          "max": 150
        }
      ]
    }
  ],
  "datasets": [
    {
      "id": "dataset_001",
      "name": "生产数据API",
      "type": "api",
      "enabled": true,
      "config": {
        "url": "http://192.168.1.200/api/production",
        "method": "GET",
        "headers": {
          "Authorization": "Bearer {token}"
        },
        "refreshInterval": 5000,
        "dataPath": "data.items"
      }
    }
  ],
  "resources": {
    "images": [
      {
        "id": "img_001",
        "name": "logo.png",
        "path": "resources/images/logo.png",
        "size": 45600,
        "width": 200,
        "height": 80,
        "uploadTime": "2025-01-18T10:00:00"
      }
    ],
    "videos": [
      {
        "id": "video_001",
        "name": "intro.mp4",
        "path": "resources/videos/intro.mp4",
        "size": 15728640,
        "duration": 60,
        "uploadTime": "2025-01-18T10:05:00"
      }
    ],
    "svgs": [
      {
        "id": "svg_001",
        "name": "custom-gauge.svg",
        "path": "resources/svgs/custom-gauge.svg",
        "size": 8192,
        "uploadTime": "2025-01-18T10:10:00"
      }
    ]
  }
}
```

---

## 4. 后端API设计

### 4.1 API概览

| 序号 | 接口路径 | 方法 | 说明 | 权限 |
|-----|---------|------|------|------|
| 1 | `/api/scada/project/list` | POST | 分页查询项目列表 | 需登录 |
| 2 | `/api/scada/project/get-meta/{id}` | GET | 获取项目元数据 | 需登录 |
| 3 | `/api/scada/project/save-to-file` | POST | 保存项目到本地文件 | 需登录 |
| 4 | `/api/scada/project/load-from-file` | GET | 从本地文件加载项目 | 需登录 |
| 5 | `/api/scada/project/delete/{id}` | DELETE | 删除项目（软删除） | 需登录 |
| 6 | `/api/scada/project/export/{id}` | GET | 导出项目为.fuxa文件 | 需登录 |
| 7 | `/api/scada/project/import` | POST | 导入.fuxa文件 | 需登录 |
| 8 | `/api/scada/project/update-last-open-time/{id}` | POST | 更新最后打开时间 | 需登录 |
| 9 | `/api/scada/resources/upload` | POST | 上传资源文件 | 需登录 |
| 10 | `/api/scada/resources/get/{projectId}/{type}/{fileName}` | GET | 获取资源文件 | 公开 |
| 11 | `/api/scada/runtime/get-project/{id}` | GET | 运行时获取项目数据 | Token可选 |

### 4.2 接口详细设计

#### 4.2.1 分页查询项目列表

**接口**: `POST /api/scada/project/list`

**请求体**:
```json
{
  "keyword": "工厂",
  "status": 1,
  "pageIndex": 1,
  "pageSize": 20,
  "orderBy": "UpdateTime",
  "orderDirection": "DESC"
}
```

**响应**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": 123,
        "name": "工厂监控系统",
        "description": "车间温度监控",
        "version": "1.0.0",
        "status": 1,
        "componentCount": 56,
        "fileSize": 2458624,
        "thumbnailPath": "D:/ScadaProjects/project_123/thumbnail.png",
        "createdBy": 1001,
        "createdByName": "张三",
        "createTime": "2025-01-18T10:00:00",
        "updateTime": "2025-01-18T12:30:00",
        "lastOpenTime": "2025-01-18T15:00:00"
      }
    ],
    "totalCount": 50,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 3
  }
}
```

#### 4.2.2 保存项目到本地文件

**接口**: `POST /api/scada/project/save-to-file`

**请求体**:
```json
{
  "id": 123,
  "name": "工厂监控系统",
  "description": "车间温度监控",
  "projectData": "{...完整的JSON字符串...}",
  "version": "1.0.0",
  "status": 1,
  "componentCount": 56
}
```

**响应**:
```json
{
  "success": true,
  "message": "项目保存成功",
  "data": {
    "id": 123,
    "filePath": "D:/ScadaProjects/project_123",
    "fileSize": 2458624,
    "updateTime": "2025-01-18T12:30:00"
  }
}
```

#### 4.2.3 从本地文件加载项目

**接口**: `GET /api/scada/project/load-from-file?filePath={filePath}`

**参数**:
- `filePath`: 项目文件夹路径（如: `D:/ScadaProjects/project_123`）

**响应**:
```json
{
  "success": true,
  "message": "加载成功",
  "data": {
    "content": "{...完整的JSON字符串...}",
    "filePath": "D:/ScadaProjects/project_123/project.json",
    "size": 2458624,
    "lastModified": "2025-01-18T12:30:00"
  }
}
```

#### 4.2.4 导出项目为.fuxa文件

**接口**: `GET /api/scada/project/export/{id}`

**参数**:
- `id`: 项目ID

**响应**: 直接返回文件流（application/zip）

**文件名**: `工厂监控系统_20250118.fuxa`

#### 4.2.5 导入.fuxa文件

**接口**: `POST /api/scada/project/import`

**请求**: multipart/form-data
- `file`: .fuxa压缩包文件

**响应**:
```json
{
  "success": true,
  "message": "导入成功",
  "data": {
    "id": 125,
    "name": "工厂监控系统（导入）",
    "filePath": "D:/ScadaProjects/project_125",
    "componentCount": 56
  }
}
```

#### 4.2.6 上传资源文件

**接口**: `POST /api/scada/resources/upload`

**请求**: multipart/form-data
- `file`: 资源文件（图片/视频/SVG）
- `projectId`: 项目ID
- `resourceType`: 资源类型（image/video/svg）

**响应**:
```json
{
  "success": true,
  "message": "上传成功",
  "data": {
    "id": "img_001",
    "fileName": "logo.png",
    "filePath": "resources/images/logo.png",
    "url": "/api/scada/resources/get/123/image/logo_abc123.png",
    "size": 45600,
    "type": "image"
  }
}
```

#### 4.2.7 获取资源文件

**接口**: `GET /api/scada/resources/get/{projectId}/{type}/{fileName}`

**参数**:
- `projectId`: 项目ID
- `type`: 资源类型（image/video/svg）
- `fileName`: 文件名

**响应**: 直接返回文件流（根据文件类型设置Content-Type）

### 4.3 后端实现代码

#### 4.3.1 ScadaProjectController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace EnergyPlatform.Controllers
{
    [ApiController]
    [Route("api/scada/project")]
    [Authorize] // 需要登录认证
    public class ScadaProjectController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<ScadaProjectController> _logger;
        private readonly string _projectRootPath;

        public ScadaProjectController(
            IConfiguration config,
            AppDbContext db,
            ILogger<ScadaProjectController> logger)
        {
            _config = config;
            _db = db;
            _logger = logger;

            // 从配置读取项目根路径
            _projectRootPath = _config["ScadaSettings:ProjectPath"]
                ?? "D:/ScadaProjects";

            // 确保根目录存在
            Directory.CreateDirectory(_projectRootPath);
        }

        /// <summary>
        /// 分页查询项目列表
        /// </summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetProjectList([FromBody] ProjectListRequest request)
        {
            try
            {
                var query = _db.ScadaProjects
                    .Where(p => !p.IsDeleted);

                // 关键词搜索
                if (!string.IsNullOrWhiteSpace(request.Keyword))
                {
                    query = query.Where(p =>
                        p.Name.Contains(request.Keyword) ||
                        p.Description.Contains(request.Keyword));
                }

                // 状态筛选
                if (request.Status.HasValue)
                {
                    query = query.Where(p => p.Status == request.Status.Value);
                }

                // 总数
                var totalCount = await query.CountAsync();

                // 排序
                query = request.OrderBy?.ToLower() switch
                {
                    "name" => request.OrderDirection == "ASC"
                        ? query.OrderBy(p => p.Name)
                        : query.OrderByDescending(p => p.Name),
                    "createtime" => request.OrderDirection == "ASC"
                        ? query.OrderBy(p => p.CreateTime)
                        : query.OrderByDescending(p => p.CreateTime),
                    _ => query.OrderByDescending(p => p.UpdateTime)
                };

                // 分页
                var items = await query
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Version,
                        p.Status,
                        p.ComponentCount,
                        p.FileSize,
                        p.ThumbnailPath,
                        p.CreatedBy,
                        p.CreateTime,
                        p.UpdateTime,
                        p.LastOpenTime
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "查询成功",
                    data = new
                    {
                        items,
                        totalCount,
                        pageIndex = request.PageIndex,
                        pageSize = request.PageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询项目列表失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查询失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取项目元数据
        /// </summary>
        [HttpGet("get-meta/{id}")]
        public async Task<IActionResult> GetProjectMeta(long id)
        {
            try
            {
                var project = await _db.ScadaProjects
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return NotFound(new { success = false, message = "项目不存在" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        project.Id,
                        project.Name,
                        project.Description,
                        project.FilePath,
                        project.FileName,
                        project.Version,
                        project.Status,
                        project.ComponentCount,
                        project.FileSize,
                        project.ThumbnailPath,
                        project.CreateTime,
                        project.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取项目元数据失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 保存项目到本地文件
        /// </summary>
        [HttpPost("save-to-file")]
        public async Task<IActionResult> SaveToFile([FromBody] SaveProjectRequest request)
        {
            try
            {
                // 1. 新建或更新数据库记录
                var project = await _db.ScadaProjects.FindAsync(request.Id);
                var isNew = project == null;

                if (isNew)
                {
                    project = new ScadaProject
                    {
                        Id = SnowflakeIdGenerator.NextId(),
                        CreatedBy = GetCurrentUserId(),
                        CreateTime = DateTime.Now
                    };
                }

                project.Name = request.Name;
                project.Description = request.Description;
                project.Version = request.Version;
                project.Status = request.Status;
                project.ComponentCount = request.ComponentCount;
                project.ModifiedBy = GetCurrentUserId();
                project.UpdateTime = DateTime.Now;

                // 2. 确定文件保存路径
                var projectFolder = Path.Combine(_projectRootPath, $"project_{project.Id}");
                var projectFilePath = Path.Combine(projectFolder, "project.json");

                project.FilePath = projectFolder;
                project.FileName = "project.json";

                // 3. 创建项目文件夹结构
                Directory.CreateDirectory(projectFolder);
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "images"));
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "videos"));
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "svgs"));

                // 4. 写入项目文件（格式化JSON便于阅读）
                await System.IO.File.WriteAllTextAsync(
                    projectFilePath,
                    request.ProjectData,
                    Encoding.UTF8
                );

                // 5. 更新文件大小
                var fileInfo = new FileInfo(projectFilePath);
                project.FileSize = fileInfo.Length;

                // 6. 保存到数据库
                if (isNew)
                    await _db.ScadaProjects.AddAsync(project);

                await _db.SaveChangesAsync();

                _logger.LogInformation($"项目保存成功: {project.Name} (ID: {project.Id})");

                return Ok(new
                {
                    success = true,
                    message = "项目保存成功",
                    data = new
                    {
                        id = project.Id,
                        filePath = projectFolder,
                        fileSize = project.FileSize,
                        updateTime = project.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存项目失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"保存失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 从本地文件加载项目
        /// </summary>
        [HttpGet("load-from-file")]
        public async Task<IActionResult> LoadFromFile([FromQuery] string filePath)
        {
            try
            {
                var projectFilePath = Path.Combine(filePath, "project.json");

                if (!System.IO.File.Exists(projectFilePath))
                    return NotFound(new { success = false, message = "项目文件不存在" });

                // 读取文件内容
                var content = await System.IO.File.ReadAllTextAsync(projectFilePath, Encoding.UTF8);

                // 验证JSON格式
                JsonDocument.Parse(content);

                return Ok(new
                {
                    success = true,
                    message = "加载成功",
                    data = new
                    {
                        content,
                        filePath = projectFilePath,
                        size = new FileInfo(projectFilePath).Length,
                        lastModified = System.IO.File.GetLastWriteTime(projectFilePath)
                    }
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON格式错误");
                return BadRequest(new
                {
                    success = false,
                    message = "项目文件格式错误"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载项目失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"加载失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 删除项目（软删除）
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProject(long id)
        {
            try
            {
                var project = await _db.ScadaProjects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = "项目不存在" });

                // 软删除
                project.IsDeleted = true;
                project.UpdateTime = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"项目已删除: {project.Name} (ID: {id})");

                return Ok(new
                {
                    success = true,
                    message = "项目已删除"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除项目失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"删除失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 导出项目为.fuxa文件
        /// </summary>
        [HttpGet("export/{id}")]
        public async Task<IActionResult> ExportProject(long id)
        {
            try
            {
                var project = await _db.ScadaProjects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = "项目不存在" });

                if (!Directory.Exists(project.FilePath))
                    return NotFound(new { success = false, message = "项目文件夹不存在" });

                // 创建临时ZIP文件
                var zipPath = Path.Combine(Path.GetTempPath(), $"{project.Name}_{DateTime.Now:yyyyMMddHHmmss}.zip");

                // 压缩整个项目文件夹
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(project.FilePath, zipPath);

                // 返回文件流
                var fileBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
                var fileName = $"{project.Name}_{DateTime.Now:yyyyMMdd}.fuxa";

                // 删除临时文件
                System.IO.File.Delete(zipPath);

                return File(fileBytes, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出项目失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"导出失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 导入.fuxa文件
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> ImportProject(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "文件为空" });

                // 验证文件扩展名
                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".fuxa" && ext != ".zip")
                    return BadRequest(new { success = false, message = "文件格式错误，仅支持.fuxa或.zip文件" });

                // 创建临时文件夹
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                // 保存上传文件
                var tempZipPath = Path.Combine(tempFolder, file.FileName);
                using (var stream = new FileStream(tempZipPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 解压文件
                var extractFolder = Path.Combine(tempFolder, "extracted");
                ZipFile.ExtractToDirectory(tempZipPath, extractFolder);

                // 读取project.json
                var projectJsonPath = Path.Combine(extractFolder, "project.json");
                if (!System.IO.File.Exists(projectJsonPath))
                {
                    Directory.Delete(tempFolder, true);
                    return BadRequest(new { success = false, message = "无效的.fuxa文件，缺少project.json" });
                }

                var projectJson = await System.IO.File.ReadAllTextAsync(projectJsonPath);
                var json = JsonDocument.Parse(projectJson);
                var name = json.RootElement.GetProperty("info").GetProperty("name").GetString();

                // 创建新项目
                var newId = SnowflakeIdGenerator.NextId();
                var targetFolder = Path.Combine(_projectRootPath, $"project_{newId}");

                // 复制文件到目标文件夹
                CopyDirectory(extractFolder, targetFolder);

                // 保存到数据库
                var project = new ScadaProject
                {
                    Id = newId,
                    Name = $"{name}（导入）",
                    FilePath = targetFolder,
                    FileName = "project.json",
                    Version = json.RootElement.GetProperty("version").GetString(),
                    Status = 0, // 草稿状态
                    CreatedBy = GetCurrentUserId(),
                    CreateTime = DateTime.Now
                };

                // 统计组件数量
                if (json.RootElement.TryGetProperty("views", out var views) && views.GetArrayLength() > 0)
                {
                    var firstView = views[0];
                    if (firstView.TryGetProperty("components", out var components))
                    {
                        project.ComponentCount = components.GetArrayLength();
                    }
                }

                await _db.ScadaProjects.AddAsync(project);
                await _db.SaveChangesAsync();

                // 删除临时文件夹
                Directory.Delete(tempFolder, true);

                _logger.LogInformation($"项目导入成功: {project.Name} (ID: {newId})");

                return Ok(new
                {
                    success = true,
                    message = "导入成功",
                    data = new
                    {
                        id = project.Id,
                        name = project.Name,
                        filePath = project.FilePath,
                        componentCount = project.ComponentCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入项目失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"导入失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 更新最后打开时间
        /// </summary>
        [HttpPost("update-last-open-time/{id}")]
        public async Task<IActionResult> UpdateLastOpenTime(long id)
        {
            try
            {
                var project = await _db.ScadaProjects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = "项目不存在" });

                project.LastOpenTime = DateTime.Now;
                await _db.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新最后打开时间失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"更新失败: {ex.Message}"
                });
            }
        }

        // ========== 私有方法 ==========

        /// <summary>
        /// 获取当前登录用户ID
        /// </summary>
        private long GetCurrentUserId()
        {
            // TODO: 从JWT或Session中获取当前用户ID
            return 1001;
        }

        /// <summary>
        /// 递归复制文件夹
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                System.IO.File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }

    // ========== 请求/响应模型 ==========

    public class ProjectListRequest
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "UpdateTime";
        public string OrderDirection { get; set; } = "DESC";
    }

    public class SaveProjectRequest
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProjectData { get; set; }
        public string Version { get; set; } = "1.0.0";
        public int Status { get; set; } = 0;
        public int ComponentCount { get; set; }
    }
}
```

#### 4.3.2 ScadaResourceController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnergyPlatform.Controllers
{
    [ApiController]
    [Route("api/scada/resources")]
    public class ScadaResourceController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ScadaResourceController> _logger;
        private readonly string _projectRootPath;

        public ScadaResourceController(
            IConfiguration config,
            ILogger<ScadaResourceController> logger)
        {
            _config = config;
            _logger = logger;
            _projectRootPath = _config["ScadaSettings:ProjectPath"] ?? "D:/ScadaProjects";
        }

        /// <summary>
        /// 上传资源文件
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadResource(
            IFormFile file,
            [FromForm] long projectId,
            [FromForm] string resourceType = "image")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "文件为空" });

                // 验证文件类型
                var allowedTypes = new Dictionary<string, string[]>
                {
                    ["image"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                    ["video"] = new[] { ".mp4", ".webm", ".ogg" },
                    ["svg"] = new[] { ".svg" }
                };

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (!allowedTypes[resourceType].Contains(ext))
                    return BadRequest(new { success = false, message = $"不支持的{resourceType}文件类型" });

                // 文件大小限制
                var maxSize = resourceType == "video" ? 100 * 1024 * 1024 : 10 * 1024 * 1024; // 视频100MB，其他10MB
                if (file.Length > maxSize)
                    return BadRequest(new { success = false, message = $"文件大小超过限制" });

                // 生成存储路径
                var projectFolder = Path.Combine(_projectRootPath, $"project_{projectId}");
                var resourceFolder = Path.Combine(projectFolder, "resources", $"{resourceType}s");
                Directory.CreateDirectory(resourceFolder);

                // 生成唯一文件名
                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
                var filePath = Path.Combine(resourceFolder, fileName);

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 返回访问URL
                var url = $"/api/scada/resources/get/{projectId}/{resourceType}/{fileName}";
                var relativePath = $"resources/{resourceType}s/{fileName}";

                _logger.LogInformation($"资源文件上传成功: {fileName}");

                return Ok(new
                {
                    success = true,
                    message = "上传成功",
                    data = new
                    {
                        id = $"{resourceType}_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                        fileName = file.FileName,
                        filePath = relativePath,
                        url,
                        size = file.Length,
                        type = resourceType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传资源文件失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"上传失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取资源文件
        /// </summary>
        [HttpGet("get/{projectId}/{resourceType}/{fileName}")]
        [AllowAnonymous] // 允许匿名访问（用于运行时模式）
        public IActionResult GetResource(long projectId, string resourceType, string fileName)
        {
            try
            {
                var filePath = Path.Combine(
                    _projectRootPath,
                    $"project_{projectId}",
                    "resources",
                    $"{resourceType}s",
                    fileName
                );

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { success = false, message = "文件不存在" });

                // 获取Content-Type
                var contentType = GetContentType(Path.GetExtension(fileName));

                // 返回文件流
                return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取资源文件失败: {fileName}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"获取失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取Content-Type
        /// </summary>
        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                _ => "application/octet-stream"
            };
        }
    }
}
```

#### 4.3.3 appsettings.json 配置

```json
{
  "ScadaSettings": {
    "ProjectPath": "D:/ScadaProjects",
    "MaxFileSize": 52428800,
    "EnableCompression": false,
    "AllowedImageTypes": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "AllowedVideoTypes": [".mp4", ".webm", ".ogg"],
    "MaxImageSize": 10485760,
    "MaxVideoSize": 104857600
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "EnergyPlatform.Controllers.ScadaProjectController": "Debug"
    }
  }
}
```

---

## 5. 前端实现方案

### 5.1 API接口封装

#### 5.1.1 新建API文件

**文件路径**: `src/api/scada/project/index.ts`

```typescript
import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** SCADA项目API */

/** 分页查询项目列表 */
export const getProjectList = (data?: {
  keyword?: string;
  status?: number;
  pageIndex?: number;
  pageSize?: number;
  orderBy?: string;
  orderDirection?: string;
}) => {
  return http.request<ResultTable>("post", "/scada/project/list", { data });
};

/** 获取项目元数据 */
export const getProjectMeta = (id: string | number) => {
  return http.request<Result>("get", `/scada/project/get-meta/${id}`);
};

/** 保存项目到本地文件 */
export const saveProjectToFile = (data: {
  id?: number;
  name: string;
  description?: string;
  projectData: string;
  version?: string;
  status?: number;
  componentCount?: number;
}) => {
  return http.request<Result>("post", "/scada/project/save-to-file", { data });
};

/** 从本地文件加载项目 */
export const loadProjectFromFile = (filePath: string) => {
  return http.request<Result>("get", "/scada/project/load-from-file", {
    params: { filePath }
  });
};

/** 删除项目 */
export const deleteProject = (id: string | number) => {
  return http.request<Result>("delete", `/scada/project/delete/${id}`);
};

/** 导出项目 */
export const exportProject = (id: string | number) => {
  return `/api/scada/project/export/${id}`;
};

/** 导入项目 */
export const importProject = (file: File) => {
  const formData = new FormData();
  formData.append('file', file);
  return http.request<Result>("post", "/scada/project/import", {
    data: formData,
    headers: { 'Content-Type': 'multipart/form-data' }
  });
};

/** 更新最后打开时间 */
export const updateLastOpenTime = (id: string | number) => {
  return http.request<Result>("post", `/scada/project/update-last-open-time/${id}`);
};

/** 上传资源文件 */
export const uploadResource = (file: File, projectId: number, resourceType: 'image' | 'video' | 'svg') => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('projectId', projectId.toString());
  formData.append('resourceType', resourceType);
  return http.request<Result>("post", "/scada/resources/upload", {
    data: formData,
    headers: { 'Content-Type': 'multipart/form-data' }
  });
};

export default {
  getProjectList,
  getProjectMeta,
  saveProjectToFile,
  loadProjectFromFile,
  deleteProject,
  exportProject,
  importProject,
  updateLastOpenTime,
  uploadResource
};
```

#### 5.1.2 更新API统一导出

**文件路径**: `src/api/scada/index.ts`

```typescript
import * as project from "./project";
import { fuxaProjectApi } from "./fuxa";

// SCADA API 统一导出
export const scadaApi = {
  project: {
    ...project.default
  },
  fuxa: {
    ...fuxaProjectApi
  }
};

export default scadaApi;
export { fuxaProjectApi } from "./fuxa";
export * from "./fuxa/types";
```

### 5.2 编辑器保存/加载实现

#### 5.2.1 在 index.vue 中实现保存功能

**文件路径**: `src/views/scada/index.vue`

```typescript
// 导入API
import scadaApi from "@/api/scada";

// 保存项目
const saveProject = async () => {
  try {
    loading.value = true;

    // 1. 构建完整的项目JSON数据
    const projectJson = {
      version: projectInfo.value.Version || "1.0.0",
      info: {
        id: projectInfo.value.SnowId,
        name: projectInfo.value.Name,
        description: projectInfo.value.Description || "",
        created: projectInfo.value.CreateTime || new Date().toISOString(),
        modified: new Date().toISOString(),
        author: "current_user", // TODO: 从用户信息获取
        tags: []
      },
      settings: {
        canvasWidth: canvasWidth.value,
        canvasHeight: canvasHeight.value,
        gridSize: gridSize.value,
        showGrid: showGrid.value,
        snapToGrid: snapToGrid.value,
        backgroundColor: canvasBackgroundColor.value,
        backgroundImage: canvasBackgroundImage.value
      },
      views: projectData.value.views || [],
      devices: deviceList.value || [],
      datasets: datasetList.value || [],
      resources: {
        images: [],
        videos: [],
        svgs: []
      }
    };

    // 2. 统计组件数量
    const componentCount = projectJson.views[0]?.components?.length || 0;

    // 3. 调用API保存
    const response = await scadaApi.project.saveProjectToFile({
      id: projectInfo.value.SnowId,
      name: projectInfo.value.Name,
      description: projectInfo.value.Description,
      projectData: JSON.stringify(projectJson, null, 2), // 格式化JSON
      version: projectInfo.value.Version,
      status: projectInfo.value.Status,
      componentCount
    });

    if (response.success) {
      // 4. 更新项目信息
      if (!projectInfo.value.SnowId) {
        projectInfo.value.SnowId = response.data.id;
        projectInfo.value.FilePath = response.data.filePath;

        // 更新路由（新建项目）
        router.replace({
          name: 'ScadaEditor',
          params: { id: response.data.id }
        });
      }

      isSaved.value = true;
      ElMessage.success(
        `项目已保存 (文件大小: ${(response.data.fileSize / 1024).toFixed(2)} KB)`
      );
    }
  } catch (error) {
    console.error('保存项目失败:', error);
    ElMessage.error('保存失败: ' + (error as Error).message);
  } finally {
    loading.value = false;
  }
};

// 加载项目
const loadProject = async (projectId: string) => {
  try {
    loading.value = true;

    // 1. 获取项目元数据
    const metaResponse = await scadaApi.project.getProjectMeta(projectId);

    if (!metaResponse.success || !metaResponse.data) {
      ElMessage.error('项目不存在');
      return;
    }

    const meta = metaResponse.data;

    // 2. 从本地文件加载项目数据
    const fileResponse = await scadaApi.project.loadProjectFromFile(meta.FilePath);

    if (!fileResponse.success || !fileResponse.data) {
      ElMessage.error('加载项目文件失败');
      return;
    }

    // 3. 解析JSON数据
    const projectJson = JSON.parse(fileResponse.data.content);

    // 4. 恢复项目信息
    projectInfo.value = {
      SnowId: meta.Id,
      Name: projectJson.info.name,
      Description: projectJson.info.description,
      Version: projectJson.version,
      Status: meta.Status,
      FilePath: meta.FilePath,
      CreateTime: meta.CreateTime,
      UpdateTime: meta.UpdateTime
    };

    // 5. 恢复项目数据
    projectData.value = {
      views: projectJson.views || [{ id: 'view_1', name: '主画面', components: [] }],
      devices: projectJson.devices || [],
      datasets: projectJson.datasets || []
    };

    deviceList.value = projectJson.devices || [];
    datasetList.value = projectJson.datasets || [];

    // 6. 恢复画布设置
    if (projectJson.settings) {
      canvasWidth.value = projectJson.settings.canvasWidth || 1920;
      canvasHeight.value = projectJson.settings.canvasHeight || 1080;
      gridSize.value = projectJson.settings.gridSize || 10;
      showGrid.value = projectJson.settings.showGrid !== false;
      snapToGrid.value = projectJson.settings.snapToGrid !== false;
      canvasBackgroundColor.value = projectJson.settings.backgroundColor || '#f5f5f5';
      canvasBackgroundImage.value = projectJson.settings.backgroundImage || '';
    }

    // 7. 渲染画布
    await nextTick();
    redrawCanvas();

    // 8. 更新最后打开时间
    await scadaApi.project.updateLastOpenTime(projectId);

    isSaved.value = true;
    ElMessage.success(
      `项目加载成功 (组件数: ${projectData.value.views[0]?.components?.length || 0}, ` +
      `大小: ${(fileResponse.data.size / 1024).toFixed(2)} KB)`
    );
  } catch (error) {
    console.error('加载项目失败:', error);
    ElMessage.error('加载失败: ' + (error as Error).message);
  } finally {
    loading.value = false;
  }
};

// 初始化新项目
const initializeNewProject = () => {
  projectInfo.value = {
    SnowId: null,
    Name: "未命名项目",
    Description: "",
    Version: "1.0.0",
    Status: 0
  };

  projectData.value = {
    views: [
      {
        id: 'view_1',
        name: '主画面',
        description: '',
        components: []
      }
    ],
    devices: [],
    datasets: []
  };

  deviceList.value = [];
  datasetList.value = [];

  // 默认画布设置
  canvasWidth.value = 1920;
  canvasHeight.value = 1080;
  gridSize.value = 10;
  showGrid.value = true;
  snapToGrid.value = true;
  canvasBackgroundColor.value = '#f5f5f5';
  canvasBackgroundImage.value = '';

  isSaved.value = false;
};

// 组件挂载时加载项目
onMounted(async () => {
  // ... 现有初始化代码 ...

  // 如果有项目ID，加载项目
  if (projectId.value && projectId.value !== 'new') {
    await loadProject(projectId.value);
  } else {
    // 新建项目，初始化空数据
    initializeNewProject();
  }
});
```

### 5.3 资源文件上传管理

#### 5.3.1 图片上传实现

```typescript
// 上传图片并创建组件
const handleImageUpload = async (event: Event, position: { x: number; y: number }) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;

  // 验证文件类型
  if (!file.type.startsWith('image/')) {
    ElMessage.error('请选择图片文件');
    return;
  }

  // 验证文件大小
  if (file.size > 10 * 1024 * 1024) {
    ElMessage.error('图片大小不能超过10MB');
    return;
  }

  try {
    loading.value = true;

    // 上传到服务器
    const response = await scadaApi.project.uploadResource(
      file,
      projectInfo.value.SnowId,
      'image'
    );

    if (response.success) {
      const resourceData = response.data;

      // 创建图片元素获取尺寸
      const img = new Image();
      img.onload = () => {
        // 计算显示尺寸
        const maxSize = 300;
        let displayWidth = img.width;
        let displayHeight = img.height;

        if (displayWidth > maxSize || displayHeight > maxSize) {
          const ratio = Math.min(maxSize / displayWidth, maxSize / displayHeight);
          displayWidth = Math.round(displayWidth * ratio);
          displayHeight = Math.round(displayHeight * ratio);
        }

        // 创建图片组件
        const imageComponent = {
          id: `image_${Date.now()}`,
          type: 'image',
          name: resourceData.fileName,
          position,
          size: { width: displayWidth, height: displayHeight },
          properties: {
            src: resourceData.url, // 使用服务器返回的URL
            alt: resourceData.fileName,
            objectFit: 'contain'
          },
          imageData: {
            fileName: resourceData.fileName,
            filePath: resourceData.filePath,
            url: resourceData.url,
            size: resourceData.size,
            originalWidth: img.width,
            originalHeight: img.height
          }
        };

        addComponentToCanvas(imageComponent);
        ElMessage.success('图片上传成功');
      };

      img.onerror = () => {
        ElMessage.error('图片加载失败');
      };

      // 设置图片源（使用服务器URL）
      img.src = resourceData.url;
    }
  } catch (error) {
    ElMessage.error('上传失败: ' + (error as Error).message);
  } finally {
    loading.value = false;
  }
};
```

### 5.4 导入/导出功能

#### 5.4.1 导出项目

```typescript
// 导出项目为.fuxa文件
const handleExportProject = () => {
  if (!projectInfo.value.SnowId) {
    ElMessage.warning('请先保存项目');
    return;
  }

  // 直接下载文件
  const url = scadaApi.project.exportProject(projectInfo.value.SnowId);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${projectInfo.value.Name}_${Date.now()}.fuxa`;
  link.click();

  ElMessage.success('项目导出中...');
};
```

#### 5.4.2 导入项目

```typescript
// 导入.fuxa项目文件
const handleImportProject = () => {
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = '.fuxa,.zip';

  input.onchange = async (e: Event) => {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;

    try {
      loading.value = true;

      // 确认导入
      await ElMessageBox.confirm(
        `确定要导入项目"${file.name}"吗？当前项目数据将被清空。`,
        '导入项目',
        { type: 'warning' }
      );

      // 调用API导入
      const response = await scadaApi.project.importProject(file);

      if (response.success) {
        ElMessage.success('导入成功，正在加载项目...');

        // 跳转到新导入的项目
        router.push({
          name: 'ScadaEditor',
          params: { id: response.data.id }
        });

        // 重新加载页面
        window.location.reload();
      }
    } catch (error: any) {
      if (error !== 'cancel') {
        ElMessage.error('导入失败: ' + error.message);
      }
    } finally {
      loading.value = false;
    }
  };

  input.click();
};
```

### 5.5 自动保存功能

```typescript
// 自动保存配置
const autoSaveConfig = reactive({
  enabled: true,
  interval: 5 * 60 * 1000, // 5分钟
  timerId: null as number | null
});

// 启动自动保存
const startAutoSave = () => {
  if (!autoSaveConfig.enabled) return;

  // 清除旧定时器
  if (autoSaveConfig.timerId) {
    clearInterval(autoSaveConfig.timerId);
  }

  // 启动新定时器
  autoSaveConfig.timerId = setInterval(() => {
    // 只有在有未保存的修改且项目已有ID时才自动保存
    if (!isSaved.value && projectInfo.value.SnowId) {
      console.log('自动保存中...');
      saveProject();
    }
  }, autoSaveConfig.interval);
};

// 停止自动保存
const stopAutoSave = () => {
  if (autoSaveConfig.timerId) {
    clearInterval(autoSaveConfig.timerId);
    autoSaveConfig.timerId = null;
  }
};

// 组件挂载时启动自动保存
onMounted(() => {
  startAutoSave();
});

// 组件卸载时停止自动保存
onUnmounted(() => {
  stopAutoSave();
});

// 监听离开页面前提示
const handleBeforeUnload = (event: BeforeUnloadEvent) => {
  if (!isSaved.value) {
    event.preventDefault();
    event.returnValue = '当前项目有未保存的修改，确定要离开吗？';
    return event.returnValue;
  }
};

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onUnmounted(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
```

### 5.6 运行时模式实现

#### 5.6.1 创建运行时页面

**文件路径**: `src/views/scada/runtime.vue`

```vue
<template>
  <div class="scada-runtime">
    <div
      class="runtime-canvas"
      :style="{
        width: canvasWidth + 'px',
        height: canvasHeight + 'px',
        backgroundColor: canvasBackgroundColor,
        backgroundImage: canvasBackgroundImage ? `url(${canvasBackgroundImage})` : 'none',
        transform: `scale(${canvasZoom / 100})`,
        transformOrigin: 'top left'
      }"
    >
      <!-- 渲染组件 -->
      <div
        v-for="comp in components"
        :key="comp.id"
        :id="comp.id"
        class="runtime-component"
        :style="{
          position: 'absolute',
          left: comp.position.x + 'px',
          top: comp.position.y + 'px',
          width: comp.size.width + 'px',
          height: comp.size.height + 'px'
        }"
      >
        <!-- 组件内容在这里渲染 -->
      </div>
    </div>

    <!-- 全屏按钮 -->
    <el-button
      class="fullscreen-btn"
      circle
      @click="toggleFullscreen"
    >
      <el-icon><FullScreen /></el-icon>
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { FullScreen } from '@element-plus/icons-vue';
import scadaApi from '@/api/scada';
import { fuxaMqttService } from './core/fuxaMqttService';

const route = useRoute();
const projectId = route.params.id as string;

const canvasWidth = ref(1920);
const canvasHeight = ref(1080);
const canvasZoom = ref(100);
const canvasBackgroundColor = ref('#f5f5f5');
const canvasBackgroundImage = ref('');
const components = ref([]);

// 加载项目数据
const loadProject = async () => {
  try {
    // 1. 获取项目元数据
    const metaResponse = await scadaApi.project.getProjectMeta(projectId);
    if (!metaResponse.success) {
      throw new Error('获取项目元数据失败');
    }

    // 2. 加载项目文件
    const fileResponse = await scadaApi.project.loadProjectFromFile(
      metaResponse.data.FilePath
    );
    if (!fileResponse.success) {
      throw new Error('加载项目文件失败');
    }

    // 3. 解析项目数据
    const projectJson = JSON.parse(fileResponse.data.content);

    // 4. 设置画布
    canvasWidth.value = projectJson.settings?.canvasWidth || 1920;
    canvasHeight.value = projectJson.settings?.canvasHeight || 1080;
    canvasBackgroundColor.value = projectJson.settings?.backgroundColor || '#f5f5f5';
    canvasBackgroundImage.value = projectJson.settings?.backgroundImage || '';

    // 5. 加载组件
    components.value = projectJson.views?.[0]?.components || [];

    // 6. 连接MQTT（如果有设备配置）
    if (projectJson.devices?.length > 0) {
      await connectMqtt(projectJson.devices);
    }

    // 7. 自动缩放适配
    autoScale();

    // 8. 通知父页面加载完成
    notifyParent('SCADA_RUNTIME_LOADED', {
      projectId,
      componentCount: components.value.length
    });

    ElMessage.success('项目加载成功');
  } catch (error) {
    console.error('加载项目失败:', error);
    ElMessage.error('加载失败: ' + (error as Error).message);
    notifyParent('SCADA_RUNTIME_ERROR', { error: (error as Error).message });
  }
};

// 连接MQTT
const connectMqtt = async (devices: any[]) => {
  try {
    await fuxaMqttService.connect();

    // 订阅所有设备的主题
    devices.forEach(device => {
      if (device.type === 'mqtt' && device.enabled) {
        device.connection.topics?.forEach((topic: string) => {
          fuxaMqttService.subscribe(topic, 0);
        });
      }
    });
  } catch (error) {
    console.warn('MQTT连接失败:', error);
  }
};

// 全屏切换
const toggleFullscreen = () => {
  const elem = document.querySelector('.scada-runtime') as HTMLElement;
  if (!document.fullscreenElement) {
    elem.requestFullscreen();
  } else {
    document.exitFullscreen();
  }
};

// 自动缩放适配
const autoScale = () => {
  const container = document.querySelector('.scada-runtime') as HTMLElement;
  if (!container) return;

  const scaleX = container.clientWidth / canvasWidth.value;
  const scaleY = container.clientHeight / canvasHeight.value;
  canvasZoom.value = Math.min(scaleX, scaleY) * 100;
};

// 与父页面通信
const notifyParent = (type: string, data: any) => {
  if (window.parent !== window) {
    window.parent.postMessage({ type, data }, '*');
  }
};

onMounted(async () => {
  await loadProject();
  window.addEventListener('resize', autoScale);
});

onUnmounted(() => {
  window.removeEventListener('resize', autoScale);
  fuxaMqttService.disconnect();
});
</script>

<style scoped lang="scss">
.scada-runtime {
  width: 100%;
  height: 100vh;
  overflow: hidden;
  position: relative;
  background: #f5f5f5;

  .runtime-canvas {
    transition: transform 0.3s ease;
    position: relative;
  }

  .fullscreen-btn {
    position: fixed;
    bottom: 20px;
    right: 20px;
    z-index: 1000;
  }
}
</style>
```

#### 5.6.2 添加运行时路由

**文件路径**: `src/router/modules/scada.ts`

```typescript
export default {
  path: '/scada',
  redirect: '/scada/list',
  meta: {
    icon: 'ri:dashboard-line',
    title: 'SCADA组态',
    rank: 3
  },
  children: [
    {
      path: '/scada/list',
      name: 'ScadaProjectList',
      component: () => import('@/views/scada/list/index.vue'),
      meta: {
        title: '项目列表'
      }
    },
    {
      path: '/scada/editor/:id?',
      name: 'ScadaEditor',
      component: () => import('@/views/scada/index.vue'),
      meta: {
        title: '组态编辑器',
        showLink: false
      }
    },
    {
      path: '/scada/runtime/:id',
      name: 'ScadaRuntime',
      component: () => import('@/views/scada/runtime.vue'),
      meta: {
        title: '运行时',
        showLink: false,
        requiresAuth: false // 运行时模式不需要登录
      }
    }
  ]
} as RouteConfigsTable;
```

---

## 6. 部署方案

### 6.1 开发环境配置

**appsettings.Development.json**:
```json
{
  "ScadaSettings": {
    "ProjectPath": "D:/ScadaProjects_Dev",
    "MaxFileSize": 52428800
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:8848", "http://localhost:5173"]
  }
}
```

### 6.2 生产环境配置

**appsettings.Production.json**:
```json
{
  "ScadaSettings": {
    "ProjectPath": "/var/scada/projects",
    "MaxFileSize": 104857600,
    "EnableCompression": true
  },
  "AllowedHosts": "scada.yourdomain.com",
  "Cors": {
    "AllowedOrigins": ["https://scada.yourdomain.com"]
  }
}
```

### 6.3 部署步骤

#### 步骤1：准备服务器环境

```bash
# 1. 创建项目目录
sudo mkdir -p /var/scada/projects
sudo chmod 755 /var/scada/projects

# 2. 安装.NET Runtime
# (根据操作系统安装.NET 8.0或更高版本)

# 3. 安装Nginx
sudo apt install nginx
```

#### 步骤2：部署后端

```bash
# 1. 发布后端项目
dotnet publish -c Release -o /var/www/scada-api

# 2. 配置systemd服务
sudo nano /etc/systemd/system/scada-api.service
```

**scada-api.service**:
```ini
[Unit]
Description=SCADA API Service
After=network.target

[Service]
WorkingDirectory=/var/www/scada-api
ExecStart=/usr/bin/dotnet /var/www/scada-api/EnergyPlatform.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
# 3. 启动服务
sudo systemctl enable scada-api
sudo systemctl start scada-api
sudo systemctl status scada-api
```

#### 步骤3：部署前端

```bash
# 1. 构建前端项目
pnpm build

# 2. 复制到Nginx目录
sudo cp -r dist/* /var/www/scada-web/
```

#### 步骤4：配置Nginx

**/etc/nginx/sites-available/scada**:
```nginx
server {
    listen 80;
    server_name scada.yourdomain.com;

    # 前端静态资源
    location / {
        root /var/www/scada-web;
        try_files $uri $uri/ /index.html;
    }

    # 后端API代理
    location /api/ {
        proxy_pass http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 项目资源文件
    location /scada-resources/ {
        alias /var/scada/projects/;
        autoindex off;
    }
}
```

```bash
# 启用站点
sudo ln -s /etc/nginx/sites-available/scada /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## 7. 安全策略

### 7.1 认证授权

- **编辑模式**: 需要JWT登录认证
- **运行时模式**: 可选Token认证或公开访问
- **API接口**: 基于角色的访问控制（RBAC）

### 7.2 文件安全

- 限制上传文件类型和大小
- 文件名唯一化处理（避免覆盖）
- 禁止访问系统关键目录
- 定期清理过期备份文件

### 7.3 数据安全

- 敏感信息加密存储（MQTT密码等）
- 定期备份数据库和项目文件
- 实现软删除机制（可恢复）

---

## 8. 测试方案

### 8.1 单元测试

**后端测试**:
```csharp
[TestClass]
public class ScadaProjectControllerTests
{
    [TestMethod]
    public async Task SaveProject_ShouldCreateNewProject()
    {
        // Arrange
        var controller = new ScadaProjectController(...);
        var request = new SaveProjectRequest { ... };

        // Act
        var result = await controller.SaveToFile(request);

        // Assert
        Assert.IsTrue(result.Success);
    }
}
```

### 8.2 集成测试

**API测试脚本**:
```bash
#!/bin/bash
# 测试保存项目
curl -X POST http://localhost:5000/api/scada/project/save-to-file \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试项目",
    "projectData": "..."
  }'
```

### 8.3 性能测试

- 测试1000+组件的项目保存/加载性能
- 测试并发用户访问
- 测试资源文件上传速度

---

## 附录

### A. 常见问题

**Q1: 如何迁移现有项目？**
- 使用导出功能生成.fuxa文件
- 在新环境中导入.fuxa文件

**Q2: 如何备份项目？**
- 方式1：定期备份 `/var/scada/projects` 目录
- 方式2：使用导出功能手动备份

**Q3: 如何扩展存储空间？**
- 修改 `appsettings.json` 中的 `ProjectPath`
- 迁移现有项目到新路径
- 更新数据库中的 `FilePath` 字段

### B. 版本更新日志

**v1.0.0 (2025-01-18)**
- ✅ 数据库索引 + 本地文件存储方案
- ✅ 完整的保存/加载/导入/导出功能
- ✅ 资源文件独立管理
- ✅ 运行时模式支持iframe嵌入

---

**文档结束**
