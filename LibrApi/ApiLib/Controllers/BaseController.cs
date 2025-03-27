using System.Net.Mime;
using System.Reflection;
using ApiLib.Data;
using ApiLib.Extensions;
using ApiLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using static System.Net.Mime.MediaTypeNames;


namespace ApiLib.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public abstract class BaseController<TContext, TModel> : ControllerBase where TContext : BaseDbContext where TModel : BaseModel
    {
        protected readonly TContext _context;
        protected Dictionary<string, bool> Features { get; } = new()
        {
            {Filtering, true },
            {Sort, true },
            {Pagination, true },
            {PartialResponse, true }
        };

        protected int MaxPageSize = 10; // 10 par défaut

        protected static string Filtering => "Filtering";
        protected static string Sort => "Sort";
        protected static string Pagination => "Pagination";
        protected static string PartialResponse => "PartialResponse";

        protected bool IsFeatureEnabled(string featureName)
           => Features.TryGetValue(featureName, out var isEnabled) && isEnabled;

        public BaseController(TContext context)
        {
            _context = context;
        }

        [HttpGet(Name = "GetAll")]
        [SwaggerOperation(Summary = "Retrieve every book registered")]
        [SwaggerResponse(StatusCodes.Status200OK, "The list of books")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public virtual async Task<IActionResult> GetAll([FromQuery] Dictionary<string, string> queryParams)
        {
            try
            {
                // query de base
                var query = _context.Set<TModel>().Where(x => x.Deleted == false);

                // Filter
                if (IsFeatureEnabled(Filtering))
                    query = QueryFilterBuilder(query, queryParams);

                // tri ascendant et descendant
                if (IsFeatureEnabled(Sort))
                    query = QuerySortBuilder(query, queryParams);

                // pagination
                if (IsFeatureEnabled(Pagination))
                    query = QueryPaginationBuilder(query, queryParams);

                // Build la query finale
                var queryResult = await query.ToListAsync();

                if (IsFeatureEnabled(PartialResponse))
                    return Ok(queryResult.Select(item => ApplyPartialResponse(item, queryParams)));

                return Ok(queryResult);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Search entities with full-text capabilities",
            Description = "Performs a search operation with advanced filtering and pagination",
            OperationId = "SearchEntities")]
        [ProducesResponseType(typeof(IEnumerable<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<IActionResult> Search([FromQuery] Dictionary<string, string> queryParams)
        {
            try
            {
                // query de base
                var query = _context.Set<TModel>().Where(x => x.Deleted == false);

                // Search
                query = QuerySearchBuilder(query, queryParams);

                // tri ascendant et descendant
                if (IsFeatureEnabled(Sort))
                    query = QuerySortBuilder(query, queryParams);

                // pagination
                if (IsFeatureEnabled(Pagination))
                    query = QueryPaginationBuilder(query, queryParams);

                // Build la query finale
                var queryResult = await query.ToListAsync();

                if (IsFeatureEnabled(PartialResponse))
                    return Ok(queryResult.Select(item => ApplyPartialResponse(item, queryParams)));

                return Ok(queryResult);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/[Models]/5
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get entity by ID",
            Description = "Retrieves a single entity with optional field selection",
            OperationId = "GetEntityById")]
        [ProducesResponseType(typeof(Dictionary<string, object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public virtual async Task<ActionResult<TModel>> GetById(int id, [FromQuery] Dictionary<string, string> queryParams)
        {
            var model = await _context.Set<TModel>().FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }
            if (model.Deleted == true)
            {
                return NotFound();
            }
            if (IsFeatureEnabled(PartialResponse))
                return Ok(ApplyPartialResponse(model, queryParams));
            return Ok(model);
        }
         
        // PUT: api/[Models]/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Update an existing entity",
            Description = "Full update of the specified entity",
            OperationId = "UpdateEntity")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public virtual async Task<IActionResult> Put(int id, TModel model)
        {
            if (id != model.ID)
            {
                return BadRequest();
            }

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/[Models]
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new entity",
            Description = "Adds a new entity to the system",
            OperationId = "CreateEntity")]
        [SwaggerResponse(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<ActionResult<TModel>> Post(TModel model)
        {
            _context.Set<TModel>().Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetById", new { id = model.ID }, model);
        }

        // DELETE: api/[Models]/5
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Delete an entity permanently",
            Description = "Physical deletion from the database",
            OperationId = "DeleteEntity")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var model = await _context.Set<TModel>().FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            if (model.Deleted == true)
            {
                return NotFound();
            }

            _context.Set<TModel>().Remove(model);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Put: api/[Models]/5/restore
        [HttpPatch("{id}/restore")]
        [SwaggerOperation(
            Summary = "Restore a soft-deleted entity",
            Description = "Reactivates an entity that was previously soft-deleted",
            OperationId = "RestoreEntity")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<ActionResult<TModel>> Restore(int id)
        {
            var model = await _context.Set<TModel>().FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }
            if (model.Deleted == false)
            {
                return NotFound();
            }

            model.Deleted = false;
            _context.Entry(model).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(model);
        }

        /*Private*/

        private bool ModelExists(int id)
        {
            return _context.Set<TModel>().Any(e => e.ID == id);
        }

        // Filters
        private IQueryable<TModel> QueryFilterBuilder(IQueryable<TModel> query, [FromQuery] Dictionary<string, string> queryParams)
        {
            // supprimer asc, desc, range et fields des filtres
            var filters = queryParams
                .Where(kv => kv.Key != "asc" && kv.Key != "desc" && kv.Key != "range" && kv.Key != "fields")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // filtres dynamiques
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    query = query.ApplyFilter(filter.Key, filter.Value);
                }
            }
            return (query);
        }

        // Search
        private IQueryable<TModel> QuerySearchBuilder(IQueryable<TModel> query, [FromQuery] Dictionary<string, string> queryParams)
        {
            // supprimer asc, desc, range et fields des filtres
            var filters = queryParams
                .Where(kv => kv.Key != "asc" && kv.Key != "desc" && kv.Key != "range" && kv.Key != "fields")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // filtres dynamiques
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    query = query.ApplySearch(filter.Key, filter.Value);
                }
            }
            return (query);
        }

        // Sort
        private IQueryable<TModel> QuerySortBuilder(IQueryable<TModel> query, [FromQuery] Dictionary<string, string> queryParams)
        {
            queryParams.TryGetValue("asc", out var asc);
            queryParams.TryGetValue("desc", out var desc);

            if (asc != null || desc != null)
                query = query.ApplySortOnFields(asc, desc);
            return (query);
        }

        // Partial Response
        private Dictionary<string, object?> ApplyPartialResponse(TModel item, [FromQuery] Dictionary<string, string> queryParams)
        {
            queryParams.TryGetValue("fields", out var fields);

            if (item == null)
            {
                return new Dictionary<string, object?>();
            }

            if (string.IsNullOrEmpty(fields))
            {
                return item.GetType()
                    .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.Name != "LazyLoader")
                    .ToDictionary(property => property.Name, property => property.GetValue(item));
            }

            var selectedFields = fields.Split(',').Select(f => f.Trim()).ToList();
            var dictionary = new Dictionary<string, object?>();

            foreach (var field in selectedFields)
            {
                var property = typeof(TModel).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.Name != "LazyLoader") // Exclude LazyLoader property
                {
                    var value = property.GetValue(item);
                    dictionary.Add(field, value);
                }
            }

            return dictionary;
        }

        // Pagination

        private IQueryable<TModel> QueryPaginationBuilder(IQueryable<TModel> query, [FromQuery] Dictionary<string, string> queryParams)
        {
            queryParams.TryGetValue("range", out var range);
            var totalItems = query.Count();

            var (skip, take) = ParseRangeParameter(range, totalItems);
            query = query.Skip(skip).Take(take);

            AddPaginationHeaders(skip, take, totalItems);
            return (query);
        }

        

        private (int skip, int take) ParseRangeParameter(string? range, int totalItems)
        {
            if (string.IsNullOrWhiteSpace(range))
            {
                // Si range est null ou vide, appliquer la pagination par défaut
                return (0, MaxPageSize);
            }

            var parts = range.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int end))
            {
                throw new ArgumentException("Invalid range format. Expected format: start-end (e.g., 0-25).");
            }

            if (start > end)
            {
                throw new ArgumentException("Invalid range: start index cannot be greater than end index.");
            }

            int rangeSize = end - start + 1;
            if (rangeSize > MaxPageSize)
            {
                throw new ArgumentException($"Invalid range: maximum allowed range size is {MaxPageSize}.");
            }

            int take = Math.Min(rangeSize, totalItems - start);
            if (take <= 0)
            {
                throw new ArgumentException("Invalid range: range is out of bounds.");
            }

            return (start, take);
        }


        private void AddPaginationHeaders(int skip, int take, int totalItems)
        {
            Response.Headers.AcceptRanges = $"{typeof(TModel).Name} {MaxPageSize}"; // Indique la pagination possible par blocs de 50

            // Définir le Content-Range
            int end = Math.Min(skip + take - 1, totalItems - 1);
            Response.Headers.ContentRange = $"{skip}-{end}/{totalItems}";

            // Construire les liens de navigation
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            List<string> links = [];

            if (skip > 0)
            {
                int prevStart = Math.Max(0, skip - take);
                int prevEnd = prevStart + take - 1;
                links.Add($"<{baseUrl}?range={prevStart}-{prevEnd}>; rel=\"prev\"");
            }

            if (skip + take < totalItems)
            {
                int nextStart = skip + take;
                int nextEnd = Math.Min(nextStart + take - 1, totalItems - 1);
                links.Add($"<{baseUrl}?range={nextStart}-{nextEnd}>; rel=\"next\"");
            }

            links.Add($"<{baseUrl}?range=0-{take - 1}>; rel=\"first\"");
            links.Add($"<{baseUrl}?range={Math.Max(0, totalItems - take)}-{totalItems - 1}>; rel=\"last\"");

            Response.Headers.Link = string.Join(", ", links);
        }

    }
}