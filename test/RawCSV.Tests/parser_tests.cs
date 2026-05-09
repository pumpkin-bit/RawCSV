namespace RawCSV.Tests;

using System;
using System.Text;
using System.Collections.Generic;
using Xunit;

public class parser_tests
{
    static byte[] utf8(string s) => Encoding.UTF8.GetBytes(s);

    static string[] parse_row(string csv)
    {
        var data = utf8(csv);
        var p = new csv_parser(data);
        var fields = new List<string>();
        int r;
        while ((r = p.next(out var f)) != 0)
        {
            var raw = data.AsSpan()[f];
            if ((r & csv_parser.QUOT) != 0 && raw.IndexOf((byte)'"') >= 0)
            {
                Span<byte> tmp = stackalloc byte[raw.Length];
                int len = csv_parser.unescape(raw, tmp);
                fields.Add(Encoding.UTF8.GetString(tmp[..len]));
            }
            else
            {
                fields.Add(Encoding.UTF8.GetString(raw));
            }
            if ((r & csv_parser.EOR) != 0) break;
        }
        return fields.ToArray();
    }

    static string[][] parse_all(string csv)
    {
        var data = utf8(csv);
        var p = new csv_parser(data);
        var rows = new List<string[]>();
        var fields = new List<string>();
        int r;
        while ((r = p.next(out var f)) != 0)
        {
            var raw = data.AsSpan()[f];
            if ((r & csv_parser.QUOT) != 0 && raw.IndexOf((byte)'"') >= 0)
            {
                Span<byte> tmp = stackalloc byte[raw.Length];
                int len = csv_parser.unescape(raw, tmp);
                fields.Add(Encoding.UTF8.GetString(tmp[..len]));
            }
            else
            {
                fields.Add(Encoding.UTF8.GetString(raw));
            }
            if ((r & csv_parser.EOR) != 0)
            {
                rows.Add(fields.ToArray());
                fields.Clear();
            }
        }
        return rows.ToArray();
    }

    // RFC4180

    [Fact] public void simple_fields() => Assert.Equal(new[] { "aaa", "bbb", "ccc" }, parse_row("aaa,bbb,ccc"));
    [Fact] public void quoted_fields() => Assert.Equal(new[] { "aaa", "bbb", "ccc" }, parse_row("\"aaa\",\"bbb\",\"ccc\""));
    [Fact] public void escaped_quotes() => Assert.Equal(new[] { "a\"b" }, parse_row("\"a\"\"b\""));
    [Fact] public void embedded_crlf() => Assert.Equal(new[] { "a\r\nb", "c" }, parse_row("\"a\r\nb\",c"));
    [Fact] public void embedded_comma() => Assert.Equal(new[] { "a,b", "c" }, parse_row("\"a,b\",c"));
    [Fact] public void empty_fields() => Assert.Equal(new[] { "a", "", "b" }, parse_row("a,,b"));
    [Fact] public void single_empty_quoted() => Assert.Equal(new[] { "" }, parse_row("\"\""));
    [Fact] public void trailing_crlf() { var r = parse_all("a,b\r\n"); Assert.Single(r); Assert.Equal(new[] { "a", "b" }, r[0]); }
    [Fact] public void no_trailing_crlf() { var r = parse_all("a,b"); Assert.Single(r); Assert.Equal(new[] { "a", "b" }, r[0]); }
    [Fact] public void lf_only() { var r = parse_all("a,b\nc,d"); Assert.Equal(2, r.Length); }

    [Fact]
    public void multiple_rows()
    {
        var r = parse_all("a,b\r\nc,d\r\ne,f");
        Assert.Equal(3, r.Length);
        Assert.Equal(new[] { "a", "b" }, r[0]);
        Assert.Equal(new[] { "c", "d" }, r[1]);
        Assert.Equal(new[] { "e", "f" }, r[2]);
    }

    [Fact]
    public void complex_rfc4180()
    {
        var r = parse_all("field_name,field_name,field_name\r\naaa,bbb,ccc\r\nzzz,yyy,xxx");
        Assert.Equal(3, r.Length);
        Assert.Equal(new[] { "field_name", "field_name", "field_name" }, r[0]);
    }

    // unicode

    [Fact] public void unicode_emoji() => Assert.Equal(new[] { "\U0001F389", "мир", "hello" }, parse_row("\U0001F389,мир,hello"));
    [Fact] public void quoted_unicode() => Assert.Equal(new[] { "\U0001F600", "привет" }, parse_row("\"\U0001F600\",\"привет\""));
    [Fact] public void surrogate_pair_emoji() => Assert.Equal(new[] { "\U0001F468\u200D\U0001F469\u200D\U0001F467" }, parse_row("\U0001F468\u200D\U0001F469\u200D\U0001F467"));
    [Fact] public void mixed_emoji_and_text() => Assert.Equal(new[] { "hello\U0001F30D", "\U0001F525fire" }, parse_row("hello\U0001F30D,\U0001F525fire"));
    [Fact] public void quoted_emoji_with_comma() => Assert.Equal(new[] { "\U0001F600,\U0001F602" }, parse_row("\"\U0001F600,\U0001F602\""));
    [Fact] public void japanese_chinese_arabic() => Assert.Equal(new[] { "日本語", "中文", "العربية" }, parse_row("日本語,中文,العربية"));
    [Fact] public void flag_emoji_sequence() => Assert.Equal(new[] { "\U0001F1FA\U0001F1E6", "UA" }, parse_row("\U0001F1FA\U0001F1E6,UA"));

