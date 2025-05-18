using System.Text;
using System.Text.Json;
using API.Extensions;
using API.Helpers;
using API.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Shop.Application.DTOs.Reviews;
using Shop.Application.Interfaces;
using Shop.Application.Services.Abstracts;
using Shop.Application.Ultilities;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;

namespace API.Controllers
{
    public class ReviewController : BaseApiController
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHubContext<ReviewHub> _hub;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IReviewService _reviewService;
        private readonly IConfiguration _configuration;

        public ReviewController(IReviewService reviewService,
            ICloudinaryService cloudinaryService, IHubContext<ReviewHub> hub,
            UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration)
        {
            _reviewService = reviewService;
            _cloudinaryService = cloudinaryService;
            _hub = hub;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetReviews(int productId, [FromQuery] ReviewParams prm)
        {
            var pagedList = await _reviewService.GetAllAsync(productId, prm, false);

            Response.AddPaginationHeader(pagedList);

            return Ok(pagedList);
        }
        [HttpGet("{productId:int}/summary")]
        public async Task<IActionResult> GetReviews(int productId, [FromQuery] ReviewParams prm, [FromQuery] bool summary = false)
        {
            var allReviews = await _reviewService.GetAllAsync(productId, prm, false);
            if (allReviews == null || !allReviews.Any())
                return NotFound("Không có bình luận nào.");

            if (summary)
            {
                // 1. Gộp tất cả reviewText
                var combinedText = string.Join("\n", allReviews.Select(r => r.ReviewText));

                // 2. Gửi lên Gemini API
                var apiKey = _configuration["Gemini:ApiKey"];
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new {
                            parts = new[]
                            {
                                new { text = $"Tóm tắt các bình luận sau:\n{combinedText}" }
                            }
                        }
                    }
                };

                using var httpClient = new HttpClient();
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return BadRequest($"Lỗi từ Gemini: {response.StatusCode} - {responseString}");

                var json = JsonDocument.Parse(responseString);
                var summaryText = json.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { productId, summary = summaryText });
            }

            // Nếu không cần tóm tắt thì trả bình thường
            var pagedList = await _reviewService.GetAllAsync(productId, prm, false);
            Response.AddPaginationHeader(pagedList);
            return Ok(pagedList);
        }



        [HttpGet("check-permission/{productId}")]
        public async Task<ActionResult<bool>> CheckReviewPermission(int productId)
        {
            var userId = ClaimsPrincipleExtensions.GetUserId(User);
            if (userId.IsNullOrEmpty())
            {
                return Ok(false);
            }

            var user = await _userManager.FindByIdAsync(userId);

            var userRoles = await _userManager.GetRolesAsync(user);


            var roleClaims = await _roleManager.GetClaimsAsync(await _roleManager.FindByNameAsync(userRoles[0]));

            var hasReviewClaim = roleClaims.Any(c => c.Type == "Permission" && c.Value == ClaimStore.Review_Reply);
            if (hasReviewClaim)
            {
                return Ok(true);
            }

            bool canReview = await _reviewService.AccceptReviewAsync(productId, userId);

            if (canReview == false)
            {
                return Ok(false);
            }

            return Ok(true);
        }



        [HttpPost]
        public async Task<IActionResult> CreateReview([FromForm] ReviewCreateDto reviewCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = await _reviewService.AddAsync(reviewCreateDto);
            await _hub.Clients.All.SendAsync("add-review", review);
            return CreatedAtAction(nameof(GetReviews), new { productId = review.ProductId }, review);
        }

        // update review & reply
        [HttpPut("{reviewId:int}")]
        public async Task<IActionResult> UpdateReview([FromRoute] int reviewId, [FromBody] ReviewEditDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = await _reviewService.UpdateAsync(dto);
            await _hub.Clients.All.SendAsync("edit-review", review);
            return Ok(review);
        }

        [HttpPost("add-images/{reviewId:int}")]
        public async Task<IActionResult> AddImagesReview([FromRoute] int reviewId, [FromForm] List<IFormFile> imageFiles)
        {
            var review = await _reviewService.AddImageAsync(reviewId, imageFiles);
            await _hub.Clients.All.SendAsync("edit-review", review);
            return Ok(review);
        }

        [HttpDelete("remove-image/{reviewId:int}")]
        public async Task<IActionResult> RemoveImagesReview([FromRoute] int reviewId, int imageId)
        {
            await _reviewService.RemoveImageAsync(reviewId, imageId);
            var review = await _reviewService.GetAsync(r => r.Id == reviewId);
            await _hub.Clients.All.SendAsync("edit-review", review);
            return NoContent();
        }

        [HttpPost("add-video/{reviewId:int}")]
        public async Task<IActionResult> AddVideoReview([FromRoute] int reviewId, [FromForm] IFormFile videoFile)
        {
            var review = await _reviewService.AddVideoAsync(reviewId, videoFile);
            await _hub.Clients.All.SendAsync("edit-review", review);
            return Ok(review);
        }

        [HttpDelete("remove-video/{reviewId:int}")]
        public async Task<IActionResult> RemoveVideoReview([FromRoute] int reviewId)
        {
            await _reviewService.RemoveVideoAsync(reviewId);
            var review = await _reviewService.GetAsync(r => r.Id == reviewId);
            await _hub.Clients.All.SendAsync("edit-review", review);
            return NoContent();
        }

        [HttpPost("add-reply")]
        public async Task<IActionResult> CreateReply([FromBody] ReplyCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reply = await _reviewService.AddReplyAsync(dto);
            await _hub.Clients.All.SendAsync("add-reply", reply);
            return Ok(reply);

        }

        [HttpDelete("{reviewId:int}")]
        public async Task<IActionResult> RemoveReview([FromRoute] int reviewId)
        {
            await _reviewService.RemoveReview(reviewId);
            await _hub.Clients.All.SendAsync("delete-review", reviewId);
            return NoContent();
        }

        [HttpGet("total-rating/{productId}")]
        public async Task<IActionResult> GetTotalRating(int productId)
        {
            return Ok(await _reviewService.CalTotalRating(productId));
        }

    }
}
