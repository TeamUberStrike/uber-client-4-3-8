using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Core.ViewModel;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Helper;

public class TransactionHistory : Singleton<TransactionHistory>
{
    #region Fields

    private static string DATE_FORMAT = "yyyy/MM/dd";

    private enum TransactionType
    {
        Item = 0,
        Point = 1,
        Credit = 2
    }

    private enum AccountArea
    {
        Items,
        Points,
        Credits
    }

    private const float RowHeight = 23;
    private const float ButtonWidth = 100;
    private const float ButtonHeight = 32;
    private const int ElementsPerPage = 15;

    private int _selectedTab = 0;

    private GUIContent[] _tabs;

    private Vector2 _scrollControls;

    private string[] _itemsTableColumnHeadingArray;
    private string[] _pointsTableColumnHeadingArray;
    private string[] _creditsTableColumnHeadingArray;

    private string _prevPageButtonLabel;
    private string _nextPageButtonLabel;

    private TransactionCache<ItemTransactionsViewModel> _itemTransactions;
    private TransactionCache<PointDepositsViewModel> _pointTransactions;
    private TransactionCache<CurrencyDepositsViewModel> _creditTransactions;

    #endregion

    private class TransactionCache<T>
    {
        public SortedList<int, T> PageCache { get; private set; }
        public int CurrentPageIndex { get; set; }
        public T CurrentPage
        {
            get
            {
                if (PageCache.ContainsKey(CurrentPageIndex))
                {
                    return PageCache[CurrentPageIndex];
                }
                else
                {
                    return default(T);
                }
            }
        }
        public bool CurrentPageNeedsRefresh
        {
            get
            {
                return CurrentPage == null || (CurrentPageIndex > 0 && CurrentPageIndex == PageCount - 1 && _refreshLastPage < Time.time);
            }
        }
        public int ElementCount { get; set; }
        public int PageCount { get { return Mathf.CeilToInt(ElementCount / ElementsPerPage) + 1; } }
        private float _refreshLastPage;

        public void SetPage(int index, T page)
        {
            PageCache[index] = page;
            if (index + 1 == PageCount)
                _refreshLastPage = Time.time + 30;
        }

        public TransactionCache()
        {
            PageCache = new SortedList<int, T>();
        }
    }

    private TransactionHistory()
    {
        _itemTransactions = new TransactionCache<ItemTransactionsViewModel>();
        _pointTransactions = new TransactionCache<PointDepositsViewModel>();
        _creditTransactions = new TransactionCache<CurrencyDepositsViewModel>();

        // TODO: retrieve string from localization repo
        _tabs = new GUIContent[]
        {
			new GUIContent("Items"),
			new GUIContent("Points"),
			new GUIContent("Credits")
        };

        // TODO: retrieve string from localization repo
        _itemsTableColumnHeadingArray = new string[]{
            "Date",
            "Item Name",
            "Points",
            "Credits",
            "Duration"
        };

        // TODO: retrieve string from localization repo
        _pointsTableColumnHeadingArray = new string[]{
            "Date", 
            "Points", 
            "Type"
        };

        // TODO: retrieve string from localization repo
        _creditsTableColumnHeadingArray = new string[]{
            "Transaction Key",
            "Date",
            "Cost",
            "Credits",
            "Points",
            "Bundle Name"
        };

        // TODO: retrieve string from localization repo
        _prevPageButtonLabel = "Prev Page";
        _nextPageButtonLabel = "Next Page";
    }

    public void DrawPanel(Rect panelRect)
    {
        GUI.BeginGroup(panelRect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            DrawTabs(new Rect(2, 5, panelRect.width - 4, 30));
            DrawTable(new Rect(2, 35, panelRect.width - 4, panelRect.height - 35));
        }
        GUI.EndGroup();
    }

