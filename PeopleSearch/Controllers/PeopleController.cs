﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PeopleSearch.Models;

namespace PeopleSearch.Controllers
{
    [Route("api/[controller]")]
    public class PeopleController : Controller
    {
        private static object _contextLock = new object();
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

            lock (_contextLock)
            {
                if (_context.PersonEntries.Count() == 0)
                {
                    DbUtils.AddPeople(_context);
                    context.SaveChanges();
                }
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
            PersonEntry entry = GetExistingPerson(id);
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
        public IActionResult Post([FromBody]PersonEntry person)
        {
            var insane = SanityCheckAdd(person);
            if (insane != null)
            {
                return insane;
            }

            lock (_contextLock) {
                // make sure a new Id is generated
                person.Id = null;

                _context.PersonEntries.Add(person);
                _context.SaveChanges();

                return CreatedAtRoute("GetPersonById", new { id = person.Id }, person);
            }
        }

        /// <summary>
        /// PUT api/people/{id}
        /// Update an existing person in the people DB.
        /// </summary>
        /// <param name="person">The person object to update.</param>
        /// <returns>an error status if the given person object could not be updated.</returns>
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody]PersonEntry person)
        {
            var insane = SanityCheckUpdate(id, person);
            if (insane != null)
            {
                return insane;
            }

            lock (_contextLock)
            {
                PersonEntry existing = GetExistingPerson(id);
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

            lock (_contextLock)
            {
                PersonEntry person = GetExistingPerson(id);
                if (person == null)
                {
                    return NotFound(_localizer[Strings.PersonIdNotFound, id].Value);
                }

                _context.PersonEntries.Remove(person);
                _context.SaveChanges();

                return NoContent();
            }
        }

        /// <summary>
        /// Get an existing person entry from the DB
        /// </summary>
        /// <param name="id">The Id of the person entry to get.</param>
        /// <returns>null if the specified person Id is not in the DB.</returns>
        protected PersonEntry GetExistingPerson(string id)
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
