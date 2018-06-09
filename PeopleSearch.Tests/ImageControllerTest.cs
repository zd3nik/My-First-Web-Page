using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using PeopleSearch.Controllers;
using PeopleSearch.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace PeopleSearch.Tests
{
    public class ImageControllerTest
    {
        private readonly ImageController _controller;
        private Mock<PeopleContext> _context = new Mock<PeopleContext>();
        private Mock<DbSet<ImageEntry>> _images = new Mock<DbSet<ImageEntry>>();

        public ImageControllerTest()
        {
            List<ImageEntry> testData = new List<ImageEntry>();
            var queryable = testData.AsQueryable();

            _images.As<IQueryable<ImageEntry>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            _images.Setup(d => d.Add(It.IsAny<ImageEntry>())).Callback<ImageEntry>((s) => testData.Add(s));

            _context.Setup(c => c.ImageEntries).Returns(_images.Object);
            _controller = new ImageController(_context.Object);

            // verify the DB context was seeded
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Exactly(5));
            Assert.Equal(5, _context.Object.ImageEntries.Count());
            _context.Verify(c => c.SaveChanges());

            _context.ResetCalls();
            _images.ResetCalls();
        }

        [Theory]
        [InlineData("man_960_720.png", 80093)]
        [InlineData("mr_ed_960_720.png", 116520)]
        [InlineData("profile_placeholder.png", 22102)]
        [InlineData("woman_960_720.png", 98430)]
        [InlineData("world.png", 57236)]
        public void GetImageById_ReturnsSuccess(string id, int size)
        {
            var result = _controller.GetImageById(id);
            Assert.IsAssignableFrom<ActionResult<byte[]>>(result);
            Assert.IsType<FileContentResult>(result.Result);

            FileContentResult file = result.Result as FileContentResult;
            Assert.Equal(id, file.FileDownloadName);
            Assert.Equal(size, file.FileContents.Length);
            Assert.Equal("image/png; charset=UTF-8", file.ContentType);

            VerifyNoChangeOrSave();
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
        public void GetImageById_EmptyId_ReturnsBadRequest(string id)
        {
            var result = _controller.GetImageById(id);
            Assert.IsAssignableFrom<ActionResult<byte[]>>(result);
            Assert.IsType<BadRequestResult>(result.Result);
            VerifyNoChangeOrSave();
        }

        private void VerifyNoChangeOrSave()
        {
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Remove(It.IsAny<ImageEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Never());
        }

        private void VerifyAddAndSave()
        {
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Once());
            _images.Verify(p => p.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Remove(It.IsAny<ImageEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        private void VerifyUpdateAndSave()
        {
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Update(It.IsAny<ImageEntry>()), Times.Once());
            _images.Verify(p => p.Remove(It.IsAny<ImageEntry>()), Times.Never());
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        private void VerifyRemoveAndSave()
        {
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(p => p.Remove(It.IsAny<ImageEntry>()), Times.Once());
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
