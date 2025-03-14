using ApiLib.Attributes;
using ApiLib.Data;
using ApiLib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiLib.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public abstract class BaseController<TContext, TModel> : ControllerBase where TContext : BaseDbContext where TModel : BaseModel
    {
        protected readonly TContext _context;

        public BaseController(TContext context)
        {
            _context = context;
        }

        // GET: api/[Models]
        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<TModel>>> GetAll()
        {
            return await _context.Set<TModel>().Where(x => x.Deleted == false).ToListAsync();
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

        private bool ModelExists(int id)
        {
            return _context.Set<TModel>().Any(e => e.ID == id);
        }

    }
}