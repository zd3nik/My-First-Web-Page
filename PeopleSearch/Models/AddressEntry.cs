namespace PeopleSearch.Models
{
    public class AddressEntry
    {
        public string Id { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }

        /// <summary>
        /// Update all members except Id using the values from the given AddressEntry object.
        /// </summary>
        /// <param name="other">The AddressEntry object to get updated values from.</param>
        /// <returns>Self</returns>
        public AddressEntry Copy(AddressEntry other)
        {
            // This could also be done with serialization/deserialization or reflection so
            // this method doesn't have to be updated any time the PersonEntry structure changes.
            if (other != null && other != this)
            {
                Line1 = other.Line1;
                Line2 = other.Line2;
                Country = other.Country;
                State = other.State;
                City = other.City;
                ZipCode = other.ZipCode;
            }
            return this;
        }
    }
}
