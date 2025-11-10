using System.Collections.Generic;

public class PopupStack<T>
{
    private List<T> items = new List<T>();

    public int Count
    {
        get { return items.Count; }
    }

    public T Peek()
    {
        if (items.Count > 0)
        {
            T temp = items[items.Count - 1];

            return temp;
        }
        else
            return default(T);
    }

    public void Push(T item)
    {
        items.Add(item);
    }

    public T Pop()
    {
        if (items.Count > 0)
        {
            T temp = items[items.Count - 1];
            items.Remove(temp);
            return temp;
        }
        else
            return default(T);
    }

    public void Remove(int itemAtPosition)
    {
        items.RemoveAt(itemAtPosition);
    }

    public void Remove(T item)
    {
        items.Remove(item);
    }

    public void CLear()
    {
        items.Clear();
    }
}