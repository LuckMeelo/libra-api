using ApiLib.Data;
using ApiLib.Extensions;
using ApiLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using static System.Net.Mime.MediaTypeNames;



// pagination: handle not enough to return
// editable Batch size
// base link generation


//

namespace ApiLib.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public abstract class BaseController<TContext, TModel> : ControllerBase where TContext : BaseDbContext where TModel : BaseModel
    {
        protected readonly TContext _context;
        protected int MaxPageSize = 10; // 10 par défaut


        public BaseController(TContext context)
        {
            _context = context;
        }
     
        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<TModel>>> GetAll([FromQuery] Dictionary<string, string> queryParams)
        {
            try
            {
                // supprimer asc, desc, range et fields des filtres
                var filters = queryParams
                    .Where(kv => kv.Key != "asc" && kv.Key != "desc" && kv.Key != "range" && kv.Key != "fields")
                    .ToDictionary(kv => kv.Key, kv => kv.Value);


                // query de base
                var query = _context.Set<TModel>().Where(x => x.Deleted == false);

                // filtres dynamiques
                if (filters != null)
                {
                    foreach (var filter in filters)
                    {
                        query = query.ApplyFilter(filter.Key, filter.Value);
                    }
                }

                // tri ascendant et descendant
                queryParams.TryGetValue("asc", out var asc);
                queryParams.TryGetValue("desc", out var desc);

                query = query.ApplySortOnFields(asc, desc);

                // pagination
                queryParams.TryGetValue("range", out var range);
                var totalItems = query.Count();

                var (skip, take) = ParseRangeParameter(range, totalItems);
                query = query.Skip(skip).Take(take);

                AddPaginationHeaders(Request, Response, skip, take, totalItems);

                // Build la query finale
                return Ok(await query.ToListAsync());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/[Models]/5
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<TModel>> GetById(int id)
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
            return Ok(model);
        }

        // PUT: api/[Models]/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
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
        public virtual async Task<ActionResult<TModel>> Post(TModel model)
        {
            _context.Set<TModel>().Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetById", new { id = model.ID }, model);
        }

        // DELETE: api/[Models]/5
        [HttpDelete("{id}")]
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

        private static (int skip, int take) ParseRangeParameter(string? range, int totalItems)
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


        private void AddPaginationHeaders(HttpRequest request, HttpResponse response, int skip, int take, int totalItems)
        {
            response.Headers.AcceptRanges = $"{typeof(TModel).Name} {MaxPageSize}"; // Indique la pagination possible par blocs de 50

            // Définir le Content-Range
            int end = Math.Min(skip + take - 1, totalItems - 1);
            response.Headers.ContentRange = $"{skip}-{end}/{totalItems}";

            // Construire les liens de navigation
            var baseUrl = $"{request.Scheme}://{request.Host}{request.Path}";
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

            response.Headers.Link = string.Join(", ", links);
        }

    }
}

