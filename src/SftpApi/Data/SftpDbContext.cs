using Microsoft.EntityFrameworkCore;

namespace SftpApi.Data;

public class SftpDbContext(DbContextOptions<SftpDbContext> options) : DbContext(options)
{
    public DbSet<TransferredFile> TransferredFiles => Set<TransferredFile>();
    public DbSet<ReceivedFile> ReceivedFiles => Set<ReceivedFile>();
}
