
public abstract class Ref<T>
{
    public static readonly Ref<T> Null = new NullRef<T>();

    public T this[long index]
    {
        get => GetAtIndex(index);
        set => SetAtIndex(index, value);
    }
    public T this[ulong index]
    {
        get => GetAtIndex((long)index);
        set => SetAtIndex((long)index, value);
    }

    public abstract Ref<T> AtIndex(long index);
    public Ref<T> AtIndex(ulong index) => AtIndex((long)index);

    protected abstract T GetAtIndex(long index);
    protected abstract void SetAtIndex(long index, T value);
    public virtual int ReadFromStream(Stream stream, int count)
    {
        throw new NotImplementedException();
    }
    public virtual int WriteToStream(Stream stream, int count)
    {
        throw new NotImplementedException();
    }
}

public class NullRef<T> : Ref<T>
{
    protected override T GetAtIndex(long index) => throw new InvalidOperationException();
    protected override void SetAtIndex(long index, T value) => throw new InvalidOperationException();
    public override Ref<T> AtIndex(long index) => throw new InvalidOperationException();
}

public class ConstRef<T> : Ref<T>
{
    private T Value;
    public ConstRef(T value)
    {
        this.Value = value;
    }
    protected override T GetAtIndex(long index)
    {
        if (index != 0) throw new InvalidOperationException();
        return Value;
    }
    protected override void SetAtIndex(long index, T value)
    {
        if (index != 0) throw new InvalidOperationException();
        this.Value = value;
    }
    public override Ref<T> AtIndex(long index)
    {
        if (index != 0) throw new InvalidOperationException();
        return this;
    }
}

public class ArrayRef<T> : Ref<T>
{
    protected T[] Data;
    protected long Index;

    public ArrayRef(T[] array, long index)
    {
        Data = array;
        Index = index;
    }

    protected override T GetAtIndex(long index) => Data[this.Index + index];
    protected override void SetAtIndex(long index, T value) => Data[this.Index + index] = value;
    public override Ref<T> AtIndex(long index)
    {
        return new ArrayRef<T>(Data, this.Index + index);
    }
}

public class ByteArrayRef : ArrayRef<byte>
{
    public ByteArrayRef(byte[] array, long index) : base(array, index)
    {
    }

    public override int ReadFromStream(Stream stream, int count)
    {
        return stream.Read(this.Data, (int)this.Index, count);
    }

    public override int WriteToStream(Stream stream, int count)
    {
        stream.Write(this.Data, (int)this.Index, (int)count);
        return count;
    }
}