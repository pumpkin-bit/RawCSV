namespace RawCSV;

using System;

public ref struct csv_writer
{
    Span<byte> _buf;
    int _pos;
    bool _sep;

    public csv_writer(Span<byte> buf) { _buf = buf; _pos = 0; _sep = false; }
    public int length => _pos;

    public void field(ReadOnlySpan<byte> v)
    {
        if (_sep) _buf[_pos++] = (byte)',';
        _sep = true;
        if (v.IndexOfAny(",\"\r\n"u8) < 0) { v.CopyTo(_buf[_pos..]); _pos += v.Length; return; }
        _buf[_pos++] = (byte)'"';
        for (int i = 0; i < v.Length; _buf[_pos++] = v[i++])
            if (v[i] == (byte)'"') _buf[_pos++] = (byte)'"';
        _buf[_pos++] = (byte)'"';
    }

    public void end_row() { _buf[_pos++] = (byte)'\r'; _buf[_pos++] = (byte)'\n'; _sep = false; }
}