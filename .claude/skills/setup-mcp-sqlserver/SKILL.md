# Skill: Setup MCP SQL Server Integration

## Purpose

Tích hợp MCP SQL Server để AI có thể khám phá database schema và sinh code tự động theo CQRS convention của dự án.
Khi MCP SQL Server được cấu hình, AI sẽ dùng các MCP tools để:
- Khám phá tables, columns, data types, constraints
- Xem foreign key relationships
- Phân tích stored procedures
- Sinh Domain Entities, Commands, Queries phù hợp với schema thực tế

## Prerequisites

### 1. MCP SQL Server Connection

Đảm bảo `C:\Users\remoteuser\.cursor\mcp.json` đã được cấu hình:

```json
{
  "mcpServers": {
    "mssql": {
      "command": "npx",
      "args": ["-y", "mssql-mcp-server"],
      "env": {
        "MSSQL_HOST": "YOUR_SERVER_IP",
        "MSSQL_PORT": "1433",
        "MSSQL_DATABASE": "YOUR_DATABASE",
        "MSSQL_USER": "YOUR_USERNAME",
        "MSSQL_PASSWORD": "YOUR_PASSWORD",
        "MSSQL_ENCRYPT": "false",
        "MSSQL_TRUST_SERVER_CERTIFICATE": "true"
      }
    }
  }
}
```

### 2. Restart Cursor

Sau khi cấu hình `mcp.json`, **restart Cursor** để MCP server được load.

### 3. Xác nhận MCP Tools đã sẵn sàng

Kiểm tra Output Panel (`Ctrl + Shift + U`) > MCP Logs để xác nhận kết nối thành công.

## MCP SQL Server Tools

### Available Tools

| Tool | Mô tả | Usage |
|------|--------|-------|
| `test_connection` | Kiểm tra kết nối | Xác nhận MCP hoạt động |
| `list_databases` | Liệt kê databases | Chọn database làm việc |
| `list_tables` | Liệt kê tables | Xem entities trong schema |
| `describe_table` | Chi tiết schema table | Map columns sang Domain Entity |
| `execute_query` | Thực thi SELECT | Kiểm tra sample data |
| `get_relationships` | Xem foreign keys | Map relationships |
| `list_stored_procedures` | Liệt kê stored procs | Generate procedure wrappers |
| `get_stored_procedure_definition` | Code stored proc | Reverse engineer logic |
| `list_indexes` | Xem indexes | Performance analysis |
| `find_missing_indexes` | Indexes có thể thiếu | Optimization |

## Workflow: Generate Code from Database Schema

### Phase 1: Discover

Khi user yêu cầu sinh code cho một table, thực hiện:

**Step 1:** Test connection
```
Gọi tool: test_connection
Xác nhận: Kết nối thành công, server info hiển thị
```

**Step 2:** List tables
```
Gọi tool: list_tables với schema = "dbo"
Output: Danh sách table names
```

**Step 3:** Describe table
```
Gọi tool: describe_table với tableName = "{UserTableName}", schema = "dbo"
Output:
  - Column names
  - Data types
  - Nullable
  - IsPrimaryKey
  - DefaultValue
```

**Step 4:** Get relationships (nếu cần)
```
Gọi tool: get_relationships với tableName = "{UserTableName}"
Output: Foreign key constraints, referenced tables
```

**Step 5:** Sample data (tùy chọn)
```
Gọi tool: sample_data với tableName = "{UserTableName}", limit = 5
Output: 5 rows sample để hiểu data pattern
```

### Phase 2: Generate

Sau khi có schema, sinh code theo project conventions:

#### 2.1 Domain Entity

Dùng `generate-domain-entity` skill để tạo:

- `{EntityName}Id.cs` — Strongly-typed ID
- `{EntityName}.cs` — Aggregate Root
- `{EntityName}Errors.cs` — Domain errors
- `{EntityName}Configuration.cs` — EF Core config

**Mapping rules từ SQL types:**

| SQL Type | C# Type |
|----------|---------|
| `int` | `int` |
| `bigint` | `long` |
| `nvarchar(n)` | `string` |
| `varchar(n)` | `string` |
| `datetime`, `datetime2` | `DateTime` |
| `date` | `DateOnly` |
| `time` | `TimeOnly` |
| `bit` | `bool` |
| `decimal(p,s)` | `decimal` |
| `uniqueidentifier` | `Guid` |
| `float` | `double` |
| `real` | `float` |
| `tinyint` | `byte` |
| `smallint` | `short` |
| `binary`, `varbinary` | `byte[]` |

