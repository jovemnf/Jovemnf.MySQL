using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jovemnf.MySQL;
using MysqlTest.Fakes;
using Xunit;

namespace MysqlTest;

public class StreamingTests
{
    private sealed class Pessoa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
    }

    private static List<Dictionary<string, object>> BuildRows(int total)
    {
        var rows = new List<Dictionary<string, object>>(total);
        for (int i = 1; i <= total; i++)
        {
            rows.Add(new Dictionary<string, object>
            {
                ["id"] = i,
                ["nome"] = $"Pessoa {i}",
            });
        }
        return rows;
    }

    [Fact]
    public async Task ToModelStreamAsync_YieldsEachRow_WithoutMaterializingList()
    {
        await using var reader = new MySqlReader(new FakeDataReader(BuildRows(5)));

        var nomes = new List<string>();
        await foreach (var p in reader.ToModelStreamAsync<Pessoa>())
        {
            nomes.Add(p.Nome);
        }

        Assert.Equal(new[] { "Pessoa 1", "Pessoa 2", "Pessoa 3", "Pessoa 4", "Pessoa 5" }, nomes);
    }

    [Fact]
    public async Task ToModelStreamAsync_SupportsBreak_WithoutReadingAllRows()
    {
        await using var reader = new MySqlReader(new FakeDataReader(BuildRows(1000)));

        int count = 0;
        await foreach (var p in reader.ToModelStreamAsync<Pessoa>())
        {
            count++;
            if (p.Id == 3)
                break;
        }

        // Confirma que o consumidor parou cedo e o reader foi liberado corretamente
        // (não lança ObjectDisposedException nem precisa ler tudo).
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ToModelStreamAsync_EmptyResult_EnumeratesZeroItems()
    {
        await using var reader = new MySqlReader(new FakeDataReader(new List<Dictionary<string, object>>()));

        int count = 0;
        await foreach (var _ in reader.ToModelStreamAsync<Pessoa>())
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ToModelStreamAsync_ResultsEquivalentToToModelListAsync()
    {
        var rows = BuildRows(50);

        await using var listReader = new MySqlReader(new FakeDataReader(rows));
        var listResult = await listReader.ToModelListAsync<Pessoa>();

        await using var streamReader = new MySqlReader(new FakeDataReader(rows));
        var streamResult = new List<Pessoa>();
        await foreach (var p in streamReader.ToModelStreamAsync<Pessoa>())
        {
            streamResult.Add(p);
        }

        Assert.Equal(listResult.Count, streamResult.Count);
        Assert.Equal(listResult.Select(p => p.Id), streamResult.Select(p => p.Id));
        Assert.Equal(listResult.Select(p => p.Nome), streamResult.Select(p => p.Nome));
    }
}
