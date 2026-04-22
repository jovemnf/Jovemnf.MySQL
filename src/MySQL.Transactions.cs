using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    public void BeginSync()
    {
        if (_bdConn == null)
        {
            throw new InvalidOperationException("Conexão não foi inicializada.");
        }

        _initTrans = true;
        trans = _bdConn.BeginTransaction();
    }

    public async Task BeginAsync()
    {
        await BeginAsync(CancellationToken.None);
    }

    public void RollbackSync()
    {
        if (trans == null) return;
        trans.Rollback();
        trans.Dispose();
        trans = null;
        _initTrans = false;
    }

    public async Task RollbackAsync()
    {
        if (trans != null)
        {
            await trans.RollbackAsync();
            await trans.DisposeAsync();
            trans = null;
            _initTrans = false;
        }
    }

    public void CommitSync()
    {
        if (trans == null) return;
        trans.Commit();
        trans.Dispose();
        trans = null;
        _initTrans = false;
    }

    public async Task CommitAsync()
    {
        if (trans != null)
        {
            await trans.CommitAsync();
            await trans.DisposeAsync();
            trans = null;
            _initTrans = false;
        }
    }

    /// <summary>
    /// Indica se há uma transação ativa.
    /// </summary>
    public bool HasActiveTransaction => _initTrans && trans != null;
}
