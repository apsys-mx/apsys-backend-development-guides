using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;

namespace {ProjectName}.application.tests;

/// <summary>
/// Base class for application layer tests that provides common test setup functionality.
/// Configures AutoFixture with AutoMoq for automatic mock generation.
/// </summary>
public abstract class ApplicationTestBase
{
    protected internal IFixture fixture;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        fixture = new Fixture();

        // Configure AutoMoq for automatic mock creation
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Handle circular references
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
