using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public void CloseSync()
    {
        if (_bdConn != null && _bdConn.State == ConnectionState.Open)
        {
            _bdConn.Close(); // Retorna a conexão ao pool
        }
    }

    public async Task CloseAsync()
    {
        if (_bdConn != null && _bdConn.State == ConnectionState.Open)
        {
            await _bdConn.CloseAsync(); // Retorna a conexão ao pool
        }
    }

    public void CreateAdapter(string command)
    {
        if (_bdConn == null)
            throw new System.InvalidOperationException("A conexão MySQL não foi inicializada.");
        _da = new MySqlDataAdapter(command, _bdConn);
    }

    public void Dispose()
    {
        if (_cmd != null)
        {
            _cmd.Dispose();
            _cmd = null;
        }

        // Limpa transação se existir
        if (trans != null)
        {
            trans.Dispose();
            trans = null;
            _initTrans = false;
        }

        // Garante que a conexão seja fechada antes de retornar ao pool
        if (_bdConn != null)
        {
            if (_bdConn.State == ConnectionState.Open)
            {
                _bdConn.Close();
            }

            _bdConn.Dispose();
            _bdConn = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cmd != null)
        {
            await _cmd.DisposeAsync();
            _cmd = null;
        }

        // Garante que a conexão seja fechada antes de retornar ao pool
        if (_bdConn != null)
        {
            if (_bdConn.State == ConnectionState.Open)
            {
                await _bdConn.CloseAsync();
            }

            await _bdConn.DisposeAsync();
            _bdConn = null;
        }

        // Limpa transação se existir
        if (trans != null)
        {
            await trans.DisposeAsync();
            trans = null;
            _initTrans = false;
        }
    }

    public void OpenSync()
    {
        if (_bdConn != null && _bdConn.State != ConnectionState.Open)
        {
            _bdConn.Open();
        }
    }

    public async Task OpenAsync()
    {
        if (_bdConn != null && _bdConn.State != ConnectionState.Open)
        {
            await _bdConn.OpenAsync();
        }
    }

    /// <summary>
    /// Obtém o estado atual da conexão.
    /// </summary>
    public ConnectionState State => _bdConn?.State ?? ConnectionState.Closed;

    /// <summary>
    /// Testa a conexão com o banco de dados.
    /// </summary>
    /// <returns>True se a conexão está funcionando, caso contrário False.</returns>
    public bool TestConnection()
    {
        try
        {
            if (_bdConn == null)
                return false;

            var wasOpen = _bdConn.State == ConnectionState.Open;
            if (!wasOpen)
            {
                OpenSync();
            }

            using (var cmd = new MySqlCommand("SELECT 1", _bdConn))
            {
                cmd.ExecuteScalar();
            }

            if (!wasOpen)
            {
                CloseSync();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Testa a conexão com o banco de dados de forma assíncrona.
    /// </summary>
    /// <returns>Task com True se a conexão está funcionando, caso contrário False.</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (_bdConn == null)
                return false;

            var wasOpen = _bdConn.State == ConnectionState.Open;
            if (!wasOpen)
            {
                await OpenAsync();
            }

            await using (var cmd = new MySqlCommand("SELECT 1", _bdConn))
            {
                await cmd.ExecuteScalarAsync();
            }

            if (!wasOpen)
            {
                await CloseAsync();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
