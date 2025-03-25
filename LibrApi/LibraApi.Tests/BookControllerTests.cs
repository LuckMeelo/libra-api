using LibrApi.Controllers.v1;
using LibrApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using LibrApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LibrApi.Tests
{
    public class BooksControllerTests
    {
        private readonly LibrApiDbContext _context;
        private readonly BooksController _controller;

        public BooksControllerTests()
        {
            var options = new DbContextOptionsBuilder<LibrApiDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new LibrApiDbContext(options);
            _controller = new BooksController(_context);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            SeedTestData();
        }

        private void SeedTestData()
        {
            _context.Books.RemoveRange(_context.Books);
            _context.Books.AddRange(
                new Book {
                    Title = "Clean Code",
                    Author = "Robert C. Martin",
                    Rating = 4.5f,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Deleted = false 
                },
                new Book {
                    Title = "Design Patterns",
                    Author = "Erich Gamma",
                    Rating = 4.8f,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Deleted = false
                }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResultWithBooks()
        {
            var queryParams = new Dictionary<string, string>
            {
                { "range", "0-1" }
            };
            var result = await _controller.GetAll(new Dictionary<string, string>(queryParams));

            var okResult = Assert.IsType<OkObjectResult>(result);
            var books = Assert.IsAssignableFrom<List<Book>>(okResult.Value);
            Assert.Equal(2, books.Count);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsBook()
        {
            var queryParams = new Dictionary<string, string>
            {
                { "range", "0-1" }
            };
            var data = await _controller.GetAll(new Dictionary<string, string>(queryParams));
            var books = (data as OkObjectResult)!.Value as List<Book>;
            Assert.NotNull(books);

            var result = await _controller.GetById(books[0].ID);
            var actionResult = Assert.IsType<ActionResult<Book>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedBook = Assert.IsType<Book>(okResult.Value);

            Assert.Equal(books[0], returnedBook);
            Debug.WriteLine("Returned Book:");
            Debug.WriteLine(JsonConvert.SerializeObject(returnedBook, Formatting.Indented));
        }

        [Fact]
        public async Task Search_ValidQuery_ReturnsFilteredResults()
        {
            // Act
            var result = await _controller.Search(new Dictionary<string, string>
            {
                { "Title", "Clean*" }
            });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            //Debug.WriteLine("OkResult Book:");
            //Debug.WriteLine(JsonConvert.SerializeObject(okResult, Formatting.Indented));
            var returnedBooks = Assert.IsAssignableFrom<IEnumerable<Book>>(okResult.Value);
            Assert.Single(returnedBooks);
            Assert.Contains(returnedBooks, b => b.Title == "Clean Code");
        }

        [Fact]
        public async Task Post_ValidBook_ReturnsCreatedAtAction()
        {
            var newBook = new Book {
                Title = "New Book",
                Author = "Author",
                Rating = 4.8f,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Deleted = false,

            };

            var result = await _controller.Post(newBook);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetById", createdAtResult.ActionName);
            Assert.Equal(newBook.ID, createdAtResult.RouteValues!["id"]);
            Assert.Equal(newBook.Title, (createdAtResult.Value as Book)?.Title);
        }

        [Fact]
        public async Task Delete_ValidId_RemovesBook()
        {
            var queryParams = new Dictionary<string, string>
            {
                { "range", "0-1" }
            };
            var data = await _controller.GetAll(new Dictionary<string, string>(queryParams));
            var books = (data as OkObjectResult)!.Value as List<Book>;
            Assert.NotNull(books);

            var result = await _controller.Delete(books[0].ID);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            var result = await _controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}