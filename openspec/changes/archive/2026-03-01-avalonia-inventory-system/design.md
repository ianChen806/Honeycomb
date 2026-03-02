## Context

全新桌面應用程式，目標是提供簡易的庫存管理功能，特別著重多幣別與匯率的成本追蹤。使用者為個人或小型團隊，資料量預期不大（數百至數千筆商品）。

技術約束：
- .NET 10 + Avalonia 11.x（跨平台桌面 UI）
- 本地部署，無需伺服器
- 單人使用，無需多使用者同步

## Goals / Non-Goals

**Goals:**
- 提供直覺的商品新增、瀏覽、刪除介面
- 支援多幣別設定與匯率記錄
- 自動計算成本價與總價
- 匯出商品清單為 Excel 檔案
- 本地 SQLite 儲存，零配置啟動

**Non-Goals:**
- 不做多使用者或網路同步
- 不做商品圖片管理
- 不做進銷存流程（僅庫存記錄）
- 不做即時匯率查詢 API 串接
- 不做報表分析或圖表

## Decisions

### 1. UI 框架：Avalonia 11.x + MVVM

**選擇**: Avalonia 11 搭配 CommunityToolkit.Mvvm

**理由**: Avalonia 提供跨平台桌面 UI，CommunityToolkit.Mvvm 透過 source generator 減少樣板程式碼。相較 ReactiveUI 更輕量，學習曲線較低。

### 2. 資料存取：EF Core + SQLite

**選擇**: Microsoft.EntityFrameworkCore.Sqlite

**理由**: SQLite 零配置、嵌入式，適合桌面應用。EF Core 提供型別安全的查詢與 migration 支援。資料量小，不需要更重量級的資料庫。

**替代方案**: LiteDB（NoSQL，不需定義 schema）——但 EF Core 生態系更成熟，未來擴充性更好。

### 3. Excel 匯出：ClosedXML

**選擇**: ClosedXML

**理由**: MIT 授權、免費、API 直覺。相較 EPPlus（需商業授權）和 NPOI（API 較複雜），ClosedXML 是最佳平衡點。

### 4. 專案結構：單一專案 + 功能分資料夾

**選擇**: 單一 Avalonia 專案，以 Models / Data / ViewModels / Views / Services 資料夾組織。

**理由**: 專案規模小，拆分多個專案反而增加複雜度。功能分資料夾足以維持程式碼組織。

### 5. 導覽方式：TabControl 分頁

**選擇**: MainWindow 使用 TabControl，分為「商品列表」和「幣別設定」兩個分頁。

**理由**: 功能僅兩個主要區塊，TabControl 簡單直覺，不需要複雜的導覽框架。

## Risks / Trade-offs

- **[.NET 10 相容性]** → Avalonia 11.x 可能尚未完全支援 .NET 10。若遇問題，回退至 .NET 9。
- **[SQLite 並發]** → 單人桌面應用，不會有並發問題。若未來需多人使用，需遷移至 PostgreSQL/SQL Server。
- **[無即時匯率]** → 匯率為手動輸入。使用者需自行查詢當前匯率。這是刻意簡化，未來可加入 API 串接。
