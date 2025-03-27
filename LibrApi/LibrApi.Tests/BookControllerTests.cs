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
using NuGet.Protocol;
using System.Reflection;

namespace LibrApi.Tests
{

    public static class ObjectExtensions
    {
        public static Dictionary<string, object?>? ToDictionary(this object? obj)
        {
            if (obj == null)
                return null;

            var dict = new Dictionary<string, object?>();
            Type type = obj.GetType();
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dict[prop.Name] = prop.GetValue(obj);
            }

            return dict;
        }
    }

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
            var books = Assert.IsAssignableFrom<List<Dictionary<string, object?>>>(okResult.Value);
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
            var books = (data as OkObjectResult)!.Value as List<Dictionary<string, object?>>;
            Assert.NotNull(books);

            books[0].TryGetValue("ID", out var id);
            Assert.NotNull(id);

            var result = await _controller.GetById((int)id, queryParams);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedBook = Assert.IsType<Dictionary<string, object?>>(okResult.Value);

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
            var returnedBooks = Assert.IsAssignableFrom<List<Dictionary<string, object?>>>(okResult.Value);
            Assert.Single(returnedBooks);

            Assert.Contains(returnedBooks, b => {
                b.TryGetValue("Title", out var title);
                return (title == "Clean Code");
                });
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
            var books = (data as OkObjectResult)!.Value as List<Dictionary<string, object?>>;
            Assert.NotNull(books);


            books[0].TryGetValue("ID", out var id);
            Assert.NotNull(id);

            var result = await _controller.Delete((int)id);

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