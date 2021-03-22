namespace Raft.Demo
{
    internal abstract class Role
    {
        public abstract string Id { get; }

        public abstract RoleType Type { get; }

        public ElectionAlarm Alarm { get; } = new ElectionAlarm();
    }
}
