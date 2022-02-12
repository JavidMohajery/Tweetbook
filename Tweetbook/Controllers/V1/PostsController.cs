using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extensions;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
    [Authorize( AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpGet(ApiRoutes.Posts.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _postService.GetPostsAsync());
        }

        [HttpGet(ApiRoutes.Posts.Get)]
        public async Task<IActionResult> Get([FromRoute] Guid postId)
        {
            var post = await _postService.GetPostByIdAsync(postId);

            if (post == null)
                return NotFound();

            return Ok(post);
        }

        [HttpPost(ApiRoutes.Posts.Create)]
        public async Task<IActionResult> Create([FromBody]CreatePostRequest postRequest)
        {
            var userId = HttpContext.GetUserId();
            var post = new Post { Name = postRequest.Name, UserId = userId};
            
            await _postService.CreatePostAsync(post);
            var locationUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host
                 + "/" + ApiRoutes.Posts.Get.Replace("{postId}", post.Id.ToString());
            var response = new CreatePostResponse { Id = post.Id };

            return Created(locationUrl, response);
        }

        [HttpPut(ApiRoutes.Posts.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostRequest request)
        {
            var userOwnsPost = await _postService.UserOwnsPostAsync(postId, HttpContext.GetUserId());
            if (!userOwnsPost)
            {
                return BadRequest(new {error = "You do not own this post."});
            }

            var post = await _postService.GetPostByIdAsync(postId);
            post.Name = request.Name;
            var updated = await _postService.UpdatePostAsync(post);
            if (updated)
                return Ok();

            return NotFound();
        }

        [HttpDelete(ApiRoutes.Posts.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid postId)
        {
            var userOwnsPost = await _postService.UserOwnsPostAsync(postId, HttpContext.GetUserId());
            if (!userOwnsPost)
            {
                return BadRequest(new {error = "You do not own this post."});
            }
            bool deleted = await _postService.DeleteAsync(postId);
            if (deleted) return NoContent();
            return NotFound();
        }
    }
}
