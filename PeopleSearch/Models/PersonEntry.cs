namespace PeopleSearch.Models
{
    public class PersonEntry
    {
        public string Id { get; set;  }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string Interests { get; set; }
        public string AvatarUri { get; set; }
        public AddressEntry Address { get; set; }

        /// <summary>
        /// Update all members except Id using the values from the given PersonEntry object.
        /// </summary>
        /// <param name="other">The PersonEntry object to get updated values from.</param>
        /// <returns>Self</returns>
        public PersonEntry Copy(PersonEntry other)
        {
            // This could also be done with serialization/deserialization or reflection so
            // this method doesn't have to be updated any time the PersonEntry structure changes.
            if (other != null && other != this)
            {
                FirstName = other.FirstName;
                LastName = other.LastName;
                Age = other.Age;
                Interests = other.Interests;
                AvatarUri = other.AvatarUri;
                if (other.Address == null)
                {
                    Address = null;
                }
                else
                {
                    if (Address == null)
                    {
                        Address = new AddressEntry();
                    }
                    Address.Copy(other.Address);
                }
            }
            return this;
        }
    }
}
