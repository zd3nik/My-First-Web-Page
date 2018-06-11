using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using PeopleSearch.Controllers;
using PeopleSearch.Models;
using System.Collections.Generic;
using System.Linq;

namespace PeopleSearch.Tests
{
    public class ControllerTestBase
    {
        protected Mock<PeopleContext> _context = new Mock<PeopleContext>();
        protected Mock<DbSet<ImageEntry>> _images = new Mock<DbSet<ImageEntry>>();
        protected Mock<DbSet<PersonEntry>> _people = new Mock<DbSet<PersonEntry>>();
        protected Mock<IStringLocalizer<PeopleController>> _peopleLocalizer = new Mock<IStringLocalizer<PeopleController>>();
        protected Mock<IStringLocalizer<ImageController>> _imageLocalizer = new Mock<IStringLocalizer<ImageController>>();

        protected ControllerTestBase() { }

        protected void MockDbContext()
        {
            MockImageDbSet();
            MockPersonDbSet();
            MockLocalizers();

            _context.Setup(c => c.ImageEntries).Returns(_images.Object);
            _context.Setup(c => c.PersonEntries).Returns(_people.Object);
        }

        protected void MockImageDbSet()
        {
            List<ImageEntry> imageList = new List<ImageEntry>();
            var imagesLinq = imageList.AsQueryable();

            _images.As<IQueryable<ImageEntry>>().Setup(m => m.Provider).Returns(imagesLinq.Provider);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.Expression).Returns(imagesLinq.Expression);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.ElementType).Returns(imagesLinq.ElementType);
            _images.As<IQueryable<ImageEntry>>().Setup(m => m.GetEnumerator()).Returns(() => imagesLinq.GetEnumerator());
            _images.Setup(d => d.Add(It.IsAny<ImageEntry>())).Callback<ImageEntry>((s) => imageList.Add(s));
        }

        protected void MockPersonDbSet()
        {
            List<PersonEntry> personList = new List<PersonEntry>();
            var peopleLinq = personList.AsQueryable();

            _people.As<IQueryable<PersonEntry>>().Setup(m => m.Provider).Returns(peopleLinq.Provider);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.Expression).Returns(peopleLinq.Expression);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.ElementType).Returns(peopleLinq.ElementType);
            _people.As<IQueryable<PersonEntry>>().Setup(m => m.GetEnumerator()).Returns(() => peopleLinq.GetEnumerator());
            _people.Setup(d => d.Add(It.IsAny<PersonEntry>())).Callback<PersonEntry>((s) => personList.Add(s));
        }

        protected void MockLocalizers()
        {
            _peopleLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));
            _peopleLocalizer.Setup(l => l[It.IsAny<string>(), It.IsAny<System.Object[]>()])
                .Returns((string s, System.Object[] a) => new LocalizedString(s, s));

            _imageLocalizer.Setup(l => l[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));
            _imageLocalizer.Setup(l => l[It.IsAny<string>(), It.IsAny<System.Object[]>()])
                .Returns((string s, System.Object[] a) => new LocalizedString(s, s));
        }

        protected void VerifyNoDbChanges()
        {
            VerifyNoChangePeople();
            VerifyNoChangeImages();
            VerifyNoDbSave();
        }

        protected void VerifyNoDbSave()
        {
            _context.Verify(c => c.SaveChanges(), Times.Never());
        }

        protected void VerifySaveDbOnce()
        {
            _context.Verify(c => c.SaveChanges(), Times.Once());
        }

        protected void VerifyNoChangePeople()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
        }

        protected void VerifyAddPerson()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Once());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
        }

        protected void VerifyUpdatePerson()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Once());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Never());
        }

        protected void VerifyRemovePerson()
        {
            _people.Verify(p => p.Add(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Update(It.IsAny<PersonEntry>()), Times.Never());
            _people.Verify(p => p.Remove(It.IsAny<PersonEntry>()), Times.Once());
        }

        protected void VerifyNoChangeImages()
        {
            _images.Verify(i => i.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Remove(It.IsAny<ImageEntry>()), Times.Never());
        }

        protected void VerifyAddImage()
        {
            _images.Verify(i => i.Add(It.IsAny<ImageEntry>()), Times.Once());
            _images.Verify(i => i.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Remove(It.IsAny<ImageEntry>()), Times.Never());
        }

        protected void VerifyUpdateImage()
        {
            _images.Verify(i => i.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Update(It.IsAny<ImageEntry>()), Times.Once());
            _images.Verify(i => i.Remove(It.IsAny<ImageEntry>()), Times.Never());
        }

        protected void VerifyRemoveImage()
        {
            _images.Verify(i => i.Add(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Update(It.IsAny<ImageEntry>()), Times.Never());
            _images.Verify(i => i.Remove(It.IsAny<ImageEntry>()), Times.Once());
        }

        protected int GetCount<T>(IEnumerable<T> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                count++;
            }
            return count;
        }
    }
}
