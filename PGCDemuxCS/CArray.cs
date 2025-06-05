
public struct CArray<T> where T : new()
{
    private List<T> Data = new();

    public CArray()
    {
    }

    public T this[int index]
    {
        get => Data[index];
        set => Data[index] = value;
    }

    //public Ref<T> GetRef(int index) => new ArrayRef<T>(this.array, index);

    public int GetSize() => Data.Count;

    public void InsertAt(int index, T element) => this.Data.Insert(index, element);

    public void SetAtGrow(int nIndex, T newElement)
    {
        while (this.Data.Count < nIndex - 1)
        {
            this.Data.Add(new T());
        }
        this.Data.Insert(nIndex, newElement);
    }

    public void RemoveAll()
    {
        this.Data.Clear();
    }
}