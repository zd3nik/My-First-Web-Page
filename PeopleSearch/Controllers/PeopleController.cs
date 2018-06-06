using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PeopleSearch.Models;

namespace PeopleSearch.Controllers
{
    [Route("api/[controller]")]
    public class PeopleController : Controller
    {
        private readonly PeopleContext _context;
        private readonly IStringLocalizer<PeopleController> _localizer;

        /// <summary>
        /// Construct a new PeopleController object.
        /// </summary>
        /// <param name="context">The people DB context to use in this instance.</param>
        public PeopleController(PeopleContext context, IStringLocalizer<PeopleController> localizer)
        {
            _context = context;
            _localizer = localizer;

            if (_context.PersonEntries.Count() == 0)
            {
                // setup a couple entries with a numeric looking id to try to confuse the JS front end
                // also to make manual testing of the API calls a little easier to input by hand
                _context.PersonEntries.Add(new PersonEntry
                {
                    Id = "1",
                    FirstName = "Hello",
                    LastName = "World",
                    Age = 4543000,
                    Interests = "Rotating",
                    AvatarUri = "asserts/img/world.png",
                    Addr1 = "3rd Planet",
                    Country = "Milky Way",
                    State = "Orian Arm",
                    City = "Solar System",
                    ZipCode = "0"
                });
                _context.PersonEntries.Add(new PersonEntry
                {
                    Id = "2",
                    FirstName = "John",
                    LastName = "Smith",
                    Age = 25,
                    Interests = "Making stuff out of metal.",
                    AvatarUri = "assets/img/man-960_720.png",
                    Addr1 = "123 Main St.",
                    Country = "USA",
                    State = "UT",
                    City = "Salt Lake City",
                    ZipCode = "84101"
                });
                _context.PersonEntries.Add(new PersonEntry
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Age = 30,
                    Interests = "Writing letters.",
                    AvatarUri = "assets/img/woman-960_720.png",
                    Addr1 = "328 West 89th Street",
                    Addr2 = "APT B1",
                    Country = "USA",
                    State = "NY",
                    City = "New York",
                    ZipCode = "10024"
                });
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// GET: api/people?name=text
        /// </summary>
        /// <param name="name">A substring to search for in person first and last name fields.</param>
        /// <returns>all person entries with a first name or last name that contains the specified {name} text.</returns>
        /// <remarks>{name} text comparison is case insensitive.</remarks>
        [HttpGet]
        public IEnumerable<PersonEntry> GetPeople([FromQuery]string name)
        {
            name = (name == null) ? "" : name.Trim().ToLower();

            // It's not necessary to do this name.Length test. PersonEntries.Where() will return the same
            // thing as PersonEntries when the name filter is empty.  It's just "slightly" more efficient
            // to eliminate the Where() call when name is empty.
            if (name.Length > 0)
            {
                return _context.PersonEntries.Where(person =>
                    person.FirstName.ToLower().Contains(name) ||
                    person.LastName.ToLower().Contains(name)
                );
            }

            return _context.PersonEntries;
        }

        /// <summary>
        /// GET api/people/{id}
        /// </summary>
        /// <param name="id">The Id of the person to get.</param>
        /// <returns>the person object with the specified Id.</returns>
        /// <remarks>Generates NOT FOUND status if the specified Id is not in the people DB.</remarks>
        [HttpGet("{id}", Name = "GetPersonById")]
        public ActionResult<PersonEntry> GetPersonById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(_localizer[Strings.EmptyPersonId].Value);
            }
            PersonEntry entry = GetExisting(id);
            if (entry == null)
            {
                return NotFound(_localizer[Strings.PersonIdNotFound, id].Value);
            }
            return entry;
        }

        /// <summary>
        /// POST api/people
        /// Add a new person to the people DB.
        /// </summary>
        /// <param name="person">The person object to add to the DB.</param>
        /// <returns>an error status if the given person object could not be added to the DB.</returns>
        /// <remarks>If the given person object contains an Id value it will be ignored.  The new Id value
        /// of the person will be returned if the post is successful.</remarks>
        [HttpPost]
        public IActionResult Post(PersonEntry person)
        {
            var insane = SanityCheckAdd(person);
            if (insane != null)
            {
                return insane;
            }

            // make sure a new Id is generated
            person.Id = null;

            _context.PersonEntries.Add(person);
            _context.SaveChanges();

            return CreatedAtRoute("GetPersonById", new { id = person.Id }, person);
        }

