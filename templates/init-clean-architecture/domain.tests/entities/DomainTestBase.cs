using AutoFixture;
using NUnit.Framework;

namespace {ProjectName}.domain.tests.entities;

/// <summary>
/// Base class for domain entity tests that provides common test setup functionality
/// </summary>
public abstract class DomainTestBase
{
    protected internal IFixture fixture;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        fixture = new Fixture();
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
