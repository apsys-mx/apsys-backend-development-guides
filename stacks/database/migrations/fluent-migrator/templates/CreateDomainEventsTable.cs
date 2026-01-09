using FluentMigrator;

namespace {ProjectName}.migrations;

/// <summary>
/// Migration to create the domain_events table for Event Sourcing Lite pattern.
/// This table serves dual purposes:
/// 1. Outbox Pattern: Events marked with should_publish=true are processed by a background service
/// 2. Audit Trail: All events are persisted for historical and compliance purposes
/// </summary>
/// <remarks>
/// Table Structure:
/// - Identification: id, organization_id (multi-tenant)
/// - Aggregate: aggregate_type, aggregate_id
/// - Event: event_type, event_data (JSONB), occurred_at
/// - Audit: user_id, user_name, ip_address, correlation_id, conversation_id
/// - Outbox: should_publish, published_at, publish_attempts, last_publish_error
/// - Metadata: version
/// </remarks>
[Migration({MigrationNumber})]
public class M{MigrationNumber}CreateDomainEventsTable : Migration
{
    private readonly string _tableName = "domain_events";
    private readonly string _schemaName = "{SchemaName}";

    /// <summary>
    /// Creates the domain_events table with all required columns and indexes.
    /// </summary>
    public override void Up()
    {
        Create.Table(_tableName)
            .InSchema(_schemaName)
            // ─────────────────────────────────────────────────────────────
            // Identification
            // ─────────────────────────────────────────────────────────────
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("organization_id").AsGuid().NotNullable()
            // ─────────────────────────────────────────────────────────────
            // Aggregate Information
            // ─────────────────────────────────────────────────────────────
            .WithColumn("aggregate_type").AsString(200).NotNullable()
            .WithColumn("aggregate_id").AsGuid().NotNullable()
            // ─────────────────────────────────────────────────────────────
            // Event Data
            // ─────────────────────────────────────────────────────────────
            .WithColumn("event_type").AsString(200).NotNullable()
            // Use JSONB for PostgreSQL, NVARCHAR(MAX) for SQL Server
            .WithColumn("event_data").AsCustom("JSONB").NotNullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            // ─────────────────────────────────────────────────────────────
            // Audit Context
            // ─────────────────────────────────────────────────────────────
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("user_name").AsString(200).Nullable()
            .WithColumn("ip_address").AsString(45).Nullable() // IPv6 max length
            .WithColumn("correlation_id").AsString(100).Nullable()
            .WithColumn("conversation_id").AsGuid().Nullable()
            // ─────────────────────────────────────────────────────────────
            // Outbox Pattern Control
            // ─────────────────────────────────────────────────────────────
            .WithColumn("should_publish").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("published_at").AsDateTime().Nullable()
            .WithColumn("publish_attempts").AsInt32().NotNullable().WithDefaultValue(0)
            // Use TEXT for PostgreSQL, NVARCHAR(MAX) for SQL Server
            .WithColumn("last_publish_error").AsCustom("TEXT").Nullable()
            // ─────────────────────────────────────────────────────────────
            // Metadata
            // ─────────────────────────────────────────────────────────────
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // ─────────────────────────────────────────────────────────────────
        // Indexes
        // ─────────────────────────────────────────────────────────────────

        // Index for multi-tenant: query events by organization
        // Use case: "Show all events for this organization"
        Create.Index("ix_domain_events_organization")
            .OnTable(_tableName)
            .InSchema(_schemaName)
            .OnColumn("organization_id").Ascending()
            .OnColumn("occurred_at").Descending();

        // Index for audit: query events by aggregate
        // Use case: "Show history for this entity"
        Create.Index("ix_domain_events_aggregate")
            .OnTable(_tableName)
            .InSchema(_schemaName)
            .OnColumn("aggregate_id").Ascending()
            .OnColumn("occurred_at").Descending();

        // Index for correlation tracking
        // Use case: "Find all events related to this operation"
        Create.Index("ix_domain_events_correlation")
            .OnTable(_tableName)
            .InSchema(_schemaName)
            .OnColumn("correlation_id");

        // Index for outbox pattern: query pending events to publish
        // Use case: Background job fetching events to publish
        // CRITICAL: This index is essential for outbox pattern performance
        Create.Index("ix_domain_events_outbox")
            .OnTable(_tableName)
            .InSchema(_schemaName)
            .OnColumn("should_publish").Ascending()
            .OnColumn("published_at").Ascending()
            .OnColumn("publish_attempts").Ascending()
            .OnColumn("occurred_at").Ascending();
    }

    /// <summary>
    /// Drops the domain_events table and its indexes.
    /// </summary>
    public override void Down()
    {
        Delete.Index("ix_domain_events_outbox")
            .OnTable(_tableName)
            .InSchema(_schemaName);

        Delete.Index("ix_domain_events_correlation")
            .OnTable(_tableName)
            .InSchema(_schemaName);

        Delete.Index("ix_domain_events_aggregate")
            .OnTable(_tableName)
            .InSchema(_schemaName);

        Delete.Index("ix_domain_events_organization")
            .OnTable(_tableName)
            .InSchema(_schemaName);

        Delete.Table(_tableName)
            .InSchema(_schemaName);
    }
}
