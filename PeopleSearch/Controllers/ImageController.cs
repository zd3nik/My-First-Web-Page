using System.IO;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PeopleSearch.Models;
using System.Resources;
using System.Collections;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using System;

namespace PeopleSearch.Controllers
{
    [Route("api/[controller]")]
    public class ImageController : Controller
    {
        private static object _contextLock = new object();
        private readonly PeopleContext _context;

        /// <summary>
        /// Construct a new ImageController object.
        /// </summary>
        /// <param name="context">The people DB context to use in this instance.</param>
        public ImageController(PeopleContext context)
        {
            _context = context;
            AddMockData();
        }

        /// <summary>
        /// GET api/image/{id}
        /// </summary>
        /// <param name="id">The Id of the image to get.</param>
        /// <returns>the image object with the specified Id as a base64 encoded string.</returns>
        /// <remarks>Generates NOT FOUND status if the specified Id is not in the DB</remarks>
        [HttpGet("{id}", Name = "GetImageById")]
        public ActionResult<byte[]> GetImageById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            ImageEntry entry = GetExisting(id);
            if (entry == null)
            {
                return NotFound();
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

        /*
        /// <summary>
        /// POST api/image
        /// Add a new image to the DB.
        /// </summary>
        /// <returns>an error status if the given image could not be added to the DB.</returns>
        /// <remarks>If a Person-Id value is in the POST header that value will be assigned to the personId
        /// property of the stored image.</remarks>
        [HttpPost]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest();
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string tmpFilePath = Path.GetTempFileName();
                    try
                    {
                        using (var stream = new FileStream(tmpFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        ImageEntry image = new ImageEntry();
                        image.PersonId = GetRequestHeaderString("Person-Id");
                        image.Data = await System.IO.File.ReadAllBytesAsync(tmpFilePath);

                        _context.ImageEntries.Add(image);
                    }
                    finally
                    {
                        System.IO.File.Delete(tmpFilePath);
                    }
                }
                _context.SaveChanges();
            }

            return Ok();
        }
        */

        /// <summary>
        /// PUT api/image
        /// Upload the avatar image for a specific person.
        /// </summary>
        /// <param name="image">The image object upload.</param>
        /// <returns>an error status if the given image object could not be added to the DB.</returns>
        [HttpPut]
        public IActionResult Put([FromBody]AvatarImage avatar)
        {
            if (avatar == null || avatar.B64Data == null || avatar.B64Data.Length == 0)
            {
                return BadRequest();
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
                return BadRequest();
            }

            lock (_contextLock)
            {
                ImageEntry existing = null;
                if (!string.IsNullOrWhiteSpace(image.Id))
                {
                    existing = GetExisting(image.Id);
                    if (existing != null)
                    {
                        // updating an existing entry, make sure person ID in new image entry matches
                        if (!string.IsNullOrWhiteSpace(existing.PersonId) &&
                            !existing.PersonId.Equals(image.PersonId))
                        {
                            return Conflict();
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
                _context.SaveChanges();

                return Ok();
            }
        }

        /// <summary>
        /// Get an existing image entry from the DB
        /// </summary>
        /// <param name="id">The Id of the image entry to get.</param>
        /// <returns>null if the specified image Id is not in the DB.</returns>
        protected ImageEntry GetExisting(string id)
        {
            return _context.ImageEntries.SingleOrDefault(p => string.Equals(p.Id, id));
        }

        /// <summary>
        /// Get a single string value from the current http request header.
        /// </summary>
        /// <param name="name">The name of the header value to get.</param>
        /// <returns>null if the current http header does not contain the specified value.</returns>
        protected string GetRequestHeaderString(string name)
        {
            StringValues values;
            if (HttpContext.Request.Headers.TryGetValue(name, out values))
            {
                return values.First<string>();
            }
            return null;
        }

        /// <summary>
        /// Add images from the project resources to the people DB.
        /// </summary>
        protected void AddMockData()
        {
            lock (_contextLock)
            {
                if (_context.ImageEntries.Count() > 0)
                {
                    return;
                }

                ResourceSet images = Properties.Resources.ResourceManager
                    .GetResourceSet(CultureInfo.CurrentCulture, true, true);

                if (images != null)
                {
                    foreach (DictionaryEntry entry in images)
                    {
                        if (entry.Value is byte[] data)
                        {
                            string name = entry.Key.ToString();
                            ImageEntry imageEntry = new ImageEntry();
                            imageEntry.Id = name + ".png";
                            imageEntry.Data = data;
                            switch (name)
                            {
                                case "world":
                                    imageEntry.PersonId = "1";
                                    break;
                                case "man_960_720":
                                    imageEntry.PersonId = "2";
                                    break;
                                case "mr_ed_960_720":
                                    imageEntry.PersonId = "7";
                                    break;

                            }
                            _context.ImageEntries.Add(imageEntry);
                        }
                    }

                    _context.SaveChanges();
                }
            }
        }
    }
}
