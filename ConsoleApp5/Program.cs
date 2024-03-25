
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ConsoleApp5;
using ConsoleApp5.Azure;

BenchmarkRunner.Run<Bench>();

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net47)]
public class Bench
{
    string data_1 = File.ReadAllText(@"test.txt");
    string data_2;
    string data_sse = File.ReadAllText(@"data_sse.txt");
    
    public Bench()
    {
        data_2 = File.ReadAllText(@"chat_stream_response.json");
        // only to simplify benchmark - removed first [ and last ] and commas between json objects
        int startIndex = data_2.IndexOf('{');
        data_2 = data_2.Substring(startIndex, data_2.LastIndexOf('}')-startIndex+1);
        data_2 = data_2.Replace(",\n", "");
    }
    

    [Benchmark]
    public async Task StephenPropositionNoSse()
    {
        var stream = LoadDataToStream(data_1);
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
    public async Task StreamJsonParserNoSse()
    {
        var stream = LoadDataToStream(data_1);
        var parser = new StreamJsonParser();
        var result = parser.ParseAsync(stream);
        await foreach (var item in result)
        {
            // here we can parse just to business object
            var jsonObj = JsonSerializer.Deserialize<MyObjectJson>(item);
        }
    }
    
    [Benchmark]
    public async Task StreamJsonParserSseData()
    {
        var stream = LoadDataToStream(data_sse);
        var parser = new StreamJsonParser();
        var result = parser.ParseAsync(stream);
        await foreach (var item in result)
        {
            // here we can parse just to business object
            var jsonObj = JsonSerializer.Deserialize<MyObjectJson>(item);
        }
    }
    
    [Benchmark]
    public async Task AzureJsonParserSseData()
    {
        var stream = LoadDataToStream(data_sse);
        var result = SseJsonParser.ParseAsync<MyObjectJson>(stream);
        await foreach (var item in result)
        {
            // any logic here
            var jsonObj = item;
        }
    }

    private MemoryStream LoadDataToStream(string data)
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

