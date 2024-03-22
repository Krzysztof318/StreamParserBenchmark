
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ConsoleApp5;

BenchmarkRunner.Run<Bench>();

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net47)]
public class Bench
{
    string data_1 = File.ReadAllText(@"test.txt");
    string data_2;
    
    public Bench()
    {
        data_2 = File.ReadAllText(@"chat_stream_response.json");
        // only to simplify benchmark - removed first [ and last ] and commas between json objects
        int startIndex = data_2.IndexOf('{');
        data_2 = data_2.Substring(startIndex, data_2.LastIndexOf('}')-startIndex+1);
        data_2 = data_2.Replace(",\n", "");
    }

    [Benchmark]
    public async Task ParserVersion3GeminiNoSSE()
    {
        var stream = LoadDataToStream(data_2);
        var class1 = new ParserVersion3();
        using var reader = new StreamReader(stream);
        await foreach (var jsonDocument in class1.ParseJson(reader))
        {
            // we need to parse to business object
            var jsonObj = jsonDocument.Deserialize<MyObjectJson>();
            jsonDocument.Dispose();
        }
    }

    [Benchmark]
    public async Task StephenProposition()
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
    public async Task StreamJsonParser()
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

public class ParserVersion3
{
    public async IAsyncEnumerable<JsonDocument> ParseJson(StreamReader reader)
    {
        JsonReaderState state = default;
        StringBuilder sb = new StringBuilder();
        while (await reader.ReadLineAsync() is { } line)
        {
            // I know this code is not performant, but it's just for the showing of exception during parsing
            sb.Append(line);
            var buff = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(sb.ToString()));
            if (TryParseJson(ref buff, out var jsonDocument, ref state))
            {
                yield return jsonDocument;
                state = default;
                sb.Clear();
            }
        }
    }

    bool TryParseJson(ref ReadOnlySequence<byte> buffer, out JsonDocument jsonDocument, ref JsonReaderState state )
    {
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state);

        if (JsonDocument.TryParseValue(ref reader, out jsonDocument))
        {
            return true;
        }

        state = reader.CurrentState;
        return false;
    }
}