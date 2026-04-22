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
        _da = new MySqlDataAdapter(command, this._bdConn);
    }

    public void Dispose()
    {
        if (this._cmd != null)
        {
            this._cmd.Dispose();
            this._cmd = null;
        }

        // Limpa transação se existir
        if (trans != null)
        {
            trans.Dispose();
            trans = null;
            _initTrans = false;
        }

        // Garante que a conexão seja fechada antes de retornar ao pool
        if (this._bdConn != null)
        {
            if (this._bdConn.State == ConnectionState.Open)
            {
                this._bdConn.Close();
            }

            this._bdConn.Dispose();
            this._bdConn = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this._cmd != null)
        {
            await this._cmd.DisposeAsync();
            this._cmd = null;
        }

        // Garante que a conexão seja fechada antes de retornar ao pool
        if (this._bdConn != null)
        {
            if (this._bdConn.State == ConnectionState.Open)
            {
                await this._bdConn.CloseAsync();
            }

            await this._bdConn.DisposeAsync();
            this._bdConn = null;
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

            bool wasOpen = _bdConn.State == ConnectionState.Open;
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

            bool wasOpen = _bdConn.State == ConnectionState.Open;
            if (!wasOpen)
            {
                await OpenAsync();
            }

            using (var cmd = new MySqlCommand("SELECT 1", _bdConn))
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
