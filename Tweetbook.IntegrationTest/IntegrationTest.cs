using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Data;
using Xunit;

namespace Tweetbook.IntegrationTest
{
    public class IntegrationTest
    {
        protected readonly HttpClient TestClient;
        protected IntegrationTest()
        {
            var webFactory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll(typeof(DataContext));
                        services.AddDbContext<DataContext>(option => option.UseInMemoryDatabase("testDb"));
                    });
                });
            TestClient = webFactory.CreateClient();
        }

        protected async Task AuthenticateAsync()
        {
            TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", await GetJwtAsync());
        }

        private async Task<string> GetJwtAsync()
        {
            var response = await TestClient.PostAsJsonAsync(ApiRoutes.Identity.Register, new UserRegistrationRequest
            { Email = "javid@integrationtest.com", Password = "Javid1234!" });

            var registerationResponse = await response.Content.ReadAsAsync<AuthSuccessResponse>();
            return registerationResponse.Token;
        }
        protected async Task<CreatePostResponse> CreatePostAsync(CreatePostRequest createPostRequest)
        {
            var response = await TestClient.PostAsJsonAsync(ApiRoutes.Posts.Create, createPostRequest);
            return await response.Content.ReadAsAsync<CreatePostResponse>();
        }
    }
}