    // ascii

    [Fact] public void all_ascii_control_chars_in_quoted()
    {
        var input = "\"" + "\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0b\x0c\x0e\x0f" + "\"";
        var row = parse_row(input);
        Assert.Single(row);
        Assert.Equal("\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0b\x0c\x0e\x0f", row[0]);
    }

    [Fact] public void tab_is_not_delimiter() => Assert.Equal(new[] { "a\tb" }, parse_row("a\tb"));
    [Fact] public void space_preserved() => Assert.Equal(new[] { " a ", " b " }, parse_row(" a , b "));
    [Fact] public void null_byte_in_field() => Assert.Equal(new[] { "a\0b", "c" }, parse_row("a\0b,c"));
    [Fact] public void max_ascii_byte() => Assert.Equal(new[] { "\x7F", "ok" }, parse_row("\x7F,ok"));
    [Fact] public void high_bytes_latin1() => Assert.Equal(new[] { "\u00FF", "ok" }, parse_row("\u00FF,ok"));

    // edge cases

    [Fact] public void empty_input() => Assert.Equal(0, new csv_parser(ReadOnlySpan<byte>.Empty).next(out _));
    [Fact] public void single_byte() => Assert.Equal(new[] { "x" }, parse_row("x"));
    [Fact] public void single_comma() => Assert.Equal(new[] { "", "" }, parse_row(","));
    [Fact] public void only_commas() => Assert.Equal(new[] { "", "", "" }, parse_row(",,"));
    [Fact] public void only_crlf() { var r = parse_all("\r\n"); Assert.Single(r); Assert.Equal(new[] { "" }, r[0]); }
    [Fact] public void only_lf() { var r = parse_all("\n"); Assert.Single(r); Assert.Equal(new[] { "" }, r[0]); }
    [Fact] public void only_cr() { var r = parse_all("\r"); Assert.Single(r); Assert.Equal(new[] { "" }, r[0]); }
    [Fact] public void crlf_crlf() { var r = parse_all("\r\n\r\n"); Assert.Equal(2, r.Length); }
    [Fact] public void quoted_empty_comma_quoted() => Assert.Equal(new[] { "", "", "" }, parse_row("\"\",\"\",\"\""));
    [Fact] public void field_is_just_quotes() => Assert.Equal(new[] { "\"" }, parse_row("\"\"\"\""));

    [Fact]
    public void many_escaped_quotes()
    {
        var row = parse_row("\"\"\"\"\"\"\"\"");
        Assert.Equal(new[] { "\"\"\"" }, row);
    }

    [Fact]
    public void large_field_1kb()
    {
        var big = new string('X', 1024);
        var row = parse_row(big + ",y");
        Assert.Equal(1024, row[0].Length);
        Assert.Equal("y", row[1]);
    }

    [Fact]
    public void large_quoted_field_with_escapes()
    {
        var inner = new string('A', 500) + "\"\"" + new string('B', 500);
        var row = parse_row("\"" + inner + "\"");
        Assert.Equal(new string('A', 500) + "\"" + new string('B', 500), row[0]);
    }

    [Fact]
    public void hundred_columns()
    {
        var csv = string.Join(",", System.Linq.Enumerable.Range(0, 100).Select(i => i.ToString()));
        var row = parse_row(csv);
        Assert.Equal(100, row.Length);
        Assert.Equal("0", row[0]);
        Assert.Equal("99", row[99]);
    }

    [Fact]
    public void thousand_rows()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++) sb.Append($"r{i},v{i}\r\n");
        var r = parse_all(sb.ToString());
        Assert.Equal(1000, r.Length);
        Assert.Equal("r0", r[0][0]);
        Assert.Equal("r999", r[999][0]);
    }

    // bitmask

    [Fact]
    public void flags_simple()
    {
        var p = new csv_parser(utf8("a,b"));
        int r1 = p.next(out _);
        Assert.Equal(csv_parser.DATA, r1);
        int r2 = p.next(out _);
        Assert.Equal(csv_parser.DATA | csv_parser.EOR, r2);
        Assert.Equal(0, p.next(out _));
    }

    [Fact]
    public void flags_quoted()
    {
        var p = new csv_parser(utf8("\"x\""));
        int r = p.next(out _);
        Assert.NotEqual(0, r & csv_parser.QUOT);
        Assert.NotEqual(0, r & csv_parser.EOR);
    }

    // unescape

    [Fact] public void unescape_no_quotes()
    {
        var src = "hello"u8;
        Span<byte> dst = stackalloc byte[16];
        int n = csv_parser.unescape(src, dst);
        Assert.Equal(5, n);
        Assert.Equal("hello", Encoding.UTF8.GetString(dst[..n]));
    }

    [Fact] public void unescape_all_quotes()
    {
        var src = "\"\"\"\"\"\""u8;
        Span<byte> dst = stackalloc byte[16];
        int n = csv_parser.unescape(src, dst);
        Assert.Equal(3, n);
        Assert.Equal("\"\"\"", Encoding.UTF8.GetString(dst[..n]));
    }
}

