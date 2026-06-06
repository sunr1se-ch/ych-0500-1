# 烫金版材压印衰减与套准偏差追溯工作台

工厂单机场景的版材压印追溯系统，基于 Docker Compose 部署。

## 功能特性

- **版材管理**: 登记版材（钢版编号、设计凹深、寿命极限）
- **压印记录**: 记录每次压印的套准偏差（X/Y轴）和实压温度
- **寿命预警**: 累计压印次数达到 80% 预警，100% 自动锁定版材
- **套准异常检测**: 连续 3 次偏移 > 0.08μm 自动生成异常单
- **PDF 报告导出**: QuestPDF 生成完整追溯报告
- **数据可视化**: 偏移热力散点图展示套准偏差趋势

## 技术栈

- **后端**: C# 12 + ASP.NET Core Minimal API + EF Core + SQLite
- **前端**: Angular 17 + Standalone Components
- **报告**: QuestPDF
- **部署**: Docker Compose + Nginx

## 快速开始

### Docker Compose 部署

```bash
docker-compose up -d --build
```

访问地址:
- 前端 UI: http://localhost:8080
- 后端 API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### 手动运行（开发环境）

#### 后端
```bash
cd backend
dotnet run
```

#### 前端
```bash
cd frontend
npm install
npm start
```

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/plates` | 获取版材列表 |
| GET | `/api/plates/{id}` | 获取版材详情 |
| GET | `/api/plates/{id}/report` | 下载版材 PDF 报告 |
| POST | `/api/plates` | 登记新版材 |
| POST | `/api/impressions` | 记录压印 |
| GET | `/api/incidents` | 获取异常单列表 |
| PUT | `/api/incidents/{id}/resolve` | 标记异常已解决 |
| GET | `/api/warnings` | 获取警告列表 |
| PUT | `/api/warnings/{id}/acknowledge` | 确认警告 |

## 数据模型

### plates 表
- `Id` (PK)
- `SteelPlateNumber` 钢版编号
- `DesignDepth` 设计凹深 (μm)
- `LifeLimit` 寿命极限 (次)
- `ImpressionCount` 当前压印次数
- `IsLocked` 是否已锁定
- `CreatedAt` 登记时间

### impressions 表
- `Id` (PK)
- `PlateId` (FK)
- `OffsetX` X轴偏移 (μm)
- `OffsetY` Y轴偏移 (μm)
- `ActualTemperature` 实压温度 (°C)
- `CreatedAt` 压印时间

### alignment_incidents 表
- `Id` (PK)
- `PlateId` (FK)
- `StartImpressionId` 起始压印ID
- `EndImpressionId` 结束压印ID
- `Axis` 轴 (X/Y)
- `Notes` 备注
- `IsResolved` 是否已解决
- `CreatedAt` 创建时间

### warnings 表
- `Id` (PK)
- `PlateId` (FK)
- `WarningType` 警告类型
- `Message` 警告消息
- `IsAcknowledged` 是否已确认
- `CreatedAt` 创建时间

## 业务规则

1. **寿命预警**: 压印次数 ≥ 寿命极限 × 80% 时生成警告
2. **自动锁定**: 压印次数 ≥ 寿命极限 × 100% 时自动锁定版材，禁止继续压印
3. **套准异常**: 连续 3 次 `|OffsetX| > 0.08` 或 `|OffsetY| > 0.08` 时生成异常单

## 目录结构

```
├── backend/                 # ASP.NET Core 后端
│   ├── Models/              # 数据模型
│   ├── Data/                # EF Core DbContext
│   ├── Dtos/                # 数据传输对象
│   ├── Services/            # 业务逻辑服务
│   ├── Program.cs           # 程序入口
│   ├── appsettings.json     # 配置文件
│   └── Dockerfile
├── frontend/                # Angular 17 前端
│   ├── src/
│   │   ├── app/
│   │   │   ├── models/      # TypeScript 接口
│   │   │   ├── services/    # API 服务
│   │   │   └── pages/       # 页面组件
│   │   ├── main.ts          # 入口文件
│   │   └── styles.css       # 全局样式
│   ├── nginx.conf           # Nginx 配置
│   └── Dockerfile
├── docker-compose.yml       # Docker Compose 配置
└── README.md
```

## 数据持久化

SQLite 数据库存储在 Docker volume `plate-data` 中，路径为 `/data/plate_tracking.db`。

## 许可证

MIT
