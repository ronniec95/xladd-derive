using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AARC.RDS
{
    // http://stackoverflow.com/questions/5557829/update-row-if-it-exists-else-insert-logic-with-entity-framework
    // Entity Framework - marking as modified:
    // When you change the EntityState of an entity object entry to Modified, 
    // all the properties of the object are marked as modified, regardless of the current or original values
    // ... OR ... 
    // Add if not exists?
    // http://stackoverflow.com/questions/31162576/entity-framework-add-if-not-exist-without-update
    // note: where T class, new() ==> constraint on the generic parameter T. It must be a class (reference type) and must have a public parameterless default constructor

    public static class DbSetExtensions
    {
        // Someclass s = new Someclass { Name = "Bob" };
        // example: context.Set<Someclass>().AddIfNotExists(s, x => x.Name == s.Name);
        public static void AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            bool exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            if (!exists)
                dbSet.Add(entity);
            //return !exists ? dbSet.Add(entity) : null;
        }


    }
}
