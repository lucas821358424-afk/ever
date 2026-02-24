# Excel模板两步法（用户只提供Excel）

当用户只提供 Excel 时，客户端按两步法处理：

1. **步骤1：Excel -> 模板元数据**
   - 客户端读取 Excel 中 `TemplateMeta` 工作表
   - 生成 `FormTemplate`（字段坐标、类型、校验规则）
2. **步骤2：模板元数据 -> 动态填写界面**
   - 使用现有 Canvas 动态控件渲染器生成可填写界面

## TemplateMeta 规范

- `B1`：TemplateId（可空，空则使用文件名）
- `B2`：TemplateName（可空）
- `B3`：Version（可空，默认 `1.0.0`）
- `B4`：BackgroundImage（可空）

第6行为字段表头，第7行开始字段数据：

| 列 | 含义 |
|---|---|
| A | FieldId |
| B | Name |
| C | Type（Text/Number/Date/Select/Checkbox/Label/Signature） |
| D | X |
| E | Y |
| F | Width |
| G | Height |
| H | Placeholder |
| I | Options（下拉用 `|` 分隔） |
| J | Required（1/true/yes/是） |
| K | Min |
| L | Max |
| M | Regex |
| N | BlockSubmitOnError（1/true/yes/是） |

> 注意：该方案避免把 Excel 当作填写载体，Excel 仅作为模板输入源。
