using Xunit;
using Moq;
using PeopleSearch.Controllers;
using PeopleSearch.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Resources;
using System.Globalization;
using System;

namespace PeopleSearch.Tests
{
    public class ImageControllerTest : ControllerTestBase
    {
        private readonly ImageController _controller;

        public ImageControllerTest()
        {
            MockDbContext();

            // populate the _people DBSet
            DbUtils.AddPeople(_context.Object);

            // construct the controller we're testing - this will populate _images DbSet
            _controller = new ImageController(_context.Object, _imageLocalizer.Object);

            // verify the _images DbSet was populated
            _images.Verify(p => p.Add(It.IsAny<ImageEntry>()), Times.Exactly(5));
            Assert.Equal(5, _context.Object.ImageEntries.Count());

            // verify the _people DbSet was populated
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Exactly(5));
            Assert.Equal(5, _context.Object.PersonEntries.Count());

            VerifySaveDbOnce();

            _context.ResetCalls();
            _images.ResetCalls();
            _people.ResetCalls();
            _imageLocalizer.ResetCalls();
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

            VerifyNoDbChanges();
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
            Assert.IsType<BadRequestObjectResult>(result.Result);
            VerifyNoDbChanges();
        }

        [Theory]
        [InlineData("profile_placeholder")]
        [InlineData("woman_960_720")]
        public void PutImage_WithIdNoPersonId_SuccessfulUpdate(string name)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyUpdateImage();
            VerifySaveDbOnce();
        }

        [Theory]
        [InlineData("man_960_720")]
        [InlineData("mr_ed_960_720")]
        [InlineData("world")]
        public void PutImage_WithIdNoPersonId_Conflict(string name)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<ConflictObjectResult>(result);

            VerifyNoDbChanges();
        }

        [Theory]
        [InlineData("man_960_720", "2")]
        [InlineData("mr_ed_960_720", "7")]
        [InlineData("world", "1")]
        public void PutImage_NoIdWithPersonId_SuccessfulAdd(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyAddImage();
            VerifyUpdatePerson();
            VerifySaveDbOnce();
        }

        [Theory]
        [InlineData("woman_960_720", "3")]
        [InlineData("man_960_720", "4")]
        [InlineData("mr_ed_960_720", "5")]
        [InlineData("world", "6")]
        public void PutImage_NoIdWithInvalidPersonId_BadRequest(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<BadRequestObjectResult>(result);

            VerifyNoDbChanges();
        }

        [Theory]
        [InlineData("man_960_720", "2")]
        [InlineData("mr_ed_960_720", "7")]
        [InlineData("world", "1")]
        public void PutImage_WithIdWithPersonId_SuccessfulUpdate(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<OkResult>(result);

            VerifyUpdateImage();
            VerifyUpdatePerson();
            VerifySaveDbOnce();
        }

        [Theory]
        [InlineData("woman_960_720", "3")]
        [InlineData("man_960_720", "4")]
        [InlineData("mr_ed_960_720", "5")]
        [InlineData("world", "6")]
        public void PutImage_WithIdWithInvalidPersonId_Conflict(string name, string personId)
        {
            byte[] data = GetEmbeddedImage(name);
            AvatarImage avatar = new AvatarImage
            {
                Id = name + ".png",
                PersonId = personId,
                B64Data = Convert.ToBase64String(data)
            };

            var result = _controller.Put(avatar);
            Assert.IsType<BadRequestObjectResult>(result);

            VerifyNoDbChanges();
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
            Assert.IsType<BadRequestObjectResult>(result);

            VerifyNoDbChanges();
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
                    Assert.NotNull(data);
                    Assert.True(data.Length > 0);
                    return data;
                }
            }
            return null;
        }
    }
}
