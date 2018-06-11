using System.IO;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PeopleSearch.Models;
using System.Resources;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace PeopleSearch.Controllers
{
    [Route("api/[controller]")]
    public class ImageController : Controller
    {
        private static object _contextLock = new object();
        private readonly PeopleContext _context;
        private readonly IStringLocalizer<ImageController> _localizer;

        /// <summary>
        /// Construct a new ImageController object.
        /// </summary>
        /// <param name="context">The people DB context to use in this instance.</param>
        public ImageController(PeopleContext context, IStringLocalizer<ImageController> localizer)
        {
            _context = context;
            _localizer = localizer;

            lock (_contextLock)
            {
                if (_context.ImageEntries.Count() == 0)
                {
                    ResourceSet images = Properties.Resources.ResourceManager
                        .GetResourceSet(CultureInfo.CurrentCulture, true, true);

                    DbUtils.AddImages(_context, images);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// GET api/image/{id}
        /// </summary>
        /// <param name="id">The Id of the image to get.</param>
        /// <returns>the image object with the specified Id as a base64 encoded string.</returns>
        /// <remarks>Generates NOT FOUND status if the specified Id is not in the DB</remarks>
        [HttpGet("{id}")]
        public ActionResult<byte[]> GetImageById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(_localizer[Strings.EmptyImageId].Value);
            }

            ImageEntry entry = GetExistingImage(id);
            if (entry == null)
            {
                return NotFound(_localizer[Strings.ImageIdNotFound, id].Value);
            }

            // determine image type from extension in entry.Id
            switch (Path.GetExtension(entry.Id).ToLower())
            {
                case "jpg":
                case "jpeg":
                    return File(entry.Data, "image/jpeg; charset=UTF-8", entry.Id);
            }

            // default to png
            return File(entry.Data, "image/png; charset=UTF-8", entry.Id);
        }

        /// <summary>
        /// POST api/image/avatar
        /// Add a new image to the DB.
        /// </summary>
        /// <param name="personId">The Id of the person the image belongs to.</param>
        /// <returns>an error status if the given image could not be added to the DB.</returns>
        /// <remarks>Expects exactly one file in the form data.
        /// If a Person-Id value is in the POST header that value will be assigned to the personId
        /// property of the stored image.</remarks>       
        [HttpPost("{personId}")]
        public async Task<IActionResult> Post(string personId)
        {
            if (string.IsNullOrWhiteSpace(personId))
            {
                return BadRequest(_localizer[Strings.PersonIdNotFound, personId].Value);
            }

            var files = HttpContext.Request.Form.Files;
            if (files == null)
            {
                return BadRequest(_localizer[Strings.EmptyImageData].Value);
            }
            if (files.Count != 1)
            {
                return BadRequest(_localizer[Strings.OneImageRequired].Value);
            }

            string tmpFilePath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tmpFilePath, FileMode.Create))
                {
                    await files[0].CopyToAsync(stream);
                }

                ImageEntry image = new ImageEntry
                {
                    PersonId = personId,
                    Data = await System.IO.File.ReadAllBytesAsync(tmpFilePath)
                };

                return StoreAvatarImage(image);
            }
            finally
            {
                System.IO.File.Delete(tmpFilePath);
            }
        }

        /// <summary>
        /// PUT api/image
        /// Upload the avatar image for a specific person.
        /// </summary>
        /// <param name="image">The image object being uploaded.</param>
        /// <returns>an error status if the given image object could not be added to the DB.</returns>
        [HttpPut]
        public IActionResult Put([FromBody]AvatarImage avatar)
        {
            if (avatar == null || avatar.B64Data == null || avatar.B64Data.Length == 0)
            {
                return BadRequest(_localizer[Strings.EmptyImageData].Value);
            }

            ImageEntry image = new ImageEntry { Id = avatar.Id, PersonId = avatar.PersonId };
            try
            {
                image.SetB64Data(avatar.B64Data);
                if (image.Data == null || image.Data.Length == 0)
                {
                    throw new Exception("Invalid B64Data");
                }
            }
            catch (Exception e)
            {
                // don't let b64 decoding failure result in Internal Server error
                Console.WriteLine(string.Format("PUT api/image: {0}: id {1}, personId {2}, b64Data {3}",
                    e.Message, avatar.Id, avatar.PersonId, avatar.B64Data));
                return BadRequest(_localizer[Strings.InvalidImageData].Value);
            }

            return StoreAvatarImage(image);
        }

        /// <summary>
        /// Store an avatar image in the people DB.
        /// </summary>
        /// <param name="image">The avatar image to store.</param>
        /// <returns>Http bad request status if the image is empty,
        /// http conflict status if the person Id on the given image is not empty and
        /// does not match the person Id in the DB for the given image.</returns>
        protected IActionResult StoreAvatarImage(ImageEntry image)
        {
            if (image == null || image.Data == null || image.Data.Length == 0)
            {
                return BadRequest(_localizer[Strings.EmptyImageData].Value);
            }

            PersonEntry person = null;
            if (!string.IsNullOrWhiteSpace(image.PersonId))
            {
                person = GetExistingPerson(image.PersonId);
                if (person == null)
                {
                    return BadRequest(_localizer[Strings.PersonIdNotFound, image.PersonId].Value);
                }
            }

            lock (_contextLock)
            {
                ImageEntry existing = null;
                if (!string.IsNullOrWhiteSpace(image.Id))
                {
                    existing = GetExistingImage(image.Id);
                    if (existing != null)
                    {
                        // updating an existing entry, make sure person ID in new image entry matches
                        if (!string.IsNullOrWhiteSpace(existing.PersonId) &&
                            !existing.PersonId.Equals(image.PersonId))
                        {
                            return Conflict(_localizer[Strings.PersonIdMismatch].Value);
                        }
                    }
                }

                if (existing == null)
                {
                    // make sure a new Id is generated
                    image.Id = null;
                    _context.ImageEntries.Add(image);
                }
                else
                {
                    existing.Data = image.Data;
                    existing.PersonId = image.PersonId;
                    _context.ImageEntries.Update(existing);
                }

                if (person != null)
                {
                    person.AvatarId = image.Id;
                    _context.PersonEntries.Update(person);
                }

                _context.SaveChanges();
                return Ok();
            }
        }

        /// <summary>
        /// Get an existing image entry from the DB
        /// </summary>
        /// <param name="id">The Id of the image entry to get.</param>
        /// <returns>null if the specified image Id is not in the DB.</returns>
        protected ImageEntry GetExistingImage(string id)
        {
            return _context.ImageEntries.SingleOrDefault(p => string.Equals(p.Id, id));
        }

        /// <summary>
        /// Get an existing person entry from the DB
        /// </summary>
        /// <param name="id">The Id of the person entry to get.</param>
        /// <returns>null if the specified person Id is not in the DB.</returns>
        protected PersonEntry GetExistingPerson(string id)
        {
            return _context.PersonEntries.SingleOrDefault(p => string.Equals(p.Id, id));
        }

        /// <summary>
        /// Get a single string value from the current http request header.
        /// </summary>
        /// <param name="name">The name of the header value to get.</param>
        /// <returns>null if the current http header does not contain the specified value.</returns>
        protected string GetRequestHeaderString(string name)
        {
            if (HttpContext.Request.Headers.TryGetValue(name, out StringValues values))
            {
                return values.First<string>();
            }
            return null;
        }
    }
}
