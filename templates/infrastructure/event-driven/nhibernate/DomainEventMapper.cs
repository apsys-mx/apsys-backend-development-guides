using {ProjectName}.domain.entities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace {ProjectName}.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="DomainEvent"/> entity.
/// Maps the DomainEvent entity to the domain_events table in the database.
/// </summary>
/// <remarks>
/// This table serves dual purposes:
/// - Outbox Pattern: Events marked with ShouldPublish=true are processed by a background service
/// - Audit Trail: All events are persisted for historical and compliance purposes
/// </remarks>
public class DomainEventMapper : ClassMapping<DomainEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventMapper"/> class.
    /// Configures the mapping between the DomainEvent entity and the domain_events database table.
    /// </summary>
    public DomainEventMapper()
    {
        Table("domain_events");
        Schema("{SchemaName}");

        // Map the primary key with GuidComb generator for efficient inserts
        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.GuidComb);
            map.Type(NHibernateUtil.Guid);
        });

        // Multi-tenant identifier
        Property(x => x.OrganizationId, map =>
        {
            map.Column("organization_id");
            map.NotNullable(true);
            map.Type(NHibernateUtil.Guid);
        });

        // Aggregate information
        Property(x => x.AggregateType, map =>
        {
            map.Column("aggregate_type");
            map.NotNullable(true);
            map.Length(200);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.AggregateId, map =>
        {
            map.Column("aggregate_id");
            map.NotNullable(true);
            map.Type(NHibernateUtil.Guid);
        });

        // Event information
        Property(x => x.EventType, map =>
        {
            map.Column("event_type");
            map.NotNullable(true);
            map.Length(200);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.EventData, map =>
        {
            map.Column("event_data");
            map.NotNullable(true);
            // For PostgreSQL use JsonbType, for SQL Server use NHibernateUtil.StringClob
            map.Type<JsonbType>(); // or map.Type(NHibernateUtil.StringClob);
        });

        Property(x => x.OccurredAt, map =>
        {
            map.Column("occurred_at");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        // Audit context
        Property(x => x.UserId, map =>
        {
            map.Column("user_id");
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.UserName, map =>
        {
            map.Column("user_name");
            map.Length(200);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.IpAddress, map =>
        {
            map.Column("ip_address");
            map.Length(45);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.CorrelationId, map =>
        {
            map.Column("correlation_id");
            map.Length(100);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.ConversationId, map =>
        {
            map.Column("conversation_id");
            map.Type(NHibernateUtil.Guid);
        });

        // Outbox pattern properties
        Property(x => x.ShouldPublish, map =>
        {
            map.Column("should_publish");
            map.NotNullable(true);
            map.Type(NHibernateUtil.Boolean);
        });

        Property(x => x.PublishedAt, map =>
        {
            map.Column("published_at");
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.PublishAttempts, map =>
        {
            map.Column("publish_attempts");
            map.NotNullable(true);
            map.Type(NHibernateUtil.Int32);
        });

        Property(x => x.LastPublishError, map =>
        {
            map.Column("last_publish_error");
            map.Type(NHibernateUtil.StringClob);
        });

        // Version
        Property(x => x.Version, map =>
        {
            map.Column("version");
            map.NotNullable(true);
            map.Type(NHibernateUtil.Int32);
        });
    }
}
