using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SportConnect.Models;
using SportConnect.Web.Controllers;
using System.Security.Claims;

namespace SportConnect.Tests.Controllers
{
    public class HomeControllerTests
    {
        private Mock<UserManager<SportConnectUser>> _userManagerMock;
        private HomeController _controller;

        [SetUp]
        public void SetUp()
        {
            var store = new Mock<IUserStore<SportConnectUser>>();
            _userManagerMock = new Mock<UserManager<SportConnectUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _controller = new HomeController(_userManagerMock.Object);
        }

        [Test]
        public void Constructor_ShouldInjectDependencies()
        {
            Assert.IsNotNull(_controller); Assert.IsInstanceOf<HomeController>(_controller); Assert.AreEqual(_userManagerMock.Object, _controller._userManager);
        }

        [Test]
        public async Task Index_UserIsLoggedIn_SetsViewBagUserName()
        {
            var user = new SportConnectUser { UserName = "TestUser" };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "TestUser")
            }));

            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _controller.ControllerContext = controllerContext;
            _userManagerMock.Setup(um => um.GetUserAsync(claimsPrincipal))
                .ReturnsAsync(user);

            var result = await _controller.Index();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual("TestUser", _controller.ViewBag.UserName);
        }

        [Test]
        public async Task Index_UserIsNotLoggedIn_ViewBagUserNameIsNull()
        {
            var claimsPrincipal = new ClaimsPrincipal();
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _controller.ControllerContext = controllerContext;
            _userManagerMock.Setup(um => um.GetUserAsync(claimsPrincipal))
                .ReturnsAsync((SportConnectUser)null);

            var result = await _controller.Index();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsNull(_controller.ViewBag.UserName);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }
    }
}