public class writer_tests
{
    [Fact] public void simple_row()
    {
        Span<byte> buf = stackalloc byte[256];
        var w = new csv_writer(buf);
        w.field("aaa"u8); w.field("bbb"u8); w.field("ccc"u8); w.end_row();
        Assert.Equal("aaa,bbb,ccc\r\n", Encoding.UTF8.GetString(buf[..w.length]));
    }

    [Fact] public void auto_quoting_comma()
    {
        Span<byte> buf = stackalloc byte[256];
        var w = new csv_writer(buf);
        w.field("a,b"u8); w.end_row();
        Assert.Equal("\"a,b\"\r\n", Encoding.UTF8.GetString(buf[..w.length]));
    }

    [Fact] public void auto_escaping_quotes()
    {
        Span<byte> buf = stackalloc byte[256];
        var w = new csv_writer(buf);
        w.field("a\"b"u8); w.end_row();
        Assert.Equal("\"a\"\"b\"\r\n", Encoding.UTF8.GetString(buf[..w.length]));
    }

    [Fact] public void auto_quoting_newline()
    {
        Span<byte> buf = stackalloc byte[256];
        var w = new csv_writer(buf);
        w.field("a\r\nb"u8); w.end_row();
        Assert.Equal("\"a\r\nb\"\r\n", Encoding.UTF8.GetString(buf[..w.length]));
    }

    [Fact] public void empty_field()
    {
        Span<byte> buf = stackalloc byte[256];
        var w = new csv_writer(buf);
        w.field(ReadOnlySpan<byte>.Empty); w.field("b"u8); w.end_row();
        Assert.Equal(",b\r\n", Encoding.UTF8.GetString(buf[..w.length]));
    }

    [Fact] public void unicode_roundtrip()
    {
        var original = "\U0001F525,\"a,b\",\"c\"\"d\"\r\n";
        var data = Encoding.UTF8.GetBytes(original);
        var p = new csv_parser(data);
        var fields = new System.Collections.Generic.List<byte[]>();
        int r;
        while ((r = p.next(out var f)) != 0)
        {
            var raw = data.AsSpan()[f];
            if ((r & csv_parser.QUOT) != 0 && raw.IndexOf((byte)'"') >= 0)
            {
                var tmp = new byte[raw.Length];
                int len = csv_parser.unescape(raw, tmp);
                fields.Add(tmp[..len]);
            }
            else fields.Add(raw.ToArray());
            if ((r & csv_parser.EOR) != 0) break;
        }
        Span<byte> buf = stackalloc byte[512];
        var w = new csv_writer(buf);
        foreach (var field in fields) w.field(field);
        w.end_row();
        Assert.Equal(original, Encoding.UTF8.GetString(buf[..w.length]));
    }
}

public class api_tests
{
    [Fact] public void api_parse_simple() { var r = csv.parse("aaa,bbb\r\nccc,ddd\r\n"u8.ToArray()); Assert.Equal(2, r.Length); Assert.Equal("aaa", r[0][0]); Assert.Equal("ddd", r[1][1]); }
    [Fact] public void api_parse_quoted() { var r = csv.parse("\"a,b\",\"c\"\"d\"\r\n"u8.ToArray()); Assert.Equal("a,b", r[0][0]); Assert.Equal("c\"d", r[0][1]); }
    [Fact] public void api_parse_unicode() { var r = csv.parse(System.Text.Encoding.UTF8.GetBytes("\U0001F525,мир\r\n")); Assert.Equal("\U0001F525", r[0][0]); Assert.Equal("мир", r[0][1]); }
    [Fact] public void api_write_simple() { var b = csv.write(new[] { new[] { "a", "b" }, new[] { "c", "d" } }); Assert.Equal("a,b\r\nc,d\r\n", System.Text.Encoding.UTF8.GetString(b)); }
    [Fact] public void api_write_special_chars() { var b = csv.write(new[] { new[] { "a,b", "c\"d" } }); Assert.Equal("\"a,b\",\"c\"\"d\"\r\n", System.Text.Encoding.UTF8.GetString(b)); }
    [Fact] public void api_roundtrip() { var rows = new[] { new[] { "hello", "a,b", "c\"d" }, new[] { "\U0001F525", "мир", "" } }; Assert.Equal(rows, csv.parse(csv.write(rows))); }
}
