using {ProjectName}.domain.interfaces.repositories;
using Microsoft.AspNetCore.Authorization;

namespace {ProjectName}.webapi.infrastructure.authorization
{
    public static class MustBeApplicationUser
    {
        /// <summary>
        /// Authorization requirement that checks if a user is an application user.
        /// </summary>
        public class Requirement : IAuthorizationRequirement
        {
            public Requirement() { }
        }

        /// <summary>
        /// Authorization handler that checks if the user is an application user.
        /// </summary>
        public class Handler(IUnitOfWork unitOfWork) : AuthorizationHandler<Requirement>
        {
            private readonly IUnitOfWork _unitOfWork = unitOfWork;

            protected override async Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                Requirement requirement)
            {
                // Get user name from claims
                // NOTE: Update the claim type based on your identity provider
                // Common claim types: "username", "email", "sub", etc.
                var userName = context.User.FindFirst("username")?.Value;
                if (string.IsNullOrEmpty(userName))
                    return;

                // TODO: Implement actual user verification logic
                // Example implementation:
                // var user = await _unitOfWork.Users.GetByEmailAsync(userName);
                // if (user is not null)
                //     context.Succeed(requirement);

                return;
            }
        }
    }
}