        /// <summary>
        /// PUT api/people/{id}
        /// Update an existing person in the people DB.
        /// </summary>
        /// <param name="person">The person object to update.</param>
        /// <returns>an error status if the given person object could not be updated.</returns>
        [HttpPut("{id}")]
        public IActionResult Put(string id, PersonEntry person)
        {
            var insane = SanityCheckUpdate(id, person);
            if (insane != null)
            {
                return insane;
            }

            PersonEntry existing = GetExisting(id);
            if (existing == null)
            {
                return NotFound(_localizer[Strings.PersonIdNotFound, id].Value);
            }

            // DB objects often need special care, particularly regarding their Ids, when updating.
            // Do a deep copy from person to existing where the existing object keeps its own Ids.
            existing.Copy(person);

            // NOTE: there is a race condition between GetExisting() and Update() calls
            _context.PersonEntries.Update(existing);
            _context.SaveChanges();

            return NoContent();
        }

        /// <summary>
        /// DELETE api/people/{id}
        /// </summary>
        /// <param name="id">The Id of the person object to delete.</param>
        /// <returns>an error status if the specified person Id could not be deleted.</returns>
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(_localizer[Strings.EmptyPersonId].Value);
            }

            PersonEntry person = GetExisting(id);
            if (person == null)
            {
                return NotFound(_localizer[Strings.PersonIdNotFound, id].Value);
            }

            _context.PersonEntries.Remove(person);
            _context.SaveChanges();

            return NoContent();
        }

        /// <summary>
        /// Get an existing person entry from the DB
        /// </summary>
        /// <param name="id">The Id of the person entry to get.</param>
        /// <returns>null if the specified person Id is not in the DB.</returns>
        protected PersonEntry GetExisting(string id)
        {
            // PersonEntries.Find() was not working with the unit tests DB context mock.
            // Using this as a work-around because figuring out what was wrong with the mock
            // was taking too much time :-)
            return _context.PersonEntries.SingleOrDefault(p => string.Equals(p.Id, id));
        }

        /// <summary>
        /// Perform sanity checks on PersonEntry object for ADD to people DB.
        /// </summary>
        /// <param name="person">The person object to validate.</param>
        /// <returns>null if sane, otherwise an ActionResult with the appropriate error status.</returns>
        protected IActionResult SanityCheckAdd(PersonEntry person)
        {
            if (person == null)
            {
                return BadRequest(_localizer[Strings.UnrecognizedJsonObject].Value);
            }
            if (string.IsNullOrWhiteSpace(person.FirstName))
            {
                return BadRequest(_localizer[Strings.EmptyPersonFirstName].Value);
            }
            if (string.IsNullOrWhiteSpace(person.LastName))
            {
                return BadRequest(_localizer[Strings.EmptyPersonLastName].Value);
            }
            return null;
        }

        /// <summary>
        /// Perform sanity checks on PersonEntry object for UPDATE to people DB.
        /// </summary>
        /// <param name="person">The person object to validate.</param>
        /// <returns>null if sane, otherwise an ActionResult with the appropriate error status.</returns>
        protected IActionResult SanityCheckUpdate(string id, PersonEntry person)
        {
            if (person == null)
            {
                return BadRequest(_localizer[Strings.UnrecognizedJsonObject].Value);
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(_localizer[Strings.EmptyPersonId].Value);
            }
            if (!string.IsNullOrWhiteSpace(person.Id) && (string.Compare(id, person.Id) != 0))
            {
                return BadRequest(_localizer[Strings.PersonIdMismatch].Value);
            }
            if (string.IsNullOrWhiteSpace(person.FirstName))
            {
                return BadRequest(_localizer[Strings.EmptyPersonFirstName].Value);
            }
            if (string.IsNullOrWhiteSpace(person.LastName))
            {
                return BadRequest(_localizer[Strings.EmptyPersonLastName].Value);
            }
            return null;
        }
    }
}
