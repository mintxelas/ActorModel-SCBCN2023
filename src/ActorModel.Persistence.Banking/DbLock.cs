using Medallion.Threading.Postgres;

namespace ActorModel.Persistence.Banking;

public class DbLock
{
    private readonly PostgresDistributedLock @lock;

    public DbLock(string lockName, string connectionString)
    {
        @lock = new PostgresDistributedLock(new PostgresAdvisoryLockKey(lockName, allowHashing: true), connectionString);
    }

    public void Lock(Action action)
    {
        using (@lock.Acquire())
        {
            action();
        }
    }
}