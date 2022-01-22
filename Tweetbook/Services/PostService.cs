using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetbook.Data;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
    public class PostService : IPostService
    {
        private readonly DataContext _dbContext;

        public PostService(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            return await _dbContext.Posts.ToListAsync();
        }
        public async Task<Post> GetPostByIdAsync(Guid id)
        {
            return await _dbContext.Posts.SingleOrDefaultAsync(x => x.Id == id);
        }
        public async Task<bool> CreatePostAsync(Post post)
        {
            await _dbContext.AddAsync(post);
            var cretaed = await _dbContext.SaveChangesAsync();
            return cretaed > 0;
        }

        public async Task<bool> UpdatePostAsync(Post postToUpdate)
        {
            _dbContext.Posts.Update(postToUpdate);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }
        public async Task<bool> DeleteAsync(Guid postId)
        {
            var post = await GetPostByIdAsync(postId);
            if (post == null) return false;

            _dbContext.Remove(post);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }
    }
}
