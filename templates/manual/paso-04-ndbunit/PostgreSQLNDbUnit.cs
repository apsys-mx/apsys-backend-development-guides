using System.Data.Common;
using System.Data;
using Npgsql;

namespace MiProyecto.ndbunit;

public class PostgreSQLNDbUnit : NDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    public PostgreSQLNDbUnit(DataSet dataSet, string connectionString)
        : base(dataSet, connectionString)
    {
    }

    public override DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter)
    {
        return new NpgsqlCommandBuilder((NpgsqlDataAdapter)dataAdapter);
    }

    public override DbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    public override DbDataAdapter CreateDataAdapter()
    {
        return new NpgsqlDataAdapter();
    }

    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a disable constraint for PostgreSQL
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} DISABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }

    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a enable constraint for PostgreSQL
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} ENABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }
}
