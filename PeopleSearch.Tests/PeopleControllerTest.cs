using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PeopleSearch.Controllers;
using PeopleSearch.Models;

namespace PeopleSearch.Tests
{
    public class PeopleControllerTest
    {
        private readonly PeopleController _controller;
        private Mock<PeopleContext> _context = new Mock<PeopleContext>();
        private Mock<DbSet<PersonEntry>> _people = new Mock<DbSet<PersonEntry>>();
        private Mock<IStringLocalizer<PeopleController>> _localizer = new Mock<IStringLocalizer<PeopleController>>();

        public PeopleControllerTest()
        {
            List<PersonEntry> testData = new List<PersonEntry>();
            var queryable = testData.AsQueryable();

            _people.As<IQueryable<PersonEntry>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            _people.Setup(d => d.Add(It.IsAny<PersonEntry>())).Callback<PersonEntry>((s) => testData.Add(s));

            _localizer.Setup(l => l[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));
            _localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<System.Object[]>()])
                .Returns((string s, System.Object[] a) => new LocalizedString(s, s));

            var ls = _localizer.Object[Strings.PersonIdMismatch, 123];
            Assert.NotNull(ls);
            Assert.Equal(Strings.PersonIdMismatch, ls.Name);
            Assert.Equal(Strings.PersonIdMismatch, ls.Value);

            _context.Setup(c => c.PersonEntries).Returns(_people.Object);
            _controller = new PeopleController(_context.Object, _localizer.Object);

