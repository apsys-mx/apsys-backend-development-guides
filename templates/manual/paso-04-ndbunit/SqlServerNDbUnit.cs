using System.Data.Common;
using System.Data;
using Microsoft.Data.SqlClient;

namespace MiProyecto.ndbunit;

public class SqlServerNDbUnit : NDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    public SqlServerNDbUnit(DataSet dataSet, string connectionString)
        : base(dataSet, connectionString)
    {
    }

    public override DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter)
    {
        return new SqlCommandBuilder((SqlDataAdapter)dataAdapter);
    }

    public override DbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public override DbDataAdapter CreateDataAdapter()
    {
        return new SqlDataAdapter();
    }

    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a disable constraint for SQL Server
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using SqlCommand command = (SqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (SqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} NOCHECK CONSTRAINT ALL";
        command.ExecuteNonQuery();
    }

    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a enable constraint for SQL Server
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using SqlCommand command = (SqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (SqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} WITH CHECK CHECK CONSTRAINT ALL";
        command.ExecuteNonQuery();
    }
}