    private void DrawTable(Rect panelRect)
    {
        Rect tableHeadingRect = new Rect(panelRect.x + 5, panelRect.y, panelRect.width - 10, 25);
        Rect tableContentRect = new Rect(panelRect.x + 5, panelRect.y + 30, panelRect.width - 10, panelRect.height - 35 - ButtonHeight - 5);

        Rect buttonsAreaCenter = new Rect(0, tableContentRect.y + tableContentRect.height, panelRect.width, panelRect.height - tableContentRect.height);

        TransactionType type = (TransactionType)_selectedTab;
        if (type == TransactionType.Item)
        {
            DrawItemsTableHeadingBar(tableHeadingRect);
            DrawItemsTableContent(tableContentRect);
            DrawItemsButtons(buttonsAreaCenter);
        }
        else if (type == TransactionType.Point)
        {
            DrawPointsTableHeadingBar(tableHeadingRect);
            DrawPointsTableContent(tableContentRect);
            DrawPointsButtons(buttonsAreaCenter);
        }
        else if (type == TransactionType.Credit)
        {
            DrawCreditsTableHeadingBar(tableHeadingRect);
            DrawCreditsTableContent(tableContentRect);
            DrawCreditsButtons(buttonsAreaCenter);
        }
    }

    private void DrawTabs(Rect tabRect)
    {
        int curTab = GUI.SelectionGrid(tabRect, _selectedTab, _tabs, _tabs.Length, BlueStonez.tab_medium);
        if (curTab != _selectedTab)
        {
            _selectedTab = curTab;
            GetCurrentTransactions();
        }
    }

