using ApiLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace ApiLib.Data
{
    public abstract class BaseDbContext : DbContext
    {
        public BaseDbContext(DbContextOptions options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyChanges();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyChanges();
            return base.SaveChanges();
        }


        private void ApplyChanges()
        {
            ApplyChangesOnState(EntityState.Added, (item, model) => {
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.Deleted = false;
            });
            ApplyChangesOnState(EntityState.Modified, (item, model) => {
                model.UpdatedAt = DateTime.Now;
            });
            ApplyChangesOnState(EntityState.Deleted, (item, model) => {
                model.Deleted = true;
                item.State = EntityState.Modified;
            });
        }


        private void ApplyChangesOnState(EntityState state, Action<EntityEntry, BaseModel> changes)
        {
            var deleteEntities = ChangeTracker.Entries().Where(x => x.State == state);
            foreach (var item in deleteEntities)
                if (item.Entity is BaseModel model)
                    changes(item, model);
        }

    }
}
