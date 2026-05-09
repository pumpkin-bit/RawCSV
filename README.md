# RawCSV

I was inspired by an example of the smallest CSV parser... And I wondered if it could be even smaller while still maintaining user-friendliness and remaining a NuGet package. In the end, I managed to get it down to 83 lines of code, provided that it

* RFC4180 format
* A user-friendly API for library users
* Support for any Unicode, emojis, and hieroglyphs
* Support for legacy and local encodings
* All types of line breaks
* Nested multi-line texts

# Example of use 

when you need to get the data as quickly and easily as possible

```csharp
using RawCSV;
using System.IO;

// parse a csv file
byte[] fileBytes = File.ReadAllBytes("data.csv");
string[][] rows = csv.parse(fileBytes);

// work with the data
foreach (var row in rows) {
    Console.WriteLine($"Name: {row[0]}, Email: {row[1]}");
}

// write it back to bytes
byte[] newCsvBytes = csv.write(rows);
```

**You may also need a lower API level without unnecessary memory allocations, so here is an example below**

```csharp
using RawCSV;
using System;
using System.IO;

byte[] fileBytes = File.ReadAllBytes("huge_data.csv");
var parser = new csv_parser(fileBytes);

// the loop runs over the file, outputting ranges of fields
while (parser.next(out Range fieldIndices) != 0) 
{
    // access the field directly from the original buffer
    ReadOnlySpan<byte> field = fileBytes.AsSpan(fieldIndices);
    
    // process your bytes instantly...
}
```

## Content 

* [Test CSV Parser](test/RawCSV.Tests/parser_tests.cs)
* [BenchmarkDotnet](bench/RawCSV.Bench/Program.cs)


## Install 

Install via [NuGet](https://www.nuget.org/packages/RawCSV) or:

```bash
dotnet add package RawCSV --version 1.0.0
```

## Performance

Benchmark results on an Intel Core i5-8600 CPU running .NET 8.0.

| Method | Mean | Speed | Allocated |
| --- | --- | --- | --- |
| `raw: 10k plain` | 1.306 ms | **~275 MB/s** | **0 B** |
| `raw: 10k quoted` | 1.071 ms | **~298 MB/s** | **0 B** |
| `raw: worldcities (150MB)` | 610.73 ms | **~248 MB/s** | **0 B** |
| `api: parse 10k plain` | 6.614 ms | ~54 MB/s | 2,912 KB |
| `api: write 10k rows` | 2.603 ms | ~138 MB/s | 3,564 KB |


