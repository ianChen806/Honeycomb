## Context

Honeycomb 是 .NET 10 + Avalonia 11 MVVM 架構的桌面庫存管理系統。目前有固定兩個 Tab（商品列表、幣別設定），Product model 含 `Quantity` 欄位但對定價無實際用途。使用者需要：(1) 以「額外成本」取代「數量」，(2) 分類 Tab 系統管理商品，(3) 改善表格可讀性。

現有架構：MainWindowViewModel 組合 ProductListViewModel + CurrencySettingsViewModel，TabControl 硬編碼在 MainWindow.axaml。Product 透過 EF Core tracking 支援 DataGrid inline editing。

## Goals / Non-Goals

**Goals:**
- 將 Quantity 替換為 ExtraCost（NT 計價，不受匯率影響）
- 更新成本公式並確保所有計算正確
- 引入 Category model 與動態 Tab 系統
- 改善 DataGrid 視覺可讀性（交替列色、毛利率 `%`）

**Non-Goals:**
- 不做子分類（僅一層大分類）
- 不做分類之間的拖拉移動商品
- 不做分類排序功能
- 不改變幣別設定的邏輯（僅維持獨立 Tab）

## Decisions

### D1: Category 與 Product 的關係
**決定**: Product 新增 nullable `CategoryId` FK 指向 Category。無分類的商品顯示在預設 Tab 或不顯示（依需求）。

**替代方案**: 用 Tag 系統（多對多）——過於複雜，不符合「大分類」的簡單需求。

### D2: 動態 Tab 實現方式
**決定**: MainWindowViewModel 維護 `ObservableCollection<CategoryTabViewModel>`，每個 CategoryTabViewModel 包含自己的 ProductListViewModel 和篩選邏輯。TabControl 綁定此 collection，加上固定的幣別設定 Tab。

**替代方案**: 單一 ProductListView 內部用 filter 切換——無法滿足「分開 Tab 跟儲存內容」的需求。

### D3: ExtraCost 欄位設計
**決定**: `ExtraCost` 為 decimal，預設值 0，單位固定 NT。加入 CostPrice 公式時直接加總，不乘匯率。

### D4: DataGrid 交替列色
**決定**: 使用 Avalonia DataGrid 的 `AlternatingRowBackground` 屬性，設定為系統配色的灰色變體（`SystemControlBackgroundListLowBrush` 或類似）。

### D5: Migration 策略
**決定**: 單一 migration 處理：(1) Product 表 rename Quantity→ExtraCost 並轉型為 decimal，(2) 新增 Category 表，(3) Product 新增 nullable CategoryId FK。既有資料的 ExtraCost 設為 0。

## Risks / Trade-offs

- **[資料遷移]** Quantity (int) → ExtraCost (decimal) 型別不同 → 使用 SQL 直接轉換，既有值設為 0（原 Quantity 值無意義於新欄位）
- **[Tab 效能]** 大量分類可能影響記憶體 → 每個 Tab 延遲載入（LazyLoading），僅活動 Tab 載入商品資料
- **[刪除分類]** 分類下有商品時強制刪除會失去分類關聯 → 將商品的 CategoryId 設為 null（回到未分類狀態）
