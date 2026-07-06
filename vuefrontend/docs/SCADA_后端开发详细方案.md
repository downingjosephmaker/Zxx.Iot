# SCADA 独立部署方案 - 后端开发详细文档

**文档版本**: v1.0.0
**创建日期**: 2025-01-18
**技术栈**: .NET Core 6.0+ / SQL Server / C#

---

## 目录

- [1. 概述](#1-概述)
- [2. 数据库设计](#2-数据库设计)
- [3. API接口设计](#3-api接口设计)
- [4. 文件系统设计](#4-文件系统设计)
- [5. 后端代码实现](#5-后端代码实现)
- [6. 配置说明](#6-配置说明)
- [7. 安全措施](#7-安全措施)
- [8. 错误处理](#8-错误处理)
- [9. 测试用例](#9-测试用例)
- [10. 部署说明](#10-部署说明)

---

## 1. 概述

### 1.1 架构设计

```
┌─────────────────────────────────────────────────┐
│                  前端 Vue 3                      │
│  - index.vue (编辑器模式)                        │
│  - runtime.vue (运行时模式)                      │
└─────────────────────────────────────────────────┘
                      ↓ HTTPS
┌─────────────────────────────────────────────────┐
│              .NET Core Web API                   │
│  ├─ ScadaProjectController (项目管理)           │
│  └─ ScadaResourceController (资源管理)          │
└─────────────────────────────────────────────────┘
         ↓                           ↓
┌──────────────────┐      ┌──────────────────────┐
│  SQL Server      │      │   本地文件系统        │
│  - ScadaProject  │      │  D:/ScadaProjects/   │
│  (元数据索引)    │      │  (项目JSON数据)       │
└──────────────────┘      └──────────────────────┘
```

### 1.2 核心特性

- ✅ 数据库存储元数据，文件系统存储项目数据
- ✅ JSON 格式存储，便于阅读和维护
- ✅ 支持导入/导出 .fuxa 文件
- ✅ 资源文件独立管理（图片/视频/SVG）
- ✅ RESTful API 设计

---

## 2. 数据库设计

### 2.1 ScadaProject 表结构

```sql
CREATE TABLE [dbo].[ScadaProject] (
    -- 主键
    [Id] BIGINT NOT NULL PRIMARY KEY,

    -- 基本信息
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,

    -- 文件路径（核心字段）
    [FilePath] NVARCHAR(500) NOT NULL,           -- 项目文件夹路径，如: D:/ScadaProjects/project_123
    [FileName] NVARCHAR(200) DEFAULT 'project.json',  -- 项目文件名

    -- 版本信息
    [Version] NVARCHAR(50) DEFAULT '1.0.0',

    -- 状态
    [Status] INT NOT NULL DEFAULT 0,              -- 0:草稿, 1:发布, 2:归档

    -- 统计信息
    [ComponentCount] INT DEFAULT 0,               -- 组件数量
    [FileSize] BIGINT DEFAULT 0,                  -- 文件大小（字节）

    -- 缩略图
    [ThumbnailPath] NVARCHAR(500) NULL,           -- 缩略图路径

    -- 审计字段
    [CreatedBy] BIGINT NOT NULL,
    [ModifiedBy] BIGINT NULL,
    [CreateTime] DATETIME2 DEFAULT GETDATE(),
    [UpdateTime] DATETIME2 NULL,
    [LastOpenTime] DATETIME2 NULL,                -- 最后打开时间

    -- 软删除
    [IsDeleted] BIT DEFAULT 0
);

-- 创建索引
CREATE INDEX IX_ScadaProject_Name ON ScadaProject(Name);
CREATE INDEX IX_ScadaProject_Status ON ScadaProject(Status);
CREATE INDEX IX_ScadaProject_CreateTime ON ScadaProject(CreateTime DESC);
CREATE INDEX IX_ScadaProject_IsDeleted ON ScadaProject(IsDeleted);

-- 创建复合索引（分页查询优化）
CREATE INDEX IX_ScadaProject_Status_CreateTime ON ScadaProject(Status, CreateTime DESC) WHERE IsDeleted = 0;
```

### 2.2 字段说明

| 字段名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|------|------|--------|
| Id | BIGINT | ✅ | 雪花ID主键 | 1234567890 |
| Name | NVARCHAR(200) | ✅ | 项目名称 | "生产线监控系统" |
| Description | NVARCHAR(500) | ❌ | 项目描述 | "第一车间生产线实时监控" |
| FilePath | NVARCHAR(500) | ✅ | **项目文件夹路径** | "D:/ScadaProjects/project_123" |
| FileName | NVARCHAR(200) | ❌ | 项目文件名，默认 project.json | "project.json" |
| Version | NVARCHAR(50) | ❌ | 版本号 | "1.0.0" |
| Status | INT | ✅ | 状态：0草稿/1发布/2归档 | 0 |
| ComponentCount | INT | ❌ | 组件数量 | 56 |
| FileSize | BIGINT | ❌ | 文件大小（字节） | 2458624 |
| ThumbnailPath | NVARCHAR(500) | ❌ | 缩略图路径 | "D:/ScadaProjects/project_123/thumbnail.png" |
| CreatedBy | BIGINT | ✅ | 创建人ID | 1001 |
| ModifiedBy | BIGINT | ❌ | 修改人ID | 1001 |
| CreateTime | DATETIME2 | ❌ | 创建时间 | "2025-01-18 10:30:00" |
| UpdateTime | DATETIME2 | ❌ | 更新时间 | "2025-01-18 12:30:00" |
| LastOpenTime | DATETIME2 | ❌ | 最后打开时间 | "2025-01-18 15:00:00" |
| IsDeleted | BIT | ❌ | 是否删除，默认0 | 0 |

### 2.3 初始化数据

```sql
-- 插入测试数据
INSERT INTO ScadaProject (Id, Name, Description, FilePath, FileName, Version, Status, ComponentCount, FileSize, CreatedBy, CreateTime)
VALUES
(1001, '示例项目', '这是一个示例项目', 'D:/ScadaProjects/project_1001', 'project.json', '1.0.0', 0, 0, 0, 1, GETDATE());
```

---

## 3. API接口设计

### 3.1 基础响应模型

```csharp
// 统一响应模型
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; } // HTTP状态码
}

// 分页响应模型
public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<T> Data { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

### 3.2 项目管理接口

#### 3.2.1 分页查询项目列表

**接口**: `POST /api/scada/project/list`

**请求体**:
```json
{
  "keyword": "监控",
  "status": 0,
  "pageIndex": 1,
  "pageSize": 20,
  "orderBy": "CreateTime",
  "orderDirection": "DESC"
}
```

**请求参数说明**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| keyword | string | ❌ | 搜索关键词（项目名称、描述） |
| status | int | ❌ | 状态筛选（0:草稿/1:发布/2:归档） |
| pageIndex | int | ❌ | 页码，默认1 |
| pageSize | int | ❌ | 每页数量，默认20 |
| orderBy | string | ❌ | 排序字段，默认CreateTime |
| orderDirection | string | ❌ | 排序方向（ASC/DESC），默认DESC |

**响应**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": 1001,
      "name": "生产线监控",
      "description": "第一车间",
      "filePath": "D:/ScadaProjects/project_1001",
      "version": "1.0.0",
      "status": 0,
      "componentCount": 56,
      "fileSize": 2458624,
      "thumbnailPath": null,
      "createTime": "2025-01-18T10:30:00",
      "updateTime": "2025-01-18T12:30:00",
      "lastOpenTime": "2025-01-18T15:00:00"
    }
  ],
  "totalCount": 100,
  "pageIndex": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

#### 3.2.2 获取项目元数据

**接口**: `GET /api/scada/project/get-meta/{id}`

**路径参数**:
- `id` (BIGINT): 项目ID

**响应**:
```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "id": 1001,
    "name": "生产线监控",
    "description": "第一车间",
    "filePath": "D:/ScadaProjects/project_1001",
    "fileName": "project.json",
    "version": "1.0.0",
    "status": 0,
    "componentCount": 56,
    "fileSize": 2458624,
    "createTime": "2025-01-18T10:30:00",
    "updateTime": "2025-01-18T12:30:00"
  }
}
```

---

#### 3.2.3 保存项目到本地文件

**接口**: `POST /api/scada/project/save-to-file`

**请求体**:
```json
{
  "id": 1001,
  "name": "生产线监控",
  "description": "第一车间生产线实时监控",
  "projectData": "{\"version\":\"1.0.0\",\"info\":{...},\"settings\":{...},\"views\":[...]}",
  "version": "1.0.0",
  "status": 0,
  "componentCount": 56
}
```

**请求参数说明**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| id | long | ❌ | 项目ID（新建时不传） |
| name | string | ✅ | 项目名称 |
| description | string | ❌ | 项目描述 |
| projectData | string | ✅ | 项目JSON数据（字符串） |
| version | string | ❌ | 版本号，默认1.0.0 |
| status | int | ❌ | 状态，默认0 |
| componentCount | int | ❌ | 组件数量 |

**projectData JSON 结构示例**:
```json
{
  "version": "1.0.0",
  "info": {
    "id": 1001,
    "name": "生产线监控",
    "description": "第一车间",
    "created": "2025-01-18T10:30:00",
    "modified": "2025-01-18T12:30:00",
    "author": "admin",
    "tags": []
  },
  "settings": {
    "canvasWidth": 1920,
    "canvasHeight": 1080,
    "gridSize": 20,
    "showGrid": true,
    "enableSnap": true,
    "backgroundColor": "#f5f5f5",
    "backgroundImage": ""
  },
  "views": [
    {
      "id": "view_1",
      "name": "主画面",
      "description": "",
      "components": [
        {
          "id": "comp_001",
          "type": "text",
          "name": "标题",
          "position": { "x": 100, "y": 100 },
          "size": { "width": 200, "height": 50 },
          "properties": {
            "text": "生产线监控",
            "fontSize": 24,
            "color": "#000000"
          }
        }
      ]
    }
  ],
  "devices": [],
  "datasets": [],
  "resources": {
    "images": [],
    "videos": [],
    "svgs": []
  }
}
```

**响应**:
```json
{
  "success": true,
  "message": "项目保存成功",
  "data": {
    "id": 1001,
    "filePath": "D:/ScadaProjects/project_1001",
    "fileSize": 2458624,
    "updateTime": "2025-01-18T12:30:00"
  }
}
```

---

#### 3.2.4 从本地文件加载项目

**接口**: `GET /api/scada/project/load-from-file`

**查询参数**:
- `filePath` (string): 文件路径，如 "D:/ScadaProjects/project_1001"

**响应**:
```json
{
  "success": true,
  "message": "加载成功",
  "data": {
    "filePath": "D:/ScadaProjects/project_1001/project.json",
    "content": "{\"version\":\"1.0.0\",\"info\":{...},\"settings\":{...}}",
    "size": 2458624,
    "lastModified": "2025-01-18T12:30:00"
  }
}
```

---

#### 3.2.5 删除项目（软删除）

**接口**: `DELETE /api/scada/project/delete/{id}`

**路径参数**:
- `id` (BIGINT): 项目ID

**响应**:
```json
{
  "success": true,
  "message": "删除成功",
  "data": {
    "id": 1001,
    "isDeleted": true
  }
}
```

---

#### 3.2.6 导出项目为 .fuxa 文件

**接口**: `GET /api/scada/project/export/{id}`

**路径参数**:
- `id` (BIGINT): 项目ID

**响应**: 直接返回文件流（application/zip）

**文件名格式**: `项目名称_时间戳.fuxa`

**压缩包内容结构**:
```
项目名称.fuxa (ZIP文件)
├── project.json         (项目数据)
├── thumbnail.png        (缩略图，可选)
└── resources/           (资源文件夹)
    ├── images/
    ├── videos/
    └── svgs/
```

---

#### 3.2.7 导入 .fuxa 项目文件

**接口**: `POST /api/scada/project/import`

**请求**: multipart/form-data

**表单参数**:
- `file` (File): .fuxa 文件

**响应**:
```json
{
  "success": true,
  "message": "导入成功",
  "data": {
    "id": 1002,
    "name": "导入的项目",
    "filePath": "D:/ScadaProjects/project_1002",
    "componentCount": 56,
    "fileSize": 2458624
  }
}
```

---

#### 3.2.8 更新最后打开时间

**接口**: `POST /api/scada/project/update-last-open-time/{id}`

**路径参数**:
- `id` (BIGINT): 项目ID

**响应**:
```json
{
  "success": true,
  "message": "更新成功",
  "data": {
    "id": 1001,
    "lastOpenTime": "2025-01-18T15:00:00"
  }
}
```

---

### 3.3 资源管理接口

#### 3.3.1 上传资源文件

**接口**: `POST /api/scada/resources/upload`

**请求**: multipart/form-data

**表单参数**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| file | File | ✅ | 文件对象 |
| projectId | long | ✅ | 项目ID |
| resourceType | string | ✅ | 资源类型: "image" / "video" / "svg" |

**响应**:
```json
{
  "success": true,
  "message": "上传成功",
  "data": {
    "fileName": "logo.png",
    "filePath": "D:/ScadaProjects/project_1001/resources/images/logo.png",
    "url": "/api/scada/resources/get/1001/image/logo.png",
    "size": 102400,
    "resourceType": "image",
    "uploadTime": "2025-01-18T15:30:00"
  }
}
```

**文件类型限制**:

| 资源类型 | 允许的扩展名 | 最大文件大小 |
|---------|------------|-------------|
| image | .jpg, .jpeg, .png, .gif, .webp | 10 MB |
| video | .mp4, .webm, .ogg | 100 MB |
| svg | .svg | 5 MB |

---

#### 3.3.2 获取资源文件

**接口**: `GET /api/scada/resources/get/{projectId}/{resourceType}/{fileName}`

**路径参数**:
- `projectId` (long): 项目ID
- `resourceType` (string): 资源类型（image/video/svg）
- `fileName` (string): 文件名

**示例**: `/api/scada/resources/get/1001/image/logo.png`

**响应**: 直接返回文件流（image/png、video/mp4 等）

---

## 4. 文件系统设计

### 4.1 目录结构

```
D:/ScadaProjects/                           # 根目录（可配置）
│
├─ project_1001/                            # 项目文件夹（ID: 1001）
│   ├─ project.json                         # 项目主文件
│   ├─ project.meta.json                    # 元数据文件（可选）
│   ├─ thumbnail.png                        # 缩略图（400x300）
│   │
│   └─ resources/                           # 资源文件夹
│       ├─ images/                          # 图片资源
│       │   ├─ logo.png
│       │   ├─ background.jpg
│       │   └─ icon_01.svg
│       ├─ videos/                          # 视频资源
│       │   └─ intro.mp4
│       └─ svgs/                            # SVG资源
│           └─ custom-gauge.svg
│
├─ project_1002/                            # 另一个项目
│   └─ ...
│
└─ .config/                                 # 全局配置
    ├─ templates/                           # 项目模板
    └─ shared-resources/                    # 共享资源库
```

### 4.2 project.json 文件格式

完整的项目JSON数据结构（参见 3.2.3 节）。

### 4.3 文件路径生成规则

```csharp
// 项目文件夹路径
string projectFolder = Path.Combine(rootPath, $"project_{projectId}");

// 项目文件路径
string projectFilePath = Path.Combine(projectFolder, "project.json");

// 资源文件路径
string resourcePath = Path.Combine(projectFolder, "resources", resourceType, fileName);
```

---

## 5. 后端代码实现

### 5.1 项目结构

```
EnergyPlatform.Api/
├── Controllers/
│   ├── ScadaProjectController.cs         # 项目管理控制器
│   └── ScadaResourceController.cs        # 资源管理控制器
├── Models/
│   ├── ScadaProject.cs                   # 项目实体
│   ├── Request/
│   │   ├── SaveProjectRequest.cs
│   │   └── ProjectQueryRequest.cs
│   └── Response/
│       ├── ProjectMetaResponse.cs
│       └── ResourceUploadResponse.cs
├── Services/
│   ├── IScadaProjectService.cs           # 项目服务接口
│   └── ScadaProjectService.cs            # 项目服务实现
└── appsettings.json                      # 配置文件
```

### 5.2 实体模型

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnergyPlatform.Api.Models
{
    [Table("ScadaProject")]
    public class ScadaProject
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        [MaxLength(200)]
        public string FileName { get; set; } = "project.json";

        [MaxLength(50)]
        public string Version { get; set; } = "1.0.0";

        public int Status { get; set; } = 0;

        public int ComponentCount { get; set; } = 0;

        public long FileSize { get; set; } = 0;

        [MaxLength(500)]
        public string ThumbnailPath { get; set; }

        [Required]
        public long CreatedBy { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public DateTime? UpdateTime { get; set; }

        public DateTime? LastOpenTime { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
```

### 5.3 请求模型

```csharp
namespace EnergyPlatform.Api.Models.Request
{
    // 保存项目请求
    public class SaveProjectRequest
    {
        public long? Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public string ProjectData { get; set; }

        [MaxLength(50)]
        public string Version { get; set; } = "1.0.0";

        public int Status { get; set; } = 0;

        public int ComponentCount { get; set; } = 0;
    }

    // 项目查询请求
    public class ProjectQueryRequest
    {
        public string Keyword { get; set; }

        public int? Status { get; set; }

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string OrderBy { get; set; } = "CreateTime";

        public string OrderDirection { get; set; } = "DESC";
    }
}
```

### 5.4 响应模型

```csharp
namespace EnergyPlatform.Api.Models.Response
{
    // 项目元数据响应
    public class ProjectMetaResponse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Version { get; set; }
        public int Status { get; set; }
        public int ComponentCount { get; set; }
        public long FileSize { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    // 资源上传响应
    public class ResourceUploadResponse
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
        public string ResourceType { get; set; }
        public DateTime UploadTime { get; set; }
    }

    // 保存项目响应
    public class SaveProjectResponse
    {
        public long Id { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    // 加载项目响应
    public class LoadProjectResponse
    {
        public string FilePath { get; set; }
        public string Content { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}
```

### 5.5 ScadaProjectController.cs（完整实现）

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyPlatform.Api.Models;
using EnergyPlatform.Api.Models.Request;
using EnergyPlatform.Api.Models.Response;

namespace EnergyPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/scada/project")]
    public class ScadaProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ScadaProjectController> _logger;
        private readonly string _projectRootPath;

        public ScadaProjectController(
            ApplicationDbContext db,
            ILogger<ScadaProjectController> logger,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _projectRootPath = configuration["ScadaSettings:ProjectPath"] ?? "D:/ScadaProjects";
        }

        /// <summary>
        /// 分页查询项目列表
        /// </summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetProjectList([FromBody] ProjectQueryRequest request)
        {
            try
            {
                var query = _db.ScadaProjects.Where(p => !p.IsDeleted);

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
                query = request.OrderDirection.ToUpper() == "ASC"
                    ? query.OrderBy(p => EF.Property<object>(p, request.OrderBy))
                    : query.OrderByDescending(p => EF.Property<object>(p, request.OrderBy));

                // 分页
                var projects = await query
                    .Skip((request.PageIndex - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new ProjectMetaResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        FilePath = p.FilePath,
                        FileName = p.FileName,
                        Version = p.Version,
                        Status = p.Status,
                        ComponentCount = p.ComponentCount,
                        FileSize = p.FileSize,
                        CreateTime = p.CreateTime,
                        UpdateTime = p.UpdateTime
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "查询成功",
                    data = projects,
                    totalCount = totalCount,
                    pageIndex = request.PageIndex,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询项目列表失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "查询失败: " + ex.Message
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目不存在"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "获取成功",
                    data = new ProjectMetaResponse
                    {
                        Id = project.Id,
                        Name = project.Name,
                        Description = project.Description,
                        FilePath = project.FilePath,
                        FileName = project.FileName,
                        Version = project.Version,
                        Status = project.Status,
                        ComponentCount = project.ComponentCount,
                        FileSize = project.FileSize,
                        CreateTime = project.CreateTime,
                        UpdateTime = project.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取项目元数据失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取失败: " + ex.Message
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
                // 1. 查询或创建项目
                ScadaProject project;
                bool isNew = false;

                if (request.Id.HasValue)
                {
                    project = await _db.ScadaProjects.FindAsync(request.Id.Value);
                    if (project == null)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "项目不存在"
                        });
                    }
                }
                else
                {
                    // 新建项目
                    project = new ScadaProject
                    {
                        Id = GenerateSnowflakeId(), // 雪花ID生成
                        CreatedBy = GetCurrentUserId(),
                        CreateTime = DateTime.Now
                    };
                    isNew = true;
                }

                // 2. 更新基本信息
                project.Name = request.Name;
                project.Description = request.Description;
                project.Version = request.Version ?? "1.0.0";
                project.Status = request.Status;
                project.ComponentCount = request.ComponentCount;
                project.ModifiedBy = GetCurrentUserId();
                project.UpdateTime = DateTime.Now;

                // 3. 确定文件保存路径
                var projectFolder = Path.Combine(_projectRootPath, $"project_{project.Id}");
                var projectFilePath = Path.Combine(projectFolder, "project.json");

                project.FilePath = projectFolder;
                project.FileName = "project.json";

                // 4. 创建项目文件夹结构
                Directory.CreateDirectory(projectFolder);
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "images"));
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "videos"));
                Directory.CreateDirectory(Path.Combine(projectFolder, "resources", "svgs"));

                // 5. 写入项目文件（格式化JSON便于阅读）
                await System.IO.File.WriteAllTextAsync(
                    projectFilePath,
                    request.ProjectData,
                    Encoding.UTF8
                );

                // 6. 更新文件大小
                var fileInfo = new FileInfo(projectFilePath);
                project.FileSize = fileInfo.Length;

                // 7. 保存到数据库
                if (isNew)
                    await _db.ScadaProjects.AddAsync(project);

                await _db.SaveChangesAsync();

                _logger.LogInformation($"项目保存成功: {project.Name} (ID: {project.Id})");

                return Ok(new
                {
                    success = true,
                    message = "项目保存成功",
                    data = new SaveProjectResponse
                    {
                        Id = project.Id,
                        FilePath = projectFolder,
                        FileSize = project.FileSize,
                        UpdateTime = project.UpdateTime.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存项目失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "保存失败: " + ex.Message
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目文件不存在"
                    });
                }

                var content = await System.IO.File.ReadAllTextAsync(projectFilePath, Encoding.UTF8);
                var fileInfo = new FileInfo(projectFilePath);

                return Ok(new
                {
                    success = true,
                    message = "加载成功",
                    data = new LoadProjectResponse
                    {
                        FilePath = projectFilePath,
                        Content = content,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载项目失败: {filePath}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "加载失败: " + ex.Message
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目不存在"
                    });
                }

                // 软删除
                project.IsDeleted = true;
                project.UpdateTime = DateTime.Now;
                project.ModifiedBy = GetCurrentUserId();

                await _db.SaveChangesAsync();

                _logger.LogInformation($"项目删除成功: {id}");

                return Ok(new
                {
                    success = true,
                    message = "删除成功",
                    data = new
                    {
                        id = id,
                        isDeleted = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除项目失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "删除失败: " + ex.Message
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目不存在"
                    });
                }

                var projectFolder = project.FilePath;
                if (!Directory.Exists(projectFolder))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目文件夹不存在"
                    });
                }

                // 创建临时ZIP文件
                var tempZipPath = Path.Combine(Path.GetTempPath(), $"{project.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.fuxa");

                // 压缩项目文件夹
                if (System.IO.File.Exists(tempZipPath))
                    System.IO.File.Delete(tempZipPath);

                ZipFile.CreateFromDirectory(projectFolder, tempZipPath);

                // 返回文件流
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempZipPath);

                // 删除临时文件
                System.IO.File.Delete(tempZipPath);

                _logger.LogInformation($"项目导出成功: {id}");

                return File(fileBytes, "application/zip", $"{project.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.fuxa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出项目失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "导出失败: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 导入.fuxa项目文件
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> ImportProject(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "请选择文件"
                    });
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".fuxa" && extension != ".zip")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "只支持.fuxa或.zip文件"
                    });
                }

                // 创建临时文件
                var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 解压到临时目录
                var tempExtractPath = Path.Combine(Path.GetTempPath(), $"scada_import_{Guid.NewGuid()}");
                ZipFile.ExtractToDirectory(tempFilePath, tempExtractPath);

                // 读取project.json
                var projectJsonPath = Path.Combine(tempExtractPath, "project.json");
                if (!System.IO.File.Exists(projectJsonPath))
                {
                    // 清理临时文件
                    Directory.Delete(tempExtractPath, true);
                    System.IO.File.Delete(tempFilePath);

                    return BadRequest(new
                    {
                        success = false,
                        message = "无效的项目文件：缺少project.json"
                    });
                }

                var projectJsonContent = await System.IO.File.ReadAllTextAsync(projectJsonPath, Encoding.UTF8);
                var projectJson = System.Text.Json.JsonDocument.Parse(projectJsonContent);

                // 创建新项目
                var newProject = new ScadaProject
                {
                    Id = GenerateSnowflakeId(),
                    Name = projectJson.RootElement.GetProperty("info").GetProperty("name").GetString() ?? "导入的项目",
                    Description = projectJson.RootElement.GetProperty("info").GetProperty("description").GetString(),
                    Version = projectJson.RootElement.GetProperty("version").GetString() ?? "1.0.0",
                    Status = 0,
                    CreatedBy = GetCurrentUserId(),
                    CreateTime = DateTime.Now
                };

                // 统计组件数量
                if (projectJson.RootElement.TryGetProperty("views", out var views) &&
                    views.GetArrayLength() > 0)
                {
                    var firstView = views[0];
                    if (firstView.TryGetProperty("components", out var components))
                    {
                        newProject.ComponentCount = components.GetArrayLength();
                    }
                }

                // 目标文件夹
                var targetFolder = Path.Combine(_projectRootPath, $"project_{newProject.Id}");
                newProject.FilePath = targetFolder;
                newProject.FileName = "project.json";

                // 移动文件夹
                CopyDirectory(tempExtractPath, targetFolder);

                // 获取文件大小
                var fileInfo = new FileInfo(Path.Combine(targetFolder, "project.json"));
                newProject.FileSize = fileInfo.Length;

                // 保存到数据库
                await _db.ScadaProjects.AddAsync(newProject);
                await _db.SaveChangesAsync();

                // 清理临时文件
                Directory.Delete(tempExtractPath, true);
                System.IO.File.Delete(tempFilePath);

                _logger.LogInformation($"项目导入成功: {newProject.Name} (ID: {newProject.Id})");

                return Ok(new
                {
                    success = true,
                    message = "导入成功",
                    data = new
                    {
                        id = newProject.Id,
                        name = newProject.Name,
                        filePath = newProject.FilePath,
                        componentCount = newProject.ComponentCount,
                        fileSize = newProject.FileSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入项目失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "导入失败: " + ex.Message
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "项目不存在"
                    });
                }

                project.LastOpenTime = DateTime.Now;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "更新成功",
                    data = new
                    {
                        id = id,
                        lastOpenTime = project.LastOpenTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新最后打开时间失败: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "更新失败: " + ex.Message
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
        /// 生成雪花ID
        /// </summary>
        private long GenerateSnowflakeId()
        {
            // TODO: 实现雪花ID算法或使用第三方库
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 递归复制文件夹
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                System.IO.File.Copy(file, destFile, true);
            }

            // 递归复制子文件夹
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destSubDir);
            }
        }
    }
}
```

### 5.6 ScadaResourceController.cs（完整实现）

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnergyPlatform.Api.Models.Response;

namespace EnergyPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/scada/resources")]
    public class ScadaResourceController : ControllerBase
    {
        private readonly ILogger<ScadaResourceController> _logger;
        private readonly string _projectRootPath;
        private readonly IConfiguration _configuration;

        // 允许的文件类型
        private readonly string[] _allowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedVideoTypes = { ".mp4", ".webm", ".ogg" };
        private readonly string[] _allowedSvgTypes = { ".svg" };

        // 文件大小限制（字节）
        private readonly long _maxImageSize = 10 * 1024 * 1024;      // 10MB
        private readonly long _maxVideoSize = 100 * 1024 * 1024;     // 100MB
        private readonly long _maxSvgSize = 5 * 1024 * 1024;         // 5MB

        public ScadaResourceController(
            ILogger<ScadaResourceController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _projectRootPath = configuration["ScadaSettings:ProjectPath"] ?? "D:/ScadaProjects";
        }

        /// <summary>
        /// 上传资源文件
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB
        public async Task<IActionResult> UploadResource(
            [FromForm] IFormFile file,
            [FromForm] long projectId,
            [FromForm] string resourceType)
        {
            try
            {
                // 1. 验证参数
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "请选择文件"
                    });
                }

                if (string.IsNullOrWhiteSpace(resourceType))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "请指定资源类型"
                    });
                }

                resourceType = resourceType.ToLower();
                if (resourceType != "image" && resourceType != "video" && resourceType != "svg")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "不支持的资源类型"
                    });
                }

                // 2. 验证文件扩展名
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = resourceType switch
                {
                    "image" => _allowedImageTypes,
                    "video" => _allowedVideoTypes,
                    "svg" => _allowedSvgTypes,
                    _ => Array.Empty<string>()
                };

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"不支持的文件格式: {extension}"
                    });
                }

                // 3. 验证文件大小
                var maxSize = resourceType switch
                {
                    "image" => _maxImageSize,
                    "video" => _maxVideoSize,
                    "svg" => _maxSvgSize,
                    _ => 0
                };

                if (file.Length > maxSize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"文件大小超过限制: {maxSize / 1024 / 1024}MB"
                    });
                }

                // 4. 确定保存路径
                var projectFolder = Path.Combine(_projectRootPath, $"project_{projectId}");
                var resourceFolder = Path.Combine(projectFolder, "resources", $"{resourceType}s");

                if (!Directory.Exists(resourceFolder))
                {
                    Directory.CreateDirectory(resourceFolder);
                }

                // 5. 生成文件名（避免重名）
                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var filePath = Path.Combine(resourceFolder, fileName);

                // 6. 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 7. 生成访问URL
                var url = $"/api/scada/resources/get/{projectId}/{resourceType}/{fileName}";

                _logger.LogInformation($"资源上传成功: {fileName} (项目ID: {projectId})");

                return Ok(new
                {
                    success = true,
                    message = "上传成功",
                    data = new ResourceUploadResponse
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        Url = url,
                        Size = file.Length,
                        ResourceType = resourceType,
                        UploadTime = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传资源失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "上传失败: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 获取资源文件
        /// </summary>
        [HttpGet("get/{projectId}/{resourceType}/{fileName}")]
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
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "文件不存在"
                    });
                }

                // 确定Content-Type
                var contentType = GetContentType(Path.GetExtension(fileName));

                // 返回文件流
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取资源失败: {fileName}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取失败: " + ex.Message
                });
            }
        }

        // ========== 私有方法 ==========

        /// <summary>
        /// 根据文件扩展名获取Content-Type
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

---

## 6. 配置说明

### 6.1 appsettings.json

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
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EnergyPlatform;User Id=sa;Password=YourPassword;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "EnergyPlatform.Api.Controllers.ScadaProjectController": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

### 6.2 appsettings.Development.json

```json
{
  "ScadaSettings": {
    "ProjectPath": "D:/ScadaProjects_Dev",
    "MaxFileSize": 52428800
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EnergyPlatform_Dev;User Id=sa;Password=DevPassword;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### 6.3 appsettings.Production.json

```json
{
  "ScadaSettings": {
    "ProjectPath": "/var/scada/projects",
    "MaxFileSize": 104857600,
    "EnableCompression": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=EnergyPlatform;User Id=prod_user;Password=ProdPassword;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 7. 安全措施

### 7.1 身份验证

```csharp
// Program.cs 或 Startup.cs 中配置JWT认证

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
    });

