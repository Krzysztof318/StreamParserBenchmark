
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ConsoleApp5;

// DefaultConfig.Instance
//     .AddJob(Job
//         .MediumRun
//         .WithLaunchCount(1)
//         .WithToolchain(InProcessEmitToolchain.Instance));

BenchmarkRunner.Run<Bench>();

[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class Bench
{
    string data = File.ReadAllText(@"test.txt");
    
    public Bench()
    {
        
    }

    [Benchmark]
    public async Task StephenProposition()
    {
        var stream = LoadDataToStream();
        var class1 = new StephenProposition();
        var reader = PipeReader.Create(stream);
        await foreach (var jsonDocument in class1.ParseJson(reader))
        {
            // we need to parse to business object
            var jsonObj = jsonDocument.Deserialize<MyObjectJson>();
            jsonDocument.Dispose();
        }
    }

    [Benchmark]
    public async Task StreamJsonParser()
    {
        var stream = LoadDataToStream();
        var parser = new StreamJsonParser();
        var result = parser.ParseAsync(stream);
        await foreach (var item in result)
        {
            // here we can parse just to business object
            var jsonObj = JsonSerializer.Deserialize<MyObjectJson>(item);
        }
    }

    private MemoryStream LoadDataToStream()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        return stream;
    }
}

public class MyObjectJson
{
    public object[] candidates { get; set; }
}

public class StephenProposition
{
    public async IAsyncEnumerable<JsonDocument> ParseJson(PipeReader reader)
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (!buffer.IsEmpty && TryParseJson(ref buffer, out JsonDocument jsonDocument))
            {
                yield return jsonDocument;
            }

            if (result.IsCompleted)
            {
                break;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }

        reader.Complete();
    }

    bool TryParseJson(ref ReadOnlySequence<byte> buffer, out JsonDocument jsonDocument)
    {
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, default);

        if (JsonDocument.TryParseValue(ref reader, out jsonDocument))
        {
            buffer = buffer.Slice(reader.BytesConsumed);
            return true;
        }

        return false;
    }
}