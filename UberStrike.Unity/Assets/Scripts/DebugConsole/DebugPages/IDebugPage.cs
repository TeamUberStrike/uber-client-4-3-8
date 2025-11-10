using System.Collections.Generic;

public interface IDebugPage
{
    string Title { get; }

    void Draw();
}

public class CompareDebugPage : IComparer<IDebugPage>
{
    public int Compare(IDebugPage a, IDebugPage b) { return a.Title.CompareTo(b.Title); }
}