// 控制器添加认证特性
[Authorize]
[ApiController]
[Route("api/scada/project")]
public class ScadaProjectController : ControllerBase
{
    // ...
}
```

### 7.2 文件安全

```csharp
// 文件路径验证（防止路径穿越攻击）
private bool IsValidPath(string path)
{
    var fullPath = Path.GetFullPath(path);
    return fullPath.StartsWith(_projectRootPath);
}

// 文件名验证
private bool IsValidFileName(string fileName)
{
    var invalidChars = Path.GetInvalidFileNameChars();
    return !fileName.Any(c => invalidChars.Contains(c)) &&
           !fileName.Contains("..") &&
           !fileName.StartsWith("/") &&
           !fileName.StartsWith("\\");
}
```

### 7.3 CORS 配置

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ScadaPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:8848", "https://yourdomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("ScadaPolicy");
```

### 7.4 输入验证

```csharp
// 使用Data Annotations
public class SaveProjectRequest
{
    [Required(ErrorMessage = "项目名称不能为空")]
    [StringLength(200, ErrorMessage = "项目名称不能超过200个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "项目数据不能为空")]
    [MinLength(10, ErrorMessage = "项目数据格式不正确")]
    public string ProjectData { get; set; }
}
```

---

## 8. 错误处理

### 8.1 全局异常处理中间件

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            success = false,
            message = "服务器内部错误",
            error = exception.Message,
            stackTrace = exception.StackTrace // 仅开发环境
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}