**EF Core column mapping:**
- Dùng `HasColumnName("snake_case_column_name")`
- Primary key: `HasColumnName("id")`
- MaxLength từ varchar/nvarchar
- Required từ nullable

#### 2.2 CQRS Command

Dùng `generate-command` skill để tạo:

- `{UseCase}Command.cs` — Input record
- `{UseCase}CommandValidator.cs` — FluentValidation
- `{UseCase}CommandHandler.cs` — Business logic

**Validation mapping:**
- `NOT NULL` + no default → Required field
- `nvarchar(max)` → `[MaxLength]` attribute
- `decimal(p,s)` → `[Precision(p,s)]` attribute
- Primary key in create → Exclude (auto-generated)

#### 2.3 CQRS Query

Dùng `generate-query` skill để tạo:

- `{QueryName}Query.cs` — Query record
- `{QueryName}QueryHandler.cs` — Read logic
- `{QueryName}Response.cs` — DTO

**Query decision:**
- Simple single-table query → EF Core với `AsNoTracking()`
- Complex JOIN → Dapper

## Integration với Existing Skills

### Với `generate-domain-entity`

Khi user gọi "generate domain entity cho table X":
1. Gọi `describe_table` để lấy schema
2. Gọi `get_relationships` để lấy FK
3. Áp dụng mapping rules tạo Entity files

### Với `generate-command`

Khi user gọi "generate command cho table X":
1. Gọi `describe_table` để lấy columns
2. Sinh Command record với properties từ schema
3. Sinh Validator với rules từ constraints

### Với `generate-query`

Khi user gọi "generate query cho table X":
1. Gọi `describe_table` và `sample_data`
2. Chọn EF Core hoặc Dapper dựa trên độ phức tạp
3. Sinh Query files

## Usage Examples

### Example 1: Khám phá và sinh Entity

**User:** "Generate domain entity cho table `Customers`"

**AI Actions:**
1. `list_tables` → Xác nhận tồn tại
2. `describe_table` với tableName="Customers" → Lấy schema
3. `get_relationships` với tableName="Customers" → Lấy FK
4. Sinh `CustomerId.cs`, `Customer.cs`, `CustomerErrors.cs`, `CustomerConfiguration.cs`

### Example 2: Sinh full CRUD

**User:** "Generate CRUD operations cho table `Products`"

**AI Actions:**
1. `describe_table` với tableName="Products"
2. `get_relationships` với tableName="Products"
3. Sinh Domain Entity
4. Sinh Create/Update/Delete Commands + Handlers
5. Sinh GetById/List Queries + Handlers
6. Sinh Controller endpoints

### Example 3: Reverse engineer stored procedure

**User:** "Wrap stored procedure `sp_GetMonthlyReport` thành CQRS query"

**AI Actions:**
1. `get_stored_procedure_definition` với procedureName="sp_GetMonthlyReport"
2. Phân tích parameters và output
3. Sinh Query record + Handler gọi procedure
4. Sinh Response DTO

## Edge Cases

- **Table không tồn tại:** Thông báo user, gợi ý `list_tables` để xem danh sách
- **Kết nối thất bại:** Nhắc kiểm tra `mcp.json` credentials và restart Cursor
- **Windows Authentication:** Dùng `Integrated Security=SSPI` trong connection string
- **Multiple schemas:** Luôn hỏi user schema name (mặc định: `dbo`)
- **Table không có PK:** Cảnh báo user — không thể tạo Entity đúng convention
- **Large table (>100 columns):** Gợi ý tách thành multiple aggregates

## Security Notes

- **KHÔNG BAO GIỜ** gọi `execute_query` với INSERT/UPDATE/DELETE — chỉ dùng SELECT
- Không hardcode credentials trong mcp.json — dùng environment variables
- Nếu database có PII columns: nhắc user cân nhắc masking trong DTO responses

## Checklist

- [ ] MCP SQL Server connection tested (`test_connection` thành công)
- [ ] Table schema discovered (`describe_table` có output)
- [ ] Domain Entity generated theo convention
- [ ] EF Core configuration dùng snake_case column names
- [ ] C# types mapped đúng từ SQL types
- [ ] FK relationships represented trong Domain layer

## References

- MCP SQL Server Tools: xem danh sách đầy đủ ở trên
- `generate-domain-entity/SKILL.md` — Domain Entity conventions
- `generate-command/SKILL.md` — CQRS Command conventions
- `generate-query/SKILL.md` — CQRS Query conventions
- `setup-configuration/SKILL.md` — Options pattern
- Database schema convention: snake_case column names
