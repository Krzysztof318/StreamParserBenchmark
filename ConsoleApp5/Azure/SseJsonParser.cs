
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ConsoleApp5.Azure;

public class SseJsonParser
{
    public static async IAsyncEnumerable<T> ParseAsync<T>(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            using SseReader sseReader = new(stream);
            while (!cancellationToken.IsCancellationRequested)
            {
                SseLine? sseEvent = await sseReader.TryReadSingleFieldEventAsync().ConfigureAwait(false);
                if (sseEvent == null)
                {
                    yield break;
                }
                ReadOnlyMemory<char> name = sseEvent.Value.FieldName;
                if (!name.Span.SequenceEqual("data".AsSpan()))
                {
                    throw new InvalidDataException();
                }
                ReadOnlyMemory<char> value = sseEvent.Value.FieldValue;
                if (value.Span.SequenceEqual("[DONE]".AsSpan()))
                {
                    break;
                }
                yield return JsonSerializer.Deserialize<T>(value.Span)!;
            }
        }
        finally
        {
            // Always dispose the stream immediately once enumeration is complete for any reason
            stream.Dispose();
        }
    }
}