// Program.cs 注册中间件
app.UseMiddleware<GlobalExceptionMiddleware>();
```

### 8.2 常见错误码

| 错误码 | 说明 | 示例 |
|-------|------|------|
| 400 | 请求参数错误 | 缺少必填字段、格式不正确 |
| 401 | 未授权 | Token过期、未登录 |
| 403 | 无权限 | 无该项目的访问权限 |
| 404 | 资源不存在 | 项目ID不存在、文件不存在 |
| 413 | 文件过大 | 超过文件大小限制 |
| 500 | 服务器错误 | 数据库连接失败、文件系统错误 |

---

## 9. 测试用例

### 9.1 API 测试（Postman）

#### 测试1：保存新项目

```http
POST /api/scada/project/save-to-file
Content-Type: application/json

{
  "name": "测试项目",
  "description": "这是一个测试项目",
  "projectData": "{\"version\":\"1.0.0\",\"info\":{\"name\":\"测试项目\"},\"settings\":{},\"views\":[],\"devices\":[],\"datasets\":[]}",
  "version": "1.0.0",
  "status": 0,
  "componentCount": 0
}
```

**预期结果**:
- HTTP 200 OK
- 返回项目ID、文件路径、文件大小
- 数据库中创建新记录
- 文件系统创建项目文件夹和project.json

---

#### 测试2：加载项目

```http
GET /api/scada/project/load-from-file?filePath=D:/ScadaProjects/project_1001
```

**预期结果**:
- HTTP 200 OK
- 返回项目JSON内容、文件大小、最后修改时间

---

#### 测试3：上传图片资源

```http
POST /api/scada/resources/upload
Content-Type: multipart/form-data

