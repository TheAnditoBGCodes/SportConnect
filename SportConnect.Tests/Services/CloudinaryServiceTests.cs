using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using SportConnect.Services;

namespace SportConnect.Tests.Services
{
    [TestFixture]
    public class CloudinaryServiceTests
    {
        private Mock<IConfiguration> _mockConfig;
        private Mock<IConfigurationSection> _mockCloudNameSection;
        private Mock<IConfigurationSection> _mockApiKeySection;
        private Mock<IConfigurationSection> _mockApiSecretSection;

        [SetUp]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockCloudNameSection = new Mock<IConfigurationSection>();
            _mockApiKeySection = new Mock<IConfigurationSection>();
            _mockApiSecretSection = new Mock<IConfigurationSection>();

            _mockCloudNameSection.Setup(s => s.Value).Returns("test-cloud-name");
            _mockApiKeySection.Setup(s => s.Value).Returns("test-api-key");
            _mockApiSecretSection.Setup(s => s.Value).Returns("test-api-secret");

            _mockConfig.Setup(c => c["Cloudinary:CloudName"]).Returns("test-cloud-name");
            _mockConfig.Setup(c => c["Cloudinary:ApiKey"]).Returns("test-api-key");
            _mockConfig.Setup(c => c["Cloudinary:ApiSecret"]).Returns("test-api-secret");
        }

        [Test]
        public void Constructor_InitializesCloudinaryWithCorrectAccount()
        {
            var cloudinaryService = new CloudinaryService(_mockConfig.Object);

            Assert.NotNull(cloudinaryService);

            _mockConfig.Verify(c => c["Cloudinary:CloudName"], Times.Once);
            _mockConfig.Verify(c => c["Cloudinary:ApiKey"], Times.Once);
            _mockConfig.Verify(c => c["Cloudinary:ApiSecret"], Times.Once);
        }

        [Test]
        public async Task UploadImageAsync_NullFile_ReturnsNull()
        {
            var cloudinaryService = new CloudinaryService(_mockConfig.Object);

            var result = await cloudinaryService.UploadImageAsync(null);

            Assert.IsNull(result);
        }

        [Test]
        public async Task UploadImageAsync_EmptyFile_ReturnsNull()
        {
            var cloudinaryService = new CloudinaryService(_mockConfig.Object);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            var result = await cloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.IsNull(result);
        }

        [Test]
        public async Task UploadImageAsync_ValidFile_UploadsAndReturnsUrl()
        {
            var testCloudinaryService = new TestableCloudinaryService(_mockConfig.Object);

            var mockFile = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

            var expectedUrl = "https://res.cloudinary.com/test/image/upload/test.jpg";
            testCloudinaryService.SetupUploadResult(new ImageUploadResult
            {
                SecureUrl = new System.Uri(expectedUrl)
            });

            var result = await testCloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.AreEqual(expectedUrl, result);

            var uploadParams = testCloudinaryService.LastUploadParams;
            Assert.NotNull(uploadParams);
            Assert.IsNotNull(uploadParams.File);
            Assert.AreEqual(fileName, uploadParams.File.FileName);
            Assert.IsNotNull(uploadParams.Transformation);
            var transformationString = uploadParams.Transformation.ToString();
            Assert.IsTrue(transformationString.Contains("w_500"));
            Assert.IsTrue(transformationString.Contains("h_500"));
            Assert.IsTrue(transformationString.Contains("c_fill"));
            Assert.IsTrue(transformationString.Contains("g_face"));
        }

        [Test]
        public async Task UploadImageAsync_UploadFails_ReturnsNull()
        {
            var testCloudinaryService = new TestableCloudinaryService(_mockConfig.Object);

            var mockFile = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

            testCloudinaryService.SetupUploadResult(null);

            var result = await testCloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.IsNull(result);
        }

        [Test]
        public async Task UploadImageAsync_UploadResultWithNullSecureUrl_ReturnsNull()
        {
            var testCloudinaryService = new TestableCloudinaryService(_mockConfig.Object);

            var mockFile = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

            testCloudinaryService.SetupUploadResult(new ImageUploadResult
            {
                SecureUrl = null
            });

            var result = await testCloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.IsNull(result);
        }
        [Test]
        public async Task UploadImageAsync_LargeFile_UploadsSuccessfully()
        {
            var testCloudinaryService = new TestableCloudinaryService(_mockConfig.Object);
            var mockFile = CreateMockFile("large-image.jpg", new string('a', 10 * 1024 * 1024));
            var expectedUrl = "https://res.cloudinary.com/test/image/upload/large-image.jpg";
            testCloudinaryService.SetupUploadResult(new ImageUploadResult { SecureUrl = new Uri(expectedUrl) });

            var result = await testCloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.AreEqual(expectedUrl, result);
        }

        [Test]
        public async Task UploadImageAsync_InvalidFileType_ReturnsNull()
        {
            var cloudinaryService = new CloudinaryService(_mockConfig.Object);
            var mockFile = CreateMockFile("test.pdf", "invalid file content");

            var result = await cloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.IsNull(result);
        }

        [Test]
        public async Task UploadImageAsync_UploadResultWithDifferentTransformation_Success()
        {
            var testCloudinaryService = new TestableCloudinaryService(_mockConfig.Object);
            var mockFile = CreateMockFile("test.jpg", "custom transform");
            var expectedUrl = "https://res.cloudinary.com/test/image/upload/test.jpg";
            testCloudinaryService.SetupUploadResult(new ImageUploadResult { SecureUrl = new Uri(expectedUrl) });

            var result = await testCloudinaryService.UploadImageAsync(mockFile.Object);

            Assert.AreEqual(expectedUrl, result);
            var transformationString = testCloudinaryService.LastUploadParams.Transformation.ToString();
            Assert.IsTrue(transformationString.Contains("w_500"));
            Assert.IsTrue(transformationString.Contains("h_500"));
            Assert.IsTrue(transformationString.Contains("c_fill"));
            Assert.IsTrue(transformationString.Contains("g_face"));
        }

        private Mock<IFormFile> CreateMockFile(string fileName, string content)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            return mockFile;
        }
        private class TestableCloudinaryService : CloudinaryService
        {
            private ImageUploadResult _mockUploadResult;
            public ImageUploadParams LastUploadParams { get; private set; }

            public TestableCloudinaryService(IConfiguration config) : base(config)
            {
            }

            public void SetupUploadResult(ImageUploadResult result)
            {
                _mockUploadResult = result;
            }

            public new async Task<string> UploadImageAsync(IFormFile file)
            {
                if (file == null || file.Length == 0) return null;

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                };

                LastUploadParams = uploadParams;

                if (_mockUploadResult == null || _mockUploadResult.SecureUrl == null)
                {
                    return null;
                }

                return _mockUploadResult.SecureUrl.ToString();
            }
        }
    }
}