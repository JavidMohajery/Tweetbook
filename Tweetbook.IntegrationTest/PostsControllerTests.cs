using FluentAssertions;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Domain;
using Xunit;

namespace Tweetbook.IntegrationTest
{
    public class PostsControllerTests : IntegrationTest
    {
        [Fact]
        public async Task GetAll_WithoutAnyPost_ReturnsEmptyResponse()
        {
            //Arrange
            await AuthenticateAsync();

            //Act
            var response = await TestClient.GetAsync(ApiRoutes.Posts.GetAll);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsAsync<List<Post>>()).Should().BeEmpty();
        }

        [Fact]
        public async Task Get_ReturnsPost_WhenPostExistsInDatabase()
        {
            //Arrange
            await AuthenticateAsync();
            var createdPost = await CreatePostAsync(new CreatePostRequest { Name = "Test Post" });
            //Act
            var response = await TestClient.GetAsync(ApiRoutes.Posts.Get.Replace("{postId}", createdPost.Id.ToString()));
            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var post = await response.Content.ReadAsAsync<Post>();
            post.Id.Should().Be(createdPost.Id);
            post.Name.Should().Be("Test Post");
        }
    }
}
