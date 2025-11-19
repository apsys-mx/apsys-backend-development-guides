using MiProyecto.infrastructure.nhibernate.mappers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Session factory using C# 12 primary constructor syntax
/// </summary>
public class NHSessionFactory(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// Create the NHibernate Session Factory
    /// </summary>
    /// <returns></returns>
    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();
        // Registrar todos los mappers del ensamblado
        // Usar cualquier mapper como referencia para obtener el assembly
        mapper.AddMappings(typeof(RoleMapper).Assembly.ExportedTypes);
        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            // Para PostgreSQL:
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            // Para SQL Server:
            // c.Driver<MicrosoftDataSqlClientDriver>();
            // c.Dialect<MsSql2012Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            //c.SchemaAction = SchemaAutoAction.Validate;
        });
        cfg.AddMapping(domainMapping);
        return cfg.BuildSessionFactory();
    }
}
