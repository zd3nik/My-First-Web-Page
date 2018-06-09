using Microsoft.EntityFrameworkCore;

namespace PeopleSearch.Models
{
    public class PeopleContext : DbContext
    {
        public PeopleContext()
            : base()
        {
        }

        public PeopleContext(DbContextOptions<PeopleContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PersonEntry> PersonEntries { get; set; }
        public virtual DbSet<ImageEntry> ImageEntries { get; set; }
    }
}