    private float GetColumnOffset(AccountArea area, int index, float totalWidth)
    {
        if (area == AccountArea.Items)
        {
            switch (index)
            {
                case 0: return 0;
                case 1: return 100;
                case 2: return 100 + Mathf.Max(totalWidth, 400) - 300;
                case 3: return 100 + Mathf.Max(totalWidth, 400) - 300 + 50;
                case 4: return 100 + Mathf.Max(totalWidth, 400) - 300 + 50 + 50;
                default: return 0;
            }
        }
        else if (area == AccountArea.Points)
        {
            return index * Mathf.RoundToInt(totalWidth / 3);
        }
        else if (area == AccountArea.Credits)
        {
            switch (index)
            {
                case 0: return 0;
                case 1: return 150;
                case 2: return 220;
                case 3: return 220 + 50;
                case 4: return 220 + 50 + 50;
                case 5: return 220 + 50 + 50 + 50;
                default: return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    private float GetColumnWidth(AccountArea area, int index, float totalWidth)
    {
        if (area == AccountArea.Items)
        {
            switch (index)
            {
                case 0: return 150 + 1;
                case 1: return Mathf.Max(totalWidth, 400) - 300 + 1; //min 100
                case 2: return 50 + 1;
                case 3: return 50 + 1;
                case 4: return 100;
                default: return 0;
            }
        }
        else if (area == AccountArea.Points)
        {
            switch (index)
            {
                case 0: return Mathf.RoundToInt(totalWidth / 3) + 1;
                case 1: return Mathf.RoundToInt(totalWidth / 3) + 1;
                case 2: return Mathf.RoundToInt(totalWidth / 3);
                default: return 0;
            }
        }
        else if (area == AccountArea.Credits)
        {
            switch (index)
            {
                case 0: return 150 + 1;
                case 1: return 70 + 1;
                case 2: return 50 + 1;
                case 3: return 50 + 1;
                case 4: return 50 + 1;
                case 5: return Mathf.Max(totalWidth, 450) - 370; //min 100
                default: return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    #region Items

    private void DrawItemsTableHeadingBar(Rect headingRect)
    {
        GUI.BeginGroup(headingRect);
        {
            for (int i = 0; i < _itemsTableColumnHeadingArray.Length; i++)
            {
                Rect buttonRect = new Rect(GetColumnOffset(AccountArea.Items, i, headingRect.width), 0, GetColumnWidth(AccountArea.Items, i, headingRect.width), headingRect.height);
                GUI.Button(buttonRect, string.Empty, BlueStonez.box_grey50);
                GUI.Label(buttonRect, new GUIContent(_itemsTableColumnHeadingArray[i]), BlueStonez.label_interparkmed_11pt);
            }
        }
        GUI.EndGroup();
    }

    private void DrawItemsTableContent(Rect scrollViewRect)
    {
        GUI.Box(scrollViewRect, GUIContent.none, BlueStonez.window_standard_grey38);

        if (_itemTransactions.CurrentPage != null)
        {
            _scrollControls = GUI.BeginScrollView(scrollViewRect.ExpandBy(0, -1), _scrollControls, new Rect(0, 0, scrollViewRect.width - 17, _itemTransactions.CurrentPage.ItemTransactions.Count * RowHeight));
            {
                //Rect entryRect = new Rect(0, 0, scrollViewRect.width, _rowHeight);
                float yOffset = 0;
                foreach (var e in _itemTransactions.CurrentPage.ItemTransactions)
                {
                    IUnityItem item = ItemManager.Instance.GetItemInShop(e.ItemId);
                    string itemName = item != null ? TextUtility.ShortenText(item.Name, 20, true) : string.Format("item[{0}]", e.ItemId);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Items, 0, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Items, 0, scrollViewRect.width), RowHeight), e.WithdrawalDate.ToString(DATE_FORMAT), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Items, 1, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Items, 1, scrollViewRect.width), RowHeight), itemName, BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Items, 2, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Items, 2, scrollViewRect.width), RowHeight), e.Points.ToString(), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Items, 3, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Items, 3, scrollViewRect.width), RowHeight), e.Credits.ToString(), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Items, 4, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Items, 4, scrollViewRect.width), RowHeight), ShopUtils.PrintDuration(e.Duration), BlueStonez.label_interparkmed_11pt);
                    yOffset += RowHeight;
                }
            }
            GUI.EndScrollView();
        }
    }

    private void DrawItemsButtons(Rect rect)
    {
        GUIStyle buttonStyle = BlueStonez.button;
        GUI.enabled = _itemTransactions.CurrentPageIndex != 0;
        if (GUITools.Button(new Rect(rect.x + 6, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_prevPageButtonLabel), buttonStyle))
        {
            _itemTransactions.CurrentPageIndex--;
            AsyncGetItemTransactions();
        }
        GUI.enabled = true;

        if (_itemTransactions.ElementCount > 0)
        {
            GUI.Label(new Rect((rect.x + rect.width) / 2 - 100, rect.y + 5, 200, ButtonHeight), string.Format("Page {0} of {1}", _itemTransactions.CurrentPageIndex + 1, _itemTransactions.PageCount), BlueStonez.label_interparkbold_11pt);

            GUI.enabled = _itemTransactions.CurrentPageIndex + 1 < _itemTransactions.PageCount;
            if (GUITools.Button(new Rect(rect.x + rect.width - ButtonWidth - 2, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_nextPageButtonLabel), buttonStyle))
            {
                _itemTransactions.CurrentPageIndex++;
                AsyncGetItemTransactions();
            }
            GUI.enabled = true;
        }
    }

    #endregion

    #region Points

    private void DrawPointsTableHeadingBar(Rect headingRect)
    {
        GUI.BeginGroup(headingRect);
        {
            for (int i = 0; i < _pointsTableColumnHeadingArray.Length; i++)
            {
                Rect buttonRect = new Rect(GetColumnOffset(AccountArea.Points, i, headingRect.width), 0, GetColumnWidth(AccountArea.Points, i, headingRect.width), headingRect.height);
                GUI.Button(buttonRect, string.Empty, BlueStonez.box_grey50);
                GUI.Label(buttonRect, new GUIContent(_pointsTableColumnHeadingArray[i]), BlueStonez.label_interparkmed_11pt);
            }
        }
        GUI.EndGroup();
    }

    private void DrawPointsTableContent(Rect scrollViewRect)
    {
        GUI.Box(scrollViewRect, GUIContent.none, BlueStonez.window_standard_grey38);

        if (_pointTransactions.CurrentPage != null)
        {
            _scrollControls = GUI.BeginScrollView(scrollViewRect.ExpandBy(0, -1), _scrollControls, new Rect(0, 0, scrollViewRect.width - 17, _pointTransactions.CurrentPage.PointDeposits.Count * RowHeight));
            {
                float yOffset = 0;
                foreach (var d in _pointTransactions.CurrentPage.PointDeposits)
                {
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Points, 0, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Points, 0, scrollViewRect.width), RowHeight), d.DepositDate.ToString(DATE_FORMAT), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Points, 1, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Points, 1, scrollViewRect.width), RowHeight), d.Points.ToString(), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Points, 2, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Points, 2, scrollViewRect.width), RowHeight), d.DepositType.ToString(), BlueStonez.label_interparkmed_11pt);
                    yOffset += RowHeight;
                }
            }
            GUI.EndScrollView();
        }
    }

    private void DrawPointsButtons(Rect rect)
    {
        GUIStyle buttonStyle = BlueStonez.button;
        GUI.enabled = _pointTransactions.CurrentPageIndex != 0;
        if (GUITools.Button(new Rect(rect.x + 6, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_prevPageButtonLabel), buttonStyle))
        {
            _pointTransactions.CurrentPageIndex--;
            AsyncGetPointsDeposits();
        }
        GUI.enabled = true;

        if (_pointTransactions.ElementCount > 0)
        {
            GUI.Label(new Rect((rect.x + rect.width) / 2 - 100, rect.y + 5, 200, ButtonHeight), string.Format("Page {0} of {1}", _pointTransactions.CurrentPageIndex + 1, _pointTransactions.PageCount), BlueStonez.label_interparkbold_11pt);

            GUI.enabled = _pointTransactions.CurrentPageIndex + 1 < _pointTransactions.PageCount;
            if (GUITools.Button(new Rect(rect.x + rect.width - ButtonWidth - 2, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_nextPageButtonLabel), buttonStyle))
            {
                _pointTransactions.CurrentPageIndex++;
                AsyncGetPointsDeposits();
            }
            GUI.enabled = true;
        }
    }

    #endregion

    #region Credits

    private void DrawCreditsTableHeadingBar(Rect headingRect)
    {
        GUI.BeginGroup(headingRect);
        {
            for (int i = 0; i < _creditsTableColumnHeadingArray.Length; i++)
            {
                Rect buttonRect = new Rect(GetColumnOffset(AccountArea.Credits, i, headingRect.width), 0, GetColumnWidth(AccountArea.Credits, i, headingRect.width), headingRect.height);
                GUI.Button(buttonRect, string.Empty, BlueStonez.box_grey50);
                GUI.Label(buttonRect, new GUIContent(_creditsTableColumnHeadingArray[i]), BlueStonez.label_interparkmed_11pt);
            }
        }
        GUI.EndGroup();
    }

    private void DrawCreditsTableContent(Rect scrollViewRect)
    {
        GUI.Box(scrollViewRect, GUIContent.none, BlueStonez.window_standard_grey38);

        if (_creditTransactions.CurrentPage != null)
        {
            _scrollControls = GUI.BeginScrollView(scrollViewRect.ExpandBy(0, -1), _scrollControls, new Rect(0, 0, scrollViewRect.width - 17, _creditTransactions.CurrentPage.CurrencyDeposits.Count * RowHeight));
            {
                float yOffset = 0;
                foreach (var entry in _creditTransactions.CurrentPage.CurrencyDeposits)
                {
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 0, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 0, scrollViewRect.width), RowHeight), TextUtility.ShortenText(entry.TransactionKey, 20, true), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 1, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 1, scrollViewRect.width), RowHeight), entry.DepositDate.ToString(DATE_FORMAT), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 2, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 2, scrollViewRect.width), RowHeight), entry.CurrencyLabel + entry.Cash.ToString("#0.00"), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 3, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 3, scrollViewRect.width), RowHeight), entry.Credits.ToString(), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 4, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 4, scrollViewRect.width), RowHeight), entry.Points.ToString(), BlueStonez.label_interparkmed_11pt);
                    GUI.Label(new Rect(GetColumnOffset(AccountArea.Credits, 5, scrollViewRect.width), yOffset, GetColumnWidth(AccountArea.Credits, 5, scrollViewRect.width), RowHeight), TextUtility.ShortenText(entry.BundleName, 14, true), BlueStonez.label_interparkmed_11pt);
                    yOffset += RowHeight;
                }
            }
            GUI.EndScrollView();
        }
    }

    private void DrawCreditsButtons(Rect rect)
    {
        GUIStyle buttonStyle = BlueStonez.button;
        GUI.enabled = _creditTransactions.CurrentPageIndex != 0;
        if (GUITools.Button(new Rect(rect.x + 6, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_prevPageButtonLabel), buttonStyle))
        {
            _creditTransactions.CurrentPageIndex--;
            AsyncGetCurrencyDeposits();
        }
        GUI.enabled = true;

        if (_creditTransactions.ElementCount > 0)
        {
            GUI.Label(new Rect((rect.x + rect.width) / 2 - 100, rect.y + 5, 200, ButtonHeight), string.Format("Page {0} of {1}", _creditTransactions.CurrentPageIndex + 1, _creditTransactions.PageCount), BlueStonez.label_interparkbold_11pt);

            GUI.enabled = _creditTransactions.CurrentPageIndex + 1 < _creditTransactions.PageCount;
            if (GUITools.Button(new Rect(rect.x + rect.width - ButtonWidth - 2, rect.y + 5, ButtonWidth, ButtonHeight), new GUIContent(_nextPageButtonLabel), buttonStyle))
            {
                _creditTransactions.CurrentPageIndex++;
                AsyncGetCurrencyDeposits();
            }
            GUI.enabled = true;
        }
    }

    #endregion

    private void AsyncGetItemTransactions()
    {
        if (_itemTransactions.CurrentPageNeedsRefresh)
        {
            int nextPageIndex = _itemTransactions.CurrentPageIndex;
            UberStrike.WebService.Unity.UserWebServiceClient.GetItemTransactions(PlayerDataManager.CmidSecure, nextPageIndex + 1, ElementsPerPage,
                (ev) =>
                {
                    _itemTransactions.SetPage(nextPageIndex, ev);
                    _itemTransactions.ElementCount = ev.TotalCount;
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }

    private void AsyncGetCurrencyDeposits()
    {
        if (_creditTransactions.CurrentPageNeedsRefresh)
        {
            int nextPageIndex = _creditTransactions.CurrentPageIndex;
            UberStrike.WebService.Unity.UserWebServiceClient.GetCurrencyDeposits(PlayerDataManager.CmidSecure, nextPageIndex + 1, ElementsPerPage,
                (ev) =>
                {
                    _creditTransactions.SetPage(nextPageIndex, ev);
                    _creditTransactions.ElementCount = ev.TotalCount;
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }

    private void AsyncGetPointsDeposits()
    {
        if (_pointTransactions.CurrentPageNeedsRefresh)
        {
            int nextPageIndex = _pointTransactions.CurrentPageIndex;
            UberStrike.WebService.Unity.UserWebServiceClient.GetPointsDeposits(PlayerDataManager.CmidSecure, nextPageIndex + 1, ElementsPerPage,
                (ev) =>
                {
                    _pointTransactions.SetPage(nextPageIndex, ev);
                    _pointTransactions.ElementCount = ev.TotalCount;
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }

    public void GetCurrentTransactions()
    {
        switch ((TransactionType)_selectedTab)
        {
            case TransactionType.Credit:
                AsyncGetCurrencyDeposits();
                break;

            case TransactionType.Item:
                AsyncGetItemTransactions();
                break;

            case TransactionType.Point:
                AsyncGetPointsDeposits();
                break;
        }
    }
}