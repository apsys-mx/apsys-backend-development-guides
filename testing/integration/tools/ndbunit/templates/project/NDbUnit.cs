using System.Data;
using System.Data.Common;

namespace {ProjectName}.ndbunit;

public abstract class NDbUnit : INDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    protected NDbUnit(DataSet dataSet, string connectionString)
    {
        this.ConnectionString = connectionString;
        this.DataSet = dataSet;
    }

    /// <summary>
    /// Gets the dataSet containing the tables where the operations are execute
    /// </summary>
    public DataSet DataSet { get; private set; }

    /// <summary>
    /// Gets the connection string to the database where the operations are execute
    /// </summary>
    public string ConnectionString { get; private set; }

    /// <summary>
    /// Get a dataset with the tables and data from the database
    /// </summary>
    /// <returns></returns>
    public DataSet GetDataSetFromDb()
    {
        using DbConnection cnn = this.CreateConnection();
        DataSet dsetResult = this.DataSet.Clone();
        dsetResult.EnforceConstraints = false;
        DbProviderFactory? dbFactory = DbProviderFactories.GetFactory(cnn) ?? throw new ArgumentException("Cannot create [DbProviderFactory] from configuration");
        foreach (DataTable table in this.DataSet.Tables)
        {
            DbCommand selectCommand = cnn.CreateCommand();
            selectCommand.CommandText = $"SELECT * FROM {table.TableName}";

            DbDataAdapter? adapter = dbFactory.CreateDataAdapter() ?? throw new ArgumentException("Cannot create [DbDataAdapter] from configuration");
            adapter.SelectCommand = selectCommand;
            adapter.Fill(dsetResult, table.TableName);
        }
        dsetResult.EnforceConstraints = true;
        return dsetResult;

    }

    /// <summary>
    /// Clear all the database data
    /// </summary>
    public void ClearDatabase()
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();

        using IDbTransaction transaction = cnn.BeginTransaction();
        try
        {
            foreach (DataTable dataTable in this.DataSet.Tables)
                this.DisableTableConstraints(transaction, dataTable);

            foreach (DataTable dataTable in this.DataSet.Tables)
            {
                var cmd = cnn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"DELETE FROM {dataTable.TableName}";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            foreach (DataTable dataTable in this.DataSet.Tables)
                this.EnabledTableConstraints(transaction, dataTable);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        cnn.Close();
    }

    /// <summary>
    /// Seed the database with the information contained in the dataset
    /// </summary>
    /// <param name="dataSet"></param>
    public void SeedDatabase(DataSet dataSet)
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();

        this.DataSet = dataSet;

        using (IDbTransaction transaction = cnn.BeginTransaction())
        {
            try
            {
                foreach (DataTable dataTable in this.DataSet.Tables)
                    this.DisableTableConstraints(transaction, dataTable);

                foreach (DataTable dataTable in this.DataSet.Tables)
                {
                    // Create select comand
                    var selectCommand = cnn.CreateCommand();
                    selectCommand.CommandText = $"SELECT * FROM {dataTable.TableName}";
                    selectCommand.Transaction = transaction;
                    // Crear un adaptador de datos
                    var adapter = this.CreateDataAdapter();
                    adapter.SelectCommand = selectCommand as DbCommand;
                    var commandBuilder = this.CreateCommandBuilder(adapter);
                    adapter.InsertCommand = commandBuilder.GetInsertCommand();
                    adapter.InsertCommand.Transaction = transaction as DbTransaction;
                    // Actualiza la tabla
                    adapter.Update(dataTable);
                }

                foreach (DataTable dataTable in this.DataSet.Tables)
                    this.EnabledTableConstraints(transaction, dataTable);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        cnn.Close();
    }

    /// <summary>
    /// Creates a DbConnection
    /// </summary>
    /// <returns></returns>
    public abstract DbConnection CreateConnection();

    /// <summary>
    /// Create a DbDataAdapter
    /// </summary>
    /// <returns></returns>
    public abstract DbDataAdapter CreateDataAdapter();

    /// <summary>
    /// Create a command builder
    /// </summary>
    /// <param name="dataAdapter"></param>
    /// <returns></returns>
    public abstract DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter);

    /// <summary>
    /// Enable datatable's constraints
    /// </summary>
    /// <param name="dbTransaction"></param>
    protected abstract void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);

    /// <summary>
    /// Disable datatable's constraints
    /// </summary>
    /// <param name="dbTransaction"></param>
    protected abstract void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);
}
