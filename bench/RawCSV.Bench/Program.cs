using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using RawCSV;

BenchmarkRunner.Run<parse_bench>(
    DefaultConfig.Instance
        .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
        .AddJob(Job.ShortRun.WithWarmupCount(3).WithIterationCount(10))
);

[MemoryDiagnoser]
public class parse_bench
{
    byte[] _simple = default!;
    byte[] _quoted = default!;
    byte[] _mixed  = default!;
    byte[] _large  = default!;
    byte[] _worldcities = default!;
    string[][] _rows = default!;

    [GlobalSetup]
    public void setup()
    {
        var sb = new StringBuilder();

        sb.Clear();
        for (int i = 0; i < 10_000; i++) sb.Append("field1,field2,field3,field4,field5\r\n");
        _simple = Encoding.UTF8.GetBytes(sb.ToString());

        sb.Clear();
        for (int i = 0; i < 10_000; i++) sb.Append("\"a,b\",\"c\"\"d\",\"e\r\nf\",plain,\"last\"\r\n");
        _quoted = Encoding.UTF8.GetBytes(sb.ToString());

        sb.Clear();
        for (int i = 0; i < 10_000; i++) sb.Append($"id{i},\"\U0001F525hot\",plain,\"a,b\",end\r\n");
        _mixed = Encoding.UTF8.GetBytes(sb.ToString());

        sb.Clear();
        for (int i = 0; i < 100_000; i++) sb.Append($"{i},value{i},end\r\n");
        _large = Encoding.UTF8.GetBytes(sb.ToString());

        // To test on a real 150MB dataset, download worldcitiespop.csv and set your path below:
        // _worldcities = System.IO.File.ReadAllBytes(@"C:\path\to\worldcitiespop.csv");

        _rows = csv.parse(_simple);
    }

    [Benchmark(Description = "raw: 10k plain")]
    public int raw_simple() { var p = new csv_parser(_simple); int n = 0; while (p.next(out _) != 0) n++; return n; }

    [Benchmark(Description = "raw: 10k quoted")]
    public int raw_quoted() { var p = new csv_parser(_quoted); int n = 0; while (p.next(out _) != 0) n++; return n; }

    [Benchmark(Description = "raw: 10k mixed+emoji")]
    public int raw_mixed()  { var p = new csv_parser(_mixed);  int n = 0; while (p.next(out _) != 0) n++; return n; }

    [Benchmark(Description = "raw: 100k throughput")]
    public int raw_large()  { var p = new csv_parser(_large);  int n = 0; while (p.next(out _) != 0) n++; return n; }

    // Uncomment the code below after configuring the path in setup() to run the 150MB benchmark:
    // [Benchmark(Description = "raw: worldcities (150MB)")]
    // public int raw_worldcities() { var p = new csv_parser(_worldcities); int n = 0; while (p.next(out _) != 0) n++; return n; }

    [Benchmark(Description = "api: parse 10k plain")]
    public string[][] api_parse_simple() => csv.parse(_simple);

    [Benchmark(Description = "api: parse 10k quoted")]
    public string[][] api_parse_quoted() => csv.parse(_quoted);

    [Benchmark(Description = "api: parse 10k mixed+emoji")]
    public string[][] api_parse_mixed()  => csv.parse(_mixed);

    [Benchmark(Description = "api: write 10k rows")]
    public byte[] api_write() => csv.write(_rows);
}
