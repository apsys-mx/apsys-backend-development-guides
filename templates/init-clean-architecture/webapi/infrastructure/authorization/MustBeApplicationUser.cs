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
        /// <remarks>
        /// This is an example of custom authorization.
        /// Customize this handler based on your domain requirements.
        /// </remarks>
        public class Handler(IUnitOfWork unitOfWork) : AuthorizationHandler<Requirement>
        {
            private readonly IUnitOfWork _unitOfWork = unitOfWork;

            protected override async Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                Requirement requirement)
            {
                // TODO: Customize this logic based on your domain
                // Example: Get user identifier from claims and verify against database

                // Get user name from claims
                var userName = context.User.FindFirst("username")?.Value;
                if (string.IsNullOrEmpty(userName))
                    return;

                // TODO: Implement actual user verification logic
                // Example:
                // var user = await _unitOfWork.Users.GetByEmailAsync(userName);
                // if (user is not null)
                //     context.Succeed(requirement);

                // Temporary: Always succeed for development
                context.Succeed(requirement);

                return;
            }
        }
    }
}
