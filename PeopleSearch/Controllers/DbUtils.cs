using PeopleSearch.Models;
using System.Collections;
using System.Linq;
using System.Resources;

namespace PeopleSearch.Controllers
{
    public class DbUtils
    {
        /// <summary>
        /// Add some people to the given DB context.
        /// </summary>
        /// <param name="context">The DB context where entries will be added.</param>
        /// <remarks>Creates a couple entries with a simple numeric-looking id to make manual
        /// testing easier and to try to confuse the JS front end (for robustness testing).</remarks>
        public static void AddPeople(PeopleContext context)
        {
            context.PersonEntries.Add(new PersonEntry
            {
                Id = "1",
                FirstName = "Hello",
                LastName = "World",
                Gender = "Planet",
                Age = 4543000,
                Interests = "Rotating",
                AvatarId = "world.png",
                Addr1 = "3rd Planet",
                Country = "Milky Way",
                State = "Orian Arm",
                City = "Solar System",
                ZipCode = "0",
            });
            context.PersonEntries.Add(new PersonEntry
            {
                Id = "2",
                FirstName = "John",
                LastName = "Smith",
                Gender = "Male",
                Age = 25,
                Interests = "Making stuff out of metal.",
                AvatarId = "man_960_720.png",
                Addr1 = "123 Main St.",
                Country = "USA",
                State = "UT",
                City = "Salt Lake City",
                ZipCode = "84101",
            });
            context.PersonEntries.Add(new PersonEntry
            {
                FirstName = "Jane",
                LastName = "Doe",
                Gender = "Female",
                Age = 30,
                Interests = "Writing letters.",
                AvatarId = "woman_960_720.png",
                Addr1 = "328 West 89th Street",
                Addr2 = "APT B1",
                Country = "USA",
                State = "NY",
                City = "New York",
                ZipCode = "10024",
            });
            context.PersonEntries.Add(new PersonEntry
            {
                FirstName = "Some",
                LastName = "Person",
            });
            context.PersonEntries.Add(new PersonEntry
            {
                Id = "7",
                FirstName = "Mr",
                LastName = "Ed",
                Gender = "Male",
                Age = 4,
                Interests = "Talking.",
                AvatarId = "mr_ed_960_720.png",
            });
        }

        /// <summary>
        /// Add some images to the given DB context.
        /// </summary>
        /// <param name="context">The DB context where entries will be added.</param>
        /// <param name="images">The set of images to add.</param>
        public static void AddImages(PeopleContext context, ResourceSet images)
        {
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
                        context.ImageEntries.Add(imageEntry);
                    }
                }
            }
        }
    }
}
