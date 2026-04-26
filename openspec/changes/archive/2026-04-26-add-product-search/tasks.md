## 1. 搜尋狀態與匹配邏輯

- [x] 1.1 建立 `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs` 並寫前 4 個 failing test（`EmptyQuery_ClearsMatchCount`、`NoMatch_SetsCountToZero`、`SingleMatch_SetsCountToOneOfOne`、`MultipleMatches_OrderedByProductsCollection`）
- [x] 1.2 跑 `dotnet test --filter "FullyQualifiedName~ProductListSearchTests"` 確認編譯失敗
- [x] 1.3 在 `ProductListViewModel` 加入 `SearchQuery`、`IsSearchVisible`、`MatchCountText` 屬性、`_matches`、`_currentMatchIndex` 欄位，以及 `OnSearchQueryChanged`、`RecomputeMatches`、`UpdateMatchCountText` 方法
- [x] 1.4 跑測試確認 4 個全部通過
- [x] 1.5 Commit：`feat: 商品搜尋狀態與匹配邏輯`

## 2. 導航（Next / Previous 含 wrap-around）

- [x] 2.1 在 `ProductListSearchTests` 追加 4 個 failing test（`NextMatch_AdvancesIndex`、`NextMatch_WrapsAroundAtEnd`、`PreviousMatch_WrapsAroundAtStart`、`NextMatch_RaisesMatchScrollRequested`）
- [x] 2.2 跑測試確認編譯失敗
- [x] 2.3 在 `ProductListViewModel` 加入 `MatchScrollRequested` 事件、`NextMatch()` / `PreviousMatch()` 方法，並修改 `RecomputeMatches` 使有匹配時觸發 scroll
- [x] 2.4 跑測試確認 8 個全部通過
- [x] 2.5 Commit：`feat: 商品搜尋導航與匹配捲動事件`

## 3. 大小寫不敏感、LoadData 重置搜尋

- [x] 3.1 在 `ProductListSearchTests` 追加 2 個測試（`CaseInsensitive_MatchesRegardlessOfCase`、`LoadData_ResetsSearchState`）
- [x] 3.2 跑測試確認 `CaseInsensitive_MatchesRegardlessOfCase` 通過、`LoadData_ResetsSearchState` 失敗
- [x] 3.3 在 `ProductListViewModel.LoadData()` 開頭加入 `SearchQuery = string.Empty;`
- [x] 3.4 跑測試確認 10 個全部通過
- [x] 3.5 跑 `dotnet test` 確認沒打壞舊功能
- [x] 3.6 Commit：`feat: LoadData 重置商品搜尋狀態`

## 4. 開關搜尋層（OpenSearch / CloseSearch）

- [x] 4.1 在 `ProductListSearchTests` 追加 2 個測試（`OpenSearchCommand_SetsIsSearchVisibleTrue`、`CloseSearch_SetsIsSearchVisibleFalse`）
- [x] 4.2 跑測試確認編譯失敗
- [x] 4.3 在 `ProductListViewModel` 加入 `[RelayCommand] OpenSearch` 與 `CloseSearch()` 方法
- [x] 4.4 跑測試確認 12 個全部通過
- [x] 4.5 Commit：`feat: OpenSearch / CloseSearch 控制搜尋層顯示`

## 5. 加入浮動搜尋層 UI

- [x] 5.1 在 `ProductListView.axaml` 加入 `<UserControl.KeyBindings>` 綁定 `Ctrl+F` 到 `OpenSearchCommand`
- [x] 5.2 在 `ProductListView.axaml` 的 Grid Row 1 加入浮動搜尋 Border（含 SearchBox、計數 TextBlock、✕ 按鈕，定位於右上角）
- [x] 5.3 在 `ProductListView.axaml.cs` 加入占位 handler `OnSearchKeyDown` 與 `OnCloseSearchClicked` 讓 build 通過
- [x] 5.4 跑 `dotnet build Honeycomb.slnx` 驗證 build 成功
- [x] 5.5 Commit：`feat: 商品搜尋浮動 UI 與 Ctrl+F 綁定`

## 6. 鍵盤事件與 ScrollIntoView 串接

- [x] 6.1 在 `ProductListView.axaml.cs` 覆寫 `OnAttachedToVisualTree` / `OnDetachedFromVisualTree` 訂閱與解訂 `MatchScrollRequested` 與 `PropertyChanged`
- [x] 6.2 加入 `OnMatchScrollRequested` handler 設定 `ProductGrid.SelectedItem` 並呼叫 `ScrollIntoView`
- [x] 6.3 加入 `OnVmPropertyChanged` 在 `IsSearchVisible` 變 true 時透過 `Dispatcher.UIThread.Post` 將焦點移至 `SearchBox` 並 `SelectAll`
- [x] 6.4 將占位 `OnSearchKeyDown` 改為實作（`Enter` → `NextMatch`、`Shift+Enter` → `PreviousMatch`、`Esc` → `CloseSearch` + `ProductGrid.Focus()`）
- [x] 6.5 將占位 `OnCloseSearchClicked` 改為實作（呼叫 `vm.CloseSearch()` + `ProductGrid.Focus()`）
- [x] 6.6 跑 `dotnet build Honeycomb.slnx` 驗證 build 成功
- [x] 6.7 跑 `dotnet test` 確認所有測試通過
- [x] 6.8 跑 `dotnet run --project src/Honeycomb` 進行手動煙霧測試（12 項，詳見 `design.md` Task 6 Step 4）
- [x] 6.9 Commit：`feat: 商品搜尋鍵盤導航與捲動串接`
