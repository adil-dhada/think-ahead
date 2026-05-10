using Playbook.Application.Common.Abstractions;

namespace Playbook.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