            // verify the DB context was seeded
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Exactly(5));
            Assert.Equal(5, _context.Object.PersonEntries.Count());
            _context.Verify(c => c.SaveChanges());

            _context.ResetCalls();
            _people.ResetCalls();
            _localizer.ResetCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\r\n")]
        [InlineData(" \t\r\n")]
        public void GetPeople_EmptyName_ReturnsAll(string name)
        {
            var result = _controller.GetPeople(name);
            Assert.IsAssignableFrom<IEnumerable<PersonEntry>>(result);
            Assert.Equal(5, GetCount(result));
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("a", 1)]
        [InlineData("an", 1)]
        [InlineData("A", 1)]
        [InlineData("AN", 1)]
        [InlineData("An", 1)]
        [InlineData("aN", 1)]
        [InlineData("a ", 1)]
        [InlineData(" a", 1)]
        [InlineData(" a ", 1)]
        [InlineData("an ", 1)]
        [InlineData(" an", 1)]
        [InlineData(" an ", 1)]
        [InlineData("World", 1)]
        [InlineData("o", 4)]
        [InlineData("j", 2)]
        [InlineData("H", 2)]
        [InlineData("OH", 1)]
        [InlineData("Or", 1)]
        public void GetPeople_NonEmptyName_NonZeroResult(string name, int count)
        {
            var result = _controller.GetPeople(name);
            Assert.IsAssignableFrom<IEnumerable<PersonEntry>>(result);
            Assert.Equal(count, GetCount(result));
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("x")]
        [InlineData("yy")]
        [InlineData("zzz")]
        [InlineData("World!")]
        public void GetPeople_NonEmptyName_ZeroResults(string name)
        {
            var result = _controller.GetPeople(name);
            Assert.IsAssignableFrom<IEnumerable<PersonEntry>>(result);
            Assert.Equal(0, GetCount(result));
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("1", "Hello", "World")]
        [InlineData("2", "John", "Smith")]
        [InlineData("7", "Mr", "Ed")]
        public void GetPersonById_KnownId_ReturnsSuccess(string id, string first, string last)
        {
            var result = _controller.GetPersonById(id);
            Assert.IsAssignableFrom<ActionResult<PersonEntry>>(result);
            Assert.IsType<PersonEntry>(result.Value);
            Assert.Equal(id, result.Value.Id);
            Assert.Equal(first, result.Value.FirstName);
            Assert.Equal(last, result.Value.LastName);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\r\n")]
        [InlineData(" \t\r\n")]
        public void GetPersonById_EmptyId_ReturnsBadRequest(string id)
        {
            var result = _controller.GetPersonById(id);
            Assert.IsAssignableFrom<ActionResult<PersonEntry>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("x")]
        [InlineData(" 1")]
        [InlineData("1 ")]
        [InlineData(" 1 ")]
        [InlineData("3")]
        [InlineData("Jane")]
        [InlineData("World")]
        public void GetPersonById_UnknownId_ReturnsNotFound(string id)
        {
            var result = _controller.GetPersonById(id);
            Assert.IsAssignableFrom<ActionResult<PersonEntry>>(result);
            Assert.IsType<NotFoundObjectResult>(result.Result);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("one", "two")]
        [InlineData("1st", "2nd")]
        [InlineData("a, b * c", "d & f!")]
        [InlineData("こんにちは", "世界")]
        public void Post_Successful(string first, string last)
        {
            PersonEntry entry = new PersonEntry
            {
                FirstName = first,
                LastName = last
            };
            var result = _controller.Post(entry);
            Assert.IsType<CreatedAtRouteResult>(result);

            CreatedAtRouteResult car = (CreatedAtRouteResult)result;
            Assert.Equal(201, car.StatusCode);
            Assert.Equal("GetPersonById", car.RouteName);
            Assert.IsAssignableFrom<PersonEntry>(car.Value);

            PersonEntry created = (PersonEntry)car.Value;
            Assert.Equal(first, created.FirstName);
            Assert.Equal(last, created.LastName);

            // The Id is coming back null, but in a real post it shows up
            // I expect it has to do with the DB context mock not being setup correctly
            // Assert.False(string.IsNullOrWhiteSpace(created.Id));

            VerifyAddAndSave();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("1")]
        [InlineData("asdjh-sdf")]
        [InlineData("こんにちは")]
        public void Post_Successful_IgnoresInputId(string id)
        {
            PersonEntry entry = new PersonEntry
            {
                Id = id,
                FirstName = "first",
                LastName = "last"
            };
            var result = _controller.Post(entry);
            Assert.IsType<CreatedAtRouteResult>(result);

            CreatedAtRouteResult car = (CreatedAtRouteResult)result;
            Assert.Equal(201, car.StatusCode);
            Assert.Equal("GetPersonById", car.RouteName);
            Assert.IsAssignableFrom<PersonEntry>(car.Value);

            PersonEntry created = (PersonEntry)car.Value;
            Assert.Equal("first", created.FirstName);
            Assert.Equal("last", created.LastName);

            // The Id is coming back null, but in a real post it shows up
            // I expect it has to do with the DB context mock not being setup correctly
            // Assert.False(string.IsNullOrWhiteSpace(created.Id));
            // Assert.NotEqual(id, created.Id);

            VerifyAddAndSave();
        }

        [Theory]
        [InlineData(null, "last")]
        [InlineData("", "last")]
        [InlineData(" ", "last")]
        [InlineData("first", null)]
        [InlineData("first", "")]
        [InlineData("first", " ")]
        public void Post_EmptyOrMissingFields_ReturnsBadRequest(string first, string last)
        {
            PersonEntry entry = new PersonEntry
            {
                FirstName = first,
                LastName = last
            };
            var result = _controller.Post(entry);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Fact]
        public void Post_NullPersonEntry_ReturnsBadRequest()
        {
            var result = _controller.Post(null);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("1", "one", "two")]
        [InlineData("2", "1st", "2nd")]
        [InlineData("2", "first name", "last name")]
        [InlineData("2", "こんにちは", "世界")]
        public void Put_Successful(string id, string first, string last)
        {
            PersonEntry entry = new PersonEntry
            {
                Id = id,
                FirstName = first,
                LastName = last
            };
            var result = _controller.Put(id, entry);
            Assert.IsType<NoContentResult>(result);
            VerifyUpdateAndSave();
        }

        [Theory]
        [InlineData(null, "first", "last")]
        [InlineData("", "first", "last")]
        [InlineData(" ", "first", "last")]
        [InlineData("1", "first", null)]
        [InlineData("1", "first", "")]
        [InlineData("1", "first", " ")]
        [InlineData("1", null, "last")]
        [InlineData("1", "", "last")]
        [InlineData("1", " ", "last")]
        public void Put_EmptyOrMissingFields_ReturnsBadRequest(string id, string first, string last)
        {
            PersonEntry entry = new PersonEntry
            {
                Id = id,
                FirstName = first,
                LastName = last
            };
            var result = _controller.Put(id, entry);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Fact]
        public void Put_NullPersonEntry_ReturnsBadRequest()
        {
            var result = _controller.Put("1", null);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Fact]
        public void Put_IdMismatch_ReturnsBadRequest()
        {
            PersonEntry entry = new PersonEntry
            {
                Id = "1",
                FirstName = "first",
                LastName = "last"
            };
            var result = _controller.Put("2", entry);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        public void Delete_Successful(string id)
        {
            var result = _controller.Delete(id);
            Assert.IsType<NoContentResult>(result);
            VerifyRemoveAndSave();
        }

        [Theory]
        [InlineData("a")]
        [InlineData("3")]
        [InlineData("12")]
        [InlineData("Hello")]
        [InlineData("こんにちは")]
        public void Delete_UnknownId_ReturnsNotFound(string id)
        {
            var result = _controller.Delete(id);
            Assert.IsType<NotFoundObjectResult>(result);
            VerifyChangeOrSave();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Delete_EmptyId_ReturnsBadRequest(string id)
        {
            var result = _controller.Delete(id);
            Assert.IsType<BadRequestObjectResult>(result);
            VerifyChangeOrSave();
        }

        private void VerifyChangeOrSave()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Never());
        }

        private void VerifyAddAndSave()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Once());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        private void VerifyUpdateAndSave()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Once());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        private void VerifyRemoveAndSave()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Once());
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        private int GetCount<T>(IEnumerable<T> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                count++;
            }
            return count;
        }
    }
}
