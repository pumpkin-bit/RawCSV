[module: SkipLocalsInit]
namespace RawCSV;

public ref struct csv_parser
{
    static readonly SearchValues<byte> _sv = SearchValues.Create(",\r\n"u8);
    public const int DATA = 1, EOR = 2, QUOT = 4;
    ReadOnlySpan<byte> _buf; int _pos = 0; bool _tail = false;
    public csv_parser(ReadOnlySpan<byte> data) => _buf = data;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int next(out Range field)
    {
        if (_pos >= _buf.Length) { var t = _tail; _tail = false; field = t ? _pos.._pos : default; return t ? DATA | EOR : 0; }
        _tail = false;
        int s, i, r;
        ref byte ptr = ref MemoryMarshal.GetReference(_buf);
        if (Unsafe.Add(ref ptr, _pos) != (byte)'"') goto plain;
        s = ++_pos;
        while (_pos < _buf.Length)
        {
            i = _buf[_pos..].IndexOf((byte)'"');
            if (i < 0) break;
            _pos += i;
            if (_pos + 1 < _buf.Length && Unsafe.Add(ref ptr, _pos + 1) == (byte)'"') { _pos += 2; continue; }
            field = s.._pos; _pos++; r = DATA | QUOT; goto eat;
        }
        field = s.._buf.Length; _pos = _buf.Length; return DATA | EOR | QUOT;
        plain:
        s = _pos; i = _buf[_pos..].IndexOfAny(_sv);
        if (i < 0) { _pos = _buf.Length; field = s.._pos; return DATA | EOR; }
        _pos += i; field = s.._pos; r = DATA; goto eat;
        eat:
        if (_pos < _buf.Length && Unsafe.Add(ref ptr, _pos) == (byte)',') { _pos++; _tail = true; return r; }
        bool c1 = _pos < _buf.Length && Unsafe.Add(ref ptr, _pos) == (byte)'\r'; _pos += Unsafe.As<bool, byte>(ref c1);
        bool c2 = _pos < _buf.Length && Unsafe.Add(ref ptr, _pos) == (byte)'\n'; _pos += Unsafe.As<bool, byte>(ref c2);
        return r | EOR;
    }

    public static int unescape(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        int w = 0;
        for (int i = 0; i < src.Length; dst[w++] = src[i++])
            if (src[i] == (byte)'"' && i + 1 < src.Length && src[i + 1] == (byte)'"') i++;
        return w;
    }
}