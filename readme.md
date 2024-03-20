| Method             | Job                | Runtime            | Mean     | Error     | StdDev    | Gen0     | Gen1    | Gen2    | Allocated  |
|------------------- |------------------- |------------------- |---------:|----------:|----------:|---------:|--------:|--------:|-----------:|
| StephenProposition | .NET 8.0           | .NET 8.0           | 1.640 ms | 0.0324 ms | 0.0304 ms | 156.2500 | 50.7813 | 50.7813 |  725.52 KB |
| StreamJsonParser   | .NET 8.0           | .NET 8.0           | 1.176 ms | 0.0076 ms | 0.0068 ms | 287.1094 | 50.7813 | 50.7813 | 1417.93 KB |
| StephenProposition | .NET Framework 4.7 | .NET Framework 4.7 | 6.415 ms | 0.0363 ms | 0.0284 ms | 148.4375 | 78.1250 | 46.8750 |  730.42 KB |
| StreamJsonParser   | .NET Framework 4.7 | .NET Framework 4.7 | 4.432 ms | 0.0139 ms | 0.0123 ms | 335.9375 | 85.9375 | 46.8750 | 1685.39 KB |

for 164 KB file

StephenProposition doesn't work with sse data when input contains also other data than json. It's not a problem for StreamJsonParser.

`dotnet run -C Release /optimize --framework net8.0`
