namespace RawCSV;

public static class csv {
    public static string[][] parse(byte[] s) {
        var p = new csv_parser(s); List<string[]> rows = new(); List<string> row = new(); int r;
        while ((r = p.next(out var f)) != 0) {
            var b = s.AsSpan()[f];
            row.Add((r & csv_parser.QUOT) != 0 && b.IndexOf((byte)'"') >= 0 ? uneq(b) : Encoding.UTF8.GetString(b));
            if ((r & csv_parser.EOR) != 0) { rows.Add([..row]); row.Clear(); }
        }
        return [..rows];
    }
    static string uneq(ReadOnlySpan<byte> r) { Span<byte> t = stackalloc byte[r.Length]; return Encoding.UTF8.GetString(t[..csv_parser.unescape(r, t)]); }
    public static byte[] write(string[][] rows) {
        var buf = new byte[rows.Sum(r => r.Sum(f => f.Length * 4 + 3) + 2)];
        var w = new csv_writer(buf);
        foreach (var row in rows) { foreach (var f in row) w.field(Encoding.UTF8.GetBytes(f)); w.end_row(); }
        return buf[..w.length];
    }
}