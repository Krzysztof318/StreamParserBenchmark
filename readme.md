| Method                  | Runtime            | Mean       | Error     | StdDev    | Gen0     | Gen1    | Gen2    | Allocated  |
|------------------------ |------------------- |-----------:|----------:|----------:|---------:|--------:|--------:|-----------:|
| StephenPropositionNoSse | .NET 8.0           | 1,578.1 us |   8.67 us |   8.11 us | 156.2500 | 50.7813 | 50.7813 |  725.52 KB |
| StreamJsonParserNoSse   | .NET 8.0           | 1,153.0 us |  17.84 us |  16.69 us | 287.1094 | 50.7813 | 50.7813 | 1417.93 KB |
| StreamJsonParserSseData | .NET 8.0           | 1,203.0 us |  18.99 us |  17.76 us | 349.6094 | 48.8281 | 48.8281 | 1738.52 KB |
| AzureJsonParserSseData  | .NET 8.0           |   945.5 us |   2.98 us |   2.79 us | 249.0234 | 49.8047 | 49.8047 | 1150.11 KB |
| StephenPropositionNoSse | .NET Framework 4.7 | 6,451.4 us | 124.18 us | 116.16 us | 148.4375 | 78.1250 | 46.8750 |  730.42 KB |
| StreamJsonParserNoSse   | .NET Framework 4.7 | 4,410.5 us |  39.37 us |  36.83 us | 335.9375 | 85.9375 | 46.8750 | 1685.39 KB |
| StreamJsonParserSseData | .NET Framework 4.7 | 4,492.0 us |  74.93 us |  86.29 us | 437.5000 | 54.6875 | 46.8750 | 2013.36 KB |
| AzureJsonParserSseData  | .NET Framework 4.7 | 3,727.3 us |  58.97 us |  49.25 us | 296.8750 | 85.9375 | 46.8750 | 1425.97 KB |

for 160 KB file

StephenProposition doesn't work with sse data when input contains also other data than json. It's not a problem for StreamJsonParser.
StreamJsonParser tolerate garbage input like `fiddf { "object": { "name": "hello" } } fdsfgf24rf` >> `{ "object": { "name": "hello" } }`

`dotnet run -C Release /optimize --framework net8.0`

StephenProposition requires also adding new dependecies to SK:
https://www.nuget.org/packages/System.IO.Pipelines/