file: [选择图片文件]
projectId: 1001
resourceType: image
```

**预期结果**:
- HTTP 200 OK
- 返回文件名、URL、文件大小
- 文件保存到 D:/ScadaProjects/project_1001/resources/images/

---

#### 测试4：导出项目

```http
GET /api/scada/project/export/1001
```

**预期结果**:
- HTTP 200 OK
- Content-Type: application/zip
- 下载.fuxa文件

---

### 9.2 单元测试（xUnit）

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using EnergyPlatform.Api.Controllers;
using EnergyPlatform.Api.Models.Request;

namespace EnergyPlatform.Tests
{
    public class ScadaProjectControllerTests
    {
        private readonly Mock<ApplicationDbContext> _dbMock;
        private readonly Mock<ILogger<ScadaProjectController>> _loggerMock;
        private readonly ScadaProjectController _controller;

        public ScadaProjectControllerTests()
        {
            _dbMock = new Mock<ApplicationDbContext>();
            _loggerMock = new Mock<ILogger<ScadaProjectController>>();
            _controller = new ScadaProjectController(_dbMock.Object, _loggerMock.Object, null);
        }

        [Fact]
        public async Task SaveProject_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new SaveProjectRequest
            {
                Name = "测试项目",
                ProjectData = "{\"version\":\"1.0.0\"}"
            };

            // Act
            var result = await _controller.SaveToFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetProjectMeta_InvalidId_ReturnsNotFound()
        {
            // Arrange
            long invalidId = 9999;

            // Act
            var result = await _controller.GetProjectMeta(invalidId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
```

