using Microsoft.EntityFrameworkCore;

namespace Dino.Common.EfCoreHelpers
{
    public static class EfPagingHelpers
    {
        /// <summary>
        /// Runs a query with paging.
        /// </summary>
        /// <typeparam name="T">The type of entities we're querying.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <param name="context">The context to use.
        /// NOTE: You might want to consider a new context, as the paging will perform a Clear() on the entities for each page.</param>
        /// <param name="runOnListElements">OPTIONAL: A method to run on the elements after retrieving them.
        /// NOTE: This method will run, potentially, more than once. One time for each page, with all of the page's elements.</param>
        /// <param name="pageSize">The page size (number of entities to retrieve each time).</param>
        /// <returns>The list of elements that were retrieved.</returns>
        public static async Task<List<T>> RunQueryWithPaging<T>(this IQueryable<T> query, DbContext context,
            Func<List<T>, Task<List<T>>> runOnListElements = null, int pageSize = 200) where T : class
        {
            List<T> entities = new List<T>();

            // Get the number of total elements and start the paging.
            var entitiesCount = await query.CountAsync();
            for (var i = 0; i <= (entitiesCount / pageSize); i++)
            {
                // Clear previous entities form the tracker so the loading won't take too long with each page.
                context.ChangeTracker.Clear();

                // Get current page.
                var pageEntities = await query
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // If there's a method that we need to run to manipulate the data, run it.
                if (runOnListElements != null)
                {
                    await runOnListElements.Invoke(pageEntities);
                }

                // Add to the final list.
                entities.AddRange(pageEntities);
            }

            // Clear previous entities, of the last page.
            context.ChangeTracker.Clear();

            // Clean cache.
            return entities;
        }
    }
}
