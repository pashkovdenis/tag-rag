using Microsoft.EntityFrameworkCore;
using TagRag.Example.Entity;

namespace TagRag.Example.Db
{
    
    public class ConversationContext : DbContext
    {
        public DbSet<Message> Messages => Set<Message>();

        public ConversationContext(DbContextOptions<ConversationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Message>().ToTable("Messages");
        }
    }

}
