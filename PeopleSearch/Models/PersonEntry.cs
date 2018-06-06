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
        public string Addr1 { get; set; }
        public string Addr2 { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }

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
                Addr1 = other.Addr1;
                Addr2 = other.Addr2;
                Country = other.Country;
                State = other.State;
                City = other.City;
                ZipCode = other.ZipCode;
            }
            return this;
        }
    }
}
