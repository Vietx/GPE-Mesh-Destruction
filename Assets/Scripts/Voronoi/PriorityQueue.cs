using System;
using System.Collections.Generic;
using System.IO;

public interface IIndexed
{
    int HeapIndex { get; set; }
}

public class PriorityQueue<T> where T : class, IComparable<T>, IIndexed
{
    private readonly List<T> _elements = new();
    private readonly IComparer<T> _comparer;

    public PriorityQueue(IComparer<T> comparer = null)
    {
        _comparer = comparer ?? Comparer<T>.Default;
    }

    public bool IsEmpty() => _elements.Count == 0;
    public int Count() => _elements.Count;

    public void Push(T elem)
    {
        elem.HeapIndex = _elements.Count;
        _elements.Add(elem);
        SiftUp(elem.HeapIndex);
    }

    public T Pop()
    {
        if (_elements.Count == 0) return null;

        int last = _elements.Count - 1;
        if (last != 0) Swap(0, last);

        T top = _elements[last];
        _elements.RemoveAt(last);
        top.HeapIndex = -1;

        if (_elements.Count > 0) SiftDown(0);
        return top;
    }

    /// <summary>
    /// Reheapify element currently at index i (use after its priority changed).
    /// </summary>
    public void Update(int i)
    {
        if ((uint)i >= (uint)_elements.Count) return;
        int parent = GetParent(i);
        if (parent >= 0 && Compare(_elements[parent], _elements[i]) < 0)
            SiftUp(i);
        else
            SiftDown(i);
    }

    /// <summary>
    /// Reheapify a known item (faster if it tracks its HeapIndex).
    /// </summary>
    public void UpdateItem(T item)
    {
        if (item == null) return;
        int i = item.HeapIndex;
        if ((uint)i >= (uint)_elements.Count) return;
        Update(i);
    }

    public void RemoveAt(int i)
    {
        if ((uint)i >= (uint)_elements.Count) return;

        int last = _elements.Count - 1;
        if (i != last) Swap(i, last);

        T removed = _elements[last];
        _elements.RemoveAt(last);
        removed.HeapIndex = -1;

        if (i < _elements.Count) Update(i);
    }

    public void Clear()
    {
        foreach (var e in _elements) e.HeapIndex = -1;
        _elements.Clear();
    }

    // Debug print (optional)
    public void Print(TextWriter writer, int i = 0, string indent = "")
    {
        if (i >= _elements.Count) return;
        writer.WriteLine($"{indent}{_elements[i]}");
        Print(writer, GetLeft(i), indent + "\t");
        Print(writer, GetRight(i), indent + "\t");
    }

    // --- heap internals ---

    private static int GetParent(int i) => i == 0 ? -1 : (i - 1) / 2;
    private static int GetLeft(int i) => 2 * i + 1;
    private static int GetRight(int i) => 2 * i + 2;

    private int Compare(T a, T b) => _comparer.Compare(a, b); // >0 means a has higher priority

    private void SiftDown(int i)
    {
        int count = _elements.Count;
        while (true)
        {
            int left = GetLeft(i);
            int right = GetRight(i);
            int best = i;

            if (left < count && Compare(_elements[best], _elements[left]) < 0) best = left;
            if (right < count && Compare(_elements[best], _elements[right]) < 0) best = right;

            if (best == i) break;
            Swap(i, best);
            i = best;
        }
    }

    private void SiftUp(int i)
    {
        int parent = GetParent(i);
        while (parent >= 0 && Compare(_elements[parent], _elements[i]) < 0)
        {
            Swap(i, parent);
            i = parent;
            parent = GetParent(i);
        }
    }

    private void Swap(int i, int j)
    {
        if (i == j) return;
        var tmp = _elements[i];
        _elements[i] = _elements[j];
        _elements[j] = tmp;

        _elements[i].HeapIndex = i;
        _elements[j].HeapIndex = j;
    }
}

//this part is copied from ChatGPT