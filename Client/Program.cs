using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using SQLite;

var summary = BenchmarkRunner.Run<Downloader>();

// var downloader = new Downloader();
// await downloader.GlobalSetup();
// await downloader.ProcessInChunksAsyncEnumerable();
// await downloader.GlobalCleanupAsync();


[MemoryDiagnoser]
public class Downloader
{
    private static HttpClient? _httpClient;
    private SQLiteAsyncConnection? _dbConnection;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _httpClient = new();

        var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyData.db");
        _dbConnection = new SQLiteAsyncConnection(databasePath);

        await _dbConnection.CreateTableAsync<Customer>();
    }

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            File.Delete(_dbConnection.DatabasePath);
        }
    }

    [Benchmark]
    public async Task GetWholeStream()
    {
        if (_httpClient == null || _dbConnection == null)
        {
            throw new InvalidOperationException("GlobalSetup first!");
        }
        Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5026/Customer");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        var allData =
                 serializer.Deserialize<List<Page>>(jsonTextReader) ?? new();
        var customers = allData.Select(x => x.Customer);
        await _dbConnection.InsertAllAsync(customers, "OR REPLACE");
    }

    [Benchmark]
    public async Task GetWholeStreamUtfJson8()
    {
        if (_httpClient == null || _dbConnection == null)
        {
            throw new InvalidOperationException("GlobalSetup first!");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5026/Customer");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();

        var allData =
                 Utf8Json.JsonSerializer.Deserialize<List<Page>>(stream) ?? new();
        var customers = allData.Select(x => x.Customer);
        await _dbConnection.InsertAllAsync(customers, "OR REPLACE");
    }

    [Benchmark]
    public async Task ProcessInChunks()
    {
        if (_httpClient == null || _dbConnection == null)
        {
            throw new InvalidOperationException("GlobalSetup first!");
        }
        List<Page> pages = new List<Page>();
        Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5026/Customer");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();
        await _dbConnection.RunInTransactionAsync(async (conn) =>
        {
            using (StreamReader sr = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                while (reader.Read())
                {
                    // deserialize only when there's "{" character in the stream
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var page = serializer.Deserialize<Page>(reader);
                        if (page != null)
                        {
                            await _dbConnection.InsertOrReplaceAsync(page.Customer);
                        }
                    }
                }
            }
        });
    }

    //[Benchmark]
    public async Task ProcessInChunksAsyncEnumerable()
    {
        if (_httpClient == null || _dbConnection == null)
        {
            throw new InvalidOperationException("GlobalSetup first!");
        }
        List<Page> pages = new List<Page>();
        Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5026/Customer");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();
        await _dbConnection.RunInTransactionAsync(async (conn) =>
        {
            IAsyncEnumerable<Page?> pages = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<Page?>(stream);
            await foreach (Page? p in pages)
            {
                if (p != null && p.Customer != null)
                {
                    await _dbConnection.InsertOrReplaceAsync(p.Customer);
                }
            }
        });
    }
}
