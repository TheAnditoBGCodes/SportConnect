using Moq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SportConnect.DataAccess.Repository;
using SportConnect.DataAccess;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using SportConnect.Models;
using System;
using Microsoft.AspNetCore.Mvc;
using SportConnect.Web.Controllers;

namespace SportConnect.Tests.Repository
{
    [TestFixture]
    public class RepositoryTests
    {
        private SportConnectDbContext _context;
        private Repository<Sport> _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<SportConnectDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new SportConnectDbContext(options);

            _repository = new Repository<Sport>(_context);
        }

        [Test]
        public void Constructor_ShouldInjectDependencies()
        {
            var contextField = typeof(Repository<Sport>).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dbSetField = typeof(Repository<Sport>).GetField("_dbSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var injectedContext = contextField.GetValue(_repository);
            var injectedDbSet = dbSetField.GetValue(_repository);

            Assert.IsNotNull(injectedContext);
            Assert.IsInstanceOf<SportConnectDbContext>(injectedContext);

            Assert.IsNotNull(injectedDbSet);
            Assert.IsInstanceOf<DbSet<Sport>>(injectedDbSet);
        }

        [Test]
        public async Task Add_ShouldAddSport()
        {
            var sport = new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" };

            await _repository.Add(sport);

            var result = await _context.Set<Sport>().FindAsync(sport.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Football", result.Name);
        }

        [Test]
        public async Task AllWithIncludes_ShouldReturnSports()
        {
            var sports = new List<Sport>
            {
                new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" },
                new Sport { Name = "Basketball", Description = "A popular team sport", ImageUrl = "http://example.com/basketball.png" }
            };

            await _context.Set<Sport>().AddRangeAsync(sports);
            await _context.SaveChangesAsync();

            var result = await _repository.AllWithIncludes();

            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public async Task IsPropertyUnique_ShouldReturnTrue_WhenNoMatch()
        {
            var result = await _repository.IsPropertyUnique(s => s.Name == "Tennis");

            Assert.IsTrue(result);
        }

        [Test]
        public async Task IsPropertyUnique_ShouldReturnFalse_WhenMatchExists()
        {
            var sport = new Sport { Name = "Tennis", Description = "A racket sport", ImageUrl = "http://example.com/tennis.png" };
            await _repository.Add(sport);

            var result = await _repository.IsPropertyUnique(s => s.Name == "Tennis");

            Assert.IsFalse(result);
        }

        [Test]
        public async Task Delete_ShouldRemoveSport()
        {
            var sport = new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" };
            await _repository.Add(sport);

            await _repository.Delete(sport);

            var result = await _context.Set<Sport>().FindAsync(sport.Id);

            Assert.IsNull(result);
        }

        [Test]
        public async Task Save_ShouldSaveChanges()
        {
            var sport = new Sport { Name = "Baseball", Description = "A bat-and-ball sport", ImageUrl = "http://example.com/baseball.png" };
            await _repository.Add(sport);
            await _repository.Save();

            var result = await _context.Set<Sport>().FindAsync(sport.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Baseball", result.Name);
        }
        [Test]
        public async Task DeleteRange_ShouldRemoveSports()
        {
            var sports = new List<Sport>
    {
        new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" },
        new Sport { Name = "Basketball", Description = "A popular team sport", ImageUrl = "http://example.com/basketball.png" }
    };

            await _context.Set<Sport>().AddRangeAsync(sports);
            await _context.SaveChangesAsync();

            // Now delete the sports
            await _repository.DeleteRange(sports);

            var result1 = await _context.Set<Sport>().FindAsync(sports[0].Id);
            var result2 = await _context.Set<Sport>().FindAsync(sports[1].Id);

            Assert.IsNull(result1);
            Assert.IsNull(result2);
        }

        [Test]
        public async Task GetAll_ShouldReturnAllSports()
        {
            var sports = new List<Sport>
            {
                new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" },
                new Sport { Name = "Basketball", Description = "A popular team sport", ImageUrl = "http://example.com/basketball.png" }
            };

            await _context.Set<Sport>().AddRangeAsync(sports);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAll();

            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public async Task GetAllBy_ShouldReturnFilteredSports()
        {
            var sports = new List<Sport>
            {
                new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" },
                new Sport { Name = "Basketball", Description = "A popular team sport", ImageUrl = "http://example.com/basketball.png" }
            };

            await _context.Set<Sport>().AddRangeAsync(sports);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllBy(s => s.Name == "Football");

            Assert.AreEqual(1, result.Count());
        }

        [Test]
        public async Task GetById_ShouldReturnSport()
        {
            var sport = new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" };
            await _repository.Add(sport);

            var result = await _repository.GetById(sport.Id);

            Assert.AreEqual("Football", result?.Name);
        }

        [Test]
        public async Task Update_ShouldUpdateSport()
        {
            var sport = new Sport { Name = "Football", Description = "A popular sport", ImageUrl = "http://example.com/football.png" };
            await _repository.Add(sport);

            sport.Name = "Updated Football";
            await _repository.Update(sport);

            var result = await _context.Set<Sport>().FindAsync(sport.Id);

            Assert.AreEqual("Updated Football", result.Name);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}