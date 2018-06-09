using System.IO;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PeopleSearch.Models;
using System.Resources;
using System.Collections;

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
        /// <remarks>Generates NOT FOUND status if the specified Id is not in the DB.</remarks>
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

                ResourceSet images = Properties.Resources.ResourceManager.GetResourceSet(
                CultureInfo.CurrentCulture, true, true);
                if (images != null)
                {
                    foreach (DictionaryEntry entry in images)
                    {
                        if (entry.Value is byte[] data)
                        {
                            ImageEntry imageEntry = new ImageEntry();
                            imageEntry.Id = entry.Key.ToString() + ".png";
                            imageEntry.Data = data;
                            _context.ImageEntries.Add(imageEntry);
                        }
                    }

                    _context.SaveChanges();
                }
            }
        }
    }
}
