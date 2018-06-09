using System;

namespace PeopleSearch.Models
{
    public class ImageEntry
    {
        public string Id { get; set; }
        public string PersonId { get; set; }
        public byte[] Data { get; set; }

        public string GetB64Data()
        {
            return (Data == null || Data.Length == 0)
                ? ""
                : Convert.ToBase64String(Data);
        }

        public void SetB64Data(string b64)
        {
            Data = (b64 == null || b64.Length == 0)
                ? null
                : Convert.FromBase64String(b64);
        }
    }
}
