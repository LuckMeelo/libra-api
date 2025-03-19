using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibrApi.Data;
using LibrApi.Data.Models;
using ApiLib.Controllers;
using ApiLib.Extensions;
using Asp.Versioning;

namespace LibrApi.Controllers.v1
{
    [ApiVersion(1)]
    public class BooksController : BaseController<LibrApiDbContext, Book>
    {
        public BooksController(LibrApiDbContext context) : base(context)
        {
            //MaxPageSize = 2; // Edit la taille max pour la pagination
        }

    }
}
