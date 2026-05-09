namespace RawCSV;

public ref struct csv_writer
{
    Span<byte> _buf; int _pos = 0; bool _sep = false;
    public csv_writer(Span<byte> buf) => _buf = buf;
    public int length => _pos;

    public void field(ReadOnlySpan<byte> v)
    {
        ref byte dst = ref MemoryMarshal.GetReference(_buf);
        if (_sep) Unsafe.Add(ref dst, _pos++) = (byte)',';
        _sep = true;
        if (v.IndexOfAny(",\"\r\n"u8) < 0) { v.CopyTo(_buf[_pos..]); _pos += v.Length; return; }
        Unsafe.Add(ref dst, _pos++) = (byte)'"';
        int qi, si = 0;
        while ((qi = v[si..].IndexOf((byte)'"')) >= 0) { v.Slice(si, qi).CopyTo(_buf[_pos..]); _pos += qi; Unsafe.Add(ref dst, _pos++) = (byte)'"'; Unsafe.Add(ref dst, _pos++) = (byte)'"'; si += qi + 1; }
        v[si..].CopyTo(_buf[_pos..]); _pos += v.Length - si;
        Unsafe.Add(ref dst, _pos++) = (byte)'"';
    }

    public void end_row() { ref byte dst = ref MemoryMarshal.GetReference(_buf); Unsafe.Add(ref dst, _pos++) = (byte)'\r'; Unsafe.Add(ref dst, _pos++) = (byte)'\n'; _sep = false; }
}