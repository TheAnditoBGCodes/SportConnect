using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using NUnit.Framework;
using SportConnect.Services;

namespace SportConnect.Tests.Services
{
    [TestFixture]
    public class CountryServiceTests
    {
        private CountryService _countryService;

        [SetUp]
        public void Setup()
        {
            _countryService = new CountryService();
        }

        [Test]
        public void GetAllCountries_SelectListItemsNotSelectedByDefault()
        {
            var result = _countryService.GetAllCountries();

            Assert.IsFalse(result.Any(item => item.Selected), "No country should be selected by default");
        }

        [Test]
        public void GetAllCountries_ReturnsAllCountries()
        {
            var result = _countryService.GetAllCountries();

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<SelectListItem>>(result);
            Assert.AreEqual(250, result.Count);
        }

        [Test]
        public void GetAllCountries_ReturnsCountriesInAlphabeticalOrder()
        {
            var result = _countryService.GetAllCountries();

            var orderedResult = result.OrderBy(x => x.Text).ToList();
            Assert.AreEqual(orderedResult[0].Text, result[0].Text);
            Assert.AreEqual(orderedResult[orderedResult.Count - 1].Text, result[result.Count - 1].Text);

            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.IsTrue(string.Compare(result[i].Text, result[i + 1].Text) <= 0,
                    $"Countries are not in alphabetical order: '{result[i].Text}' should come before '{result[i + 1].Text}'");
            }
        }

        [Test]
        public void GetAllCountries_ReturnedItemsHaveCorrectValueAndText()
        {
            var result = _countryService.GetAllCountries();

            foreach (var item in result)
            {
                Assert.AreEqual(item.Value, item.Text, "Value and Text should be the same for each country");
            }
        }

        [Test]
        public void GetAllCountries_ContainsExpectedCountries()
        {
            var result = _countryService.GetAllCountries();
            var countries = result.Select(x => x.Text).ToList();

            Assert.Contains("България", countries);
            Assert.Contains("САЩ", countries);
            Assert.Contains("Япония", countries);
            Assert.Contains("Австралия", countries);
            Assert.Contains("Германия", countries);
        }

        [Test]
        public void GetAllCountries_DoesNotReturnDuplicates()
        {
            var result = _countryService.GetAllCountries();

            var distinctCount = result.Select(x => x.Text).Distinct().Count();
            Assert.AreEqual(result.Count, distinctCount, "There should be no duplicate countries");
        }
    }
}