using UnityEngine;

class SearchBarGUI
{
    public string FilterText { get; private set; }

    public bool IsSearching { get { return !string.IsNullOrEmpty(FilterText); } }

    public SearchBarGUI(string name)
    {
        _guiName = name;
        FilterText = string.Empty;
    }

    public void Draw(Rect rect)
    {
        GUI.SetNextControlName(_guiName);
        FilterText = GUI.TextField(new Rect(rect.x, rect.y, IsSearching ? rect.width - 22 : rect.width, rect.height), 
            FilterText, BlueStonez.textField);
        if (string.IsNullOrEmpty(FilterText) && GUI.GetNameOfFocusedControl() != _guiName)
        {
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.Label(rect, " " + LocalizedStrings.Search, BlueStonez.label_interparkbold_11pt_left);
            GUI.color = Color.white;
        }

        if (IsSearching && GUITools.Button(new Rect(rect.x + rect.width - 20, 8, 20, 20),
                                           new GUIContent("x"), BlueStonez.buttondark_medium))
        {
            ClearFilter();
            GUIUtility.hotControl = 1;
        }
    }

    public void ClearFilter()
    {
        FilterText = string.Empty;
    }

    public bool CheckIfPassFilter(string text)
    {
        return !IsSearching || text.ToLower().Contains(FilterText.ToLower());
    }

    private string _guiName;
}