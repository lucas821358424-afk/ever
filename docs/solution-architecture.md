# 离线电子表单客户端技术方案（WPF + SQLite）

## 1. 总体架构

采用“本地优先（Offline-first）”架构：

1. **表示层（WPF）**：登录、模板列表、表单渲染、记录管理、日志查看。
2. **应用层（Application Service）**：权限判断、校验、提交流程、同步编排。
3. **领域层（Domain）**：模板、字段、记录、校验规则、报警规则、上传状态机。
4. **基础设施层（Infrastructure）**：SQLite、本地缓存、API客户端、加密、日志。

建议分层项目：

- `Client.App`（WPF）
- `Client.Application`
- `Client.Domain`
- `Client.Infrastructure`
- `Client.Contracts`（DTO / API模型）

---

## 2. 核心模块设计

### 2.1 用户登录模块

**能力**

- 账号密码登录（在线校验）
- 离线登录（本地缓存凭据 + 过期策略）
- 自动登录（可配置开关）
- 登录后加载角色权限与可见模板

**本地缓存建议字段**

- `user_id`、`username`、`role_id`
- `permission_json`
- `password_hash` + `salt`
- `last_login_time`
- `token`、`token_expire_time`（在线登录后）

### 2.2 模板管理模块

**模板内容**

- 背景图元数据（图片路径、尺寸、DPI）
- 字段定义（坐标、类型、是否必填、校验规则）
- 版本号（如 `major.minor.patch`）

**同步策略**

- 启动时检查模板版本
- 支持手动“同步模板”
- 模板同步优先于数据上传
- 版本不一致时提示用户更新

### 2.3 表单渲染模块（关键）

采用 `Canvas` 绝对定位 + 动态控件工厂：

- `TextBox`（Text/Number）
- `DatePicker`（Date）
- `ComboBox`（Select）
- `CheckBox`（Checkbox）
- `TextBlock`（Label）
- `InkCanvas`（Signature，可选）

**渲染流程**

1. 加载背景图片到 `Image`。
2. 按模板字段遍历创建控件。
3. 设置 `Canvas.Left/Top/Width/Height`。
4. 绑定字段值与校验状态。
5. 通过 `ScaleTransform` 实现缩放与触摸适配。

### 2.4 数据校验与报警模块

**校验规则**

- 必填、最小值、最大值、正则
- Number字段输入时实时校验

**报警动作**

- 输入框背景置红
- 弹窗提示（可防抖，避免连续弹窗）
- 根据模板配置决定是否阻止提交
- 写入报警日志

### 2.5 表单填写与记录模块

- 新建、暂存、继续编辑
- 未上传记录允许修改/删除
- 已上传记录只读
- 自动填充元数据：填写人、填写时间、设备号、模板版本

### 2.6 离线存储模块（SQLite）

建议最小表结构（可直接落地）：

```sql
CREATE TABLE form_record (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  template_id TEXT NOT NULL,
  version TEXT NOT NULL,
  user_id TEXT NOT NULL,
  fill_time TEXT NOT NULL,
  upload_status INTEGER NOT NULL DEFAULT 0,
  upload_time TEXT,
  device_id TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE field_data (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  record_id INTEGER NOT NULL,
  field_id TEXT NOT NULL,
  value TEXT,
  FOREIGN KEY(record_id) REFERENCES form_record(id)
);

CREATE TABLE form_template (
  template_id TEXT NOT NULL,
  version TEXT NOT NULL,
  name TEXT NOT NULL,
  background_path TEXT NOT NULL,
  schema_json TEXT NOT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY(template_id, version)
);

CREATE TABLE app_user (
  user_id TEXT PRIMARY KEY,
  username TEXT NOT NULL,
  role_id TEXT NOT NULL,
  permission_json TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  salt TEXT NOT NULL,
  last_login_time TEXT
);

CREATE TABLE app_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  log_type TEXT NOT NULL,
  level TEXT NOT NULL,
  message TEXT NOT NULL,
  detail_json TEXT,
  created_at TEXT NOT NULL
);
```

### 2.7 数据同步模块

- 网络检测（定时 + 手动触发）
- 批量上传 `upload_status=0` 的记录
- 成功后更新为 `1` 并写 `upload_time`
- 失败重试（指数退避）
- 幂等上传（客户端记录 `record_uuid`）

同步顺序：

1. 模板同步
2. 权限同步
3. 数据上传

### 2.8 权限控制模块（RBAC）

权限粒度建议：

- `template:view`
- `record:create`
- `record:edit`
- `record:delete`
- `record:upload`

客户端在UI层隐藏不可操作按钮，在应用层二次拦截。

### 2.9 日志与异常恢复

日志类型：登录、填写、报警、上传、错误。

可靠性建议：

- 本地事务写入（记录主表 + 明细表）
- 自动保存（如每30秒或字段变更触发）
- 崩溃恢复：启动时扫描未完成草稿
- 上传失败保留本地状态可继续重传

---

## 3. 安全设计

1. SQLite启用数据库加密（如 SQLCipher 或加密文件层）。
2. 密码使用 `PBKDF2`/`bcrypt` 哈希，不明文保存。
3. Token短期有效 + 刷新机制。
4. 关键操作（删除/上传）需权限 + 本地审计日志。
5. 对本地数据做完整性校验（防直接改库绕过权限）。

---

## 4. 性能与可维护性建议

- 模板与背景图缓存到本地，打开表单目标 < 2s
- 控件虚拟化/分段渲染（200+字段场景）
- 图片资源分辨率分级，避免超大图导致卡顿
- 使用后台队列处理同步，避免阻塞UI线程

---

## 5. API接口草案（最小集合）

- `POST /api/auth/login`
- `GET /api/templates?sinceVersion=...`
- `GET /api/permissions/me`
- `POST /api/records/batch-upload`
- `POST /api/client/heartbeat`

上传请求建议包含：

- `record_uuid`
- `template_id`
- `template_version`
- `user_id`
- `fill_time`
- `field_values[]`

---

## 6. 交付物映射

- 客户端安装包：MSIX / MSI
- 数据库脚本：`sql/init.sql`
- API文档：OpenAPI（Swagger）
- 使用手册：面向操作员和管理员
- 源代码：按分层结构交付

---

## 7. 开发里程碑（建议）

1. **M1（2周）**：登录、模板加载、基础渲染、SQLite落库。
2. **M2（2周）**：校验报警、记录管理、日志、离线稳定性。
3. **M3（2周）**：模板/数据同步、权限控制、安全加固。
4. **M4（1周）**：性能压测、打包部署、文档交付。

> 结论：该方案满足“可配置、可离线运行、可权限控制”的工业电子点检表单客户端目标。
