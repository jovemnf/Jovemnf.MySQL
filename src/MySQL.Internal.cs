using System;
using MySqlConnector;

namespace Jovemnf.MySQL;

public partial class MySQL
{
    private void EnsureCommandInitialized()
    {
        if (this._cmd == null)
            throw new InvalidOperationException("Comando não foi inicializado. Chame OpenCommand primeiro.");
    }

    private MySqlCommand AttachCommand(MySqlCommand command, bool trackAsCurrent = true)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.Connection = _bdConn;
        if (trans != null)
            command.Transaction = trans;

        if (trackAsCurrent)
            _cmd = command;

        return command;
    }
}
