using {ProjectName}.infrastructure.nhibernate.mappers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Session factory using C# 12 primary constructor syntax.
/// Configure the driver and dialect according to your database.
/// </summary>
public class NHSessionFactory(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// Create the NHibernate Session Factory
    /// </summary>
    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();
        // Register all mappers from the assembly
        // Use any mapper as reference to get the assembly
        mapper.AddMappings(typeof(ExampleMapper).Assembly.ExportedTypes);
        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            // ============================================================
            // CONFIGURE DRIVER AND DIALECT FOR YOUR DATABASE
            // See: stacks/database/{postgresql|sqlserver}/guides/setup.md
            // ============================================================

            // TODO: Uncomment ONE of the following based on your database:

            // For PostgreSQL:
            // c.Driver<NpgsqlDriver>();
            // c.Dialect<PostgreSQL83Dialect>();

            // For SQL Server:
            // c.Driver<MicrosoftDataSqlClientDriver>();
            // c.Dialect<MsSql2012Dialect>();

            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
        });

        cfg.AddMapping(domainMapping);
        return cfg.BuildSessionFactory();
    }
}
