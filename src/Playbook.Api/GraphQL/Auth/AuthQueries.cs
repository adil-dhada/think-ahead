using HotChocolate.Authorization;
using HotChocolate.Types;
using Playbook.Application.Auth;

namespace Playbook.Api.GraphQL.Auth;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class AuthQueries
{
    [Authorize]
    public async Task<UserType> MeAsync(
        [Service] GetMeHandler handler,
        CancellationToken ct)
    {
        var user = await handler.Handle(ct);
        return AuthMapper.ToType(user);
    }
}