---

## 10. 部署说明

### 10.1 Windows 部署

#### 步骤1：发布项目

```bash
dotnet publish -c Release -o ./publish
```

#### 步骤2：配置 IIS

1. 安装 ASP.NET Core Hosting Bundle
2. 创建应用程序池（.NET CLR版本：无托管代码）
3. 添加网站，物理路径指向发布目录
4. 配置应用程序池身份（需要文件系统访问权限）

#### 步骤3：创建项目根目录

```powershell
New-Item -Path "D:\ScadaProjects" -ItemType Directory
icacls "D:\ScadaProjects" /grant "IIS AppPool\YourAppPool:(OI)(CI)F"
```

---

### 10.2 Linux 部署

#### 步骤1：发布项目

```bash
dotnet publish -c Release -o ./publish
```

#### 步骤2：上传文件

```bash
scp -r ./publish user@server:/var/www/scada-api
```

#### 步骤3：配置 Systemd 服务

```ini
# /etc/systemd/system/scada-api.service
[Unit]
Description=SCADA API Service
After=network.target

[Service]
WorkingDirectory=/var/www/scada-api
ExecStart=/usr/bin/dotnet /var/www/scada-api/EnergyPlatform.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=scada-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

#### 步骤4：启动服务

```bash
sudo systemctl enable scada-api
sudo systemctl start scada-api
sudo systemctl status scada-api
```

#### 步骤5：配置 Nginx 反向代理

```nginx
server {
    listen 80;
    server_name scada-api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 静态资源路径（可选）
    location /api/scada/resources/ {
        alias /var/scada/projects/;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }
}
```

---

## 附录

### A. 常见问题

**Q1: 如何修改项目根目录？**

修改 `appsettings.json` 中的 `ScadaSettings:ProjectPath` 配置。

**Q2: 如何实现多租户隔离？**

在数据库表中添加 `TenantId` 字段，在查询时增加租户ID过滤条件。

**Q3: 如何实现项目权限控制？**

创建 `ScadaProjectPermission` 关联表，关联项目ID、用户ID、权限类型（读/写/管理）。

**Q4: 文件存储能否改为云存储？**

可以，将文件系统操作抽象为 `IFileStorageService` 接口，实现阿里云OSS、AWS S3等云存储适配器。

---

### B. 性能优化建议

1. **数据库索引优化**: 已添加复合索引，可根据实际查询优化
2. **文件压缩**: 启用 `EnableCompression` 配置，压缩大型JSON文件
3. **CDN加速**: 资源文件通过CDN分发，减轻服务器压力
4. **缓存机制**: 使用Redis缓存项目元数据
5. **分页查询**: 限制单次查询最大条数，避免大量数据加载

---

### C. 扩展方向

1. **实时协作**: WebSocket实现多用户同时编辑
2. **版本控制**: Git集成，项目变更历史追踪
3. **模板市场**: 项目模板库，快速创建标准项目
4. **组件市场**: 自定义组件发布和下载
5. **数据分析**: 项目使用情况统计、热力图分析

---

**文档结束**

如有疑问，请联系技术支持。
