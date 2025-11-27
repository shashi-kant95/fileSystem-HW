using System;
using Microsoft.EntityFrameworkCore;

namespace FileSystem_Honeywell.Model
{
    public class FSDBContext : DbContext
    {
        public FSDBContext(DbContextOptions<FSDBContext> options) : base(options) { }

        public DbSet<FileRecord> FileRecords => Set<FileRecord>();
        public DbSet<AuthUser> Users { get; set; } = default!;
    }

}

