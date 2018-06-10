using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using PeopleSearch.Controllers;
using PeopleSearch.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Resources;
using System.Globalization;

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

        [Theory]
        [InlineData("profile_placeholder")]
        [InlineData("woman_960_720")]
        public void PutImage_WithIdNoPersonId_SuccessfulUpdate(string name)
        {
            byte[] data = GetEmbeddedImage(name);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);

            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyUpdateAndSave();
        }

        [Theory]
        [InlineData("man_960_720")]
        [InlineData("mr_ed_960_720")]
        [InlineData("world")]
        public void PutImage_WithIdNoPersonId_Conflict(string name)
        {
            byte[] data = GetEmbeddedImage(name);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);

            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<ConflictResult>(result);

            VerifyNoChangeOrSave();
        }

        [Theory]
        [InlineData("man_960_720", "1")]
        [InlineData("mr_ed_960_720", "2")]
        [InlineData("profile_placeholder", "3")]
        [InlineData("woman_960_720", "4")]
        [InlineData("world", "5")]
        public void PutImage_NoIdWithPersonId_SuccessfulAdd(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);

            AvatarImage avatar = new AvatarImage
            {
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyAddAndSave();
        }

        [Theory]
        [InlineData("man_960_720", "2")]
        [InlineData("mr_ed_960_720", "7")]
        [InlineData("world", "1")]
        public void PutImage_WithIdWithPersonId_SuccessfulUpdate(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);

            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyUpdateAndSave();
        }

        [Theory]
        [InlineData("man_960_720", "100")]
        [InlineData("mr_ed_960_720", "102")]
        [InlineData("world", "103")]
        public void PutImage_WithIdWithPersonId_Conflict(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);

            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<ConflictResult>(result);

            VerifyNoChangeOrSave();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("not a b64 string")]
        public void PutImage_BadB64Data_BadRequest(string data)
        {
            AvatarImage avatar = new AvatarImage
            {
                Id = "world.png",
                PersonId = "1",
                B64Data = data
            };

            var result = _controller.Put(avatar);
            Assert.IsType<BadRequestResult>(result);

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

        private byte[] GetEmbeddedImage(string name)
        {
            ResourceSet images = Properties.Resources.ResourceManager
                .GetResourceSet(CultureInfo.CurrentCulture, true, true);

            if (images != null)
            {
                var obj = images.GetObject(name);
                if (obj is byte[] data)
                {
                    return data;
                }
            }
            return null;
        }
    }
}
