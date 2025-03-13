using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibrApi.Data;
using LibrApi.Data.Models;
using ApiLib.Controllers;
using ApiLib.Extensions;

namespace LibrApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : BaseController<LibrApiDbContext, Book>
    {
        public BooksController(LibrApiDbContext context) : base(context)
        {
        }

        public override async Task<ActionResult<IEnumerable<Book>>> GetAll()
        {
            try
            {
                var list = await _context.Books.Where(x => !x.Deleted).SortBy("Title").ToListAsync();
                return Ok(list);
            }
            catch (ArgumentException e)
            {
                return NotFound();
            }
        }
    }
}
