public struct CString
{
    private string Data = "";

    public CString()
    {

    }

    public CString(string str)
    {
        this.Data = str;
    }

    public static implicit operator CString(string input) => new CString(input);
    public static implicit operator string(CString input) => input.Data;

    public CString Left(int count)
    {
        if (count >= Data.Length) return Data;
        else return Data[..count];
    }

    public CString Right(int count)
    {
        if (count >= Data.Length) return Data;
        else return Data[^count..];
    }

    public int GetLength() => Data.Length;

    public bool IsEmpty() => Data.Length == 0;

    public int Find(char c, int start = 0) => Data.IndexOf(c, start);
    public int ReverseFind(char c) => Data.LastIndexOf(c);
    public void MakeLower() => Data = Data.ToLower();
    public void MakeUpper() => Data = Data.ToUpper();
}