﻿using API.Controllers;
using API.Extensions;
using API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.DTOs.Products;
using Shop.Application.Services.Abstracts;
using Shop.Application.Services.Implementations;
using Shop.Application.Ultilities;
using Shop.Domain.Exceptions;

namespace api.Controllers
{
    public class ProductController : BaseApiController
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts([FromQuery] ProductParams productParams, bool tracked)
        {
            var pagedList = await _productService.GetAllAsync(productParams, tracked);

            Response.AddPaginationHeader(pagedList);
            return Ok(pagedList);
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
        {
            var products = await _productService.GetAllAsync(false);
            return Ok(products);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetProductById(int id)
        {
            var product = await _productService.GetAsync(p => p.Id == id);
            return Ok(product);
        }
        [HttpGet("categoryId/{categoryId:int}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategoryId(int categoryId)
        {
            var products = await _productService.GetProductsByCategoryId(categoryId);
            return Ok(products);
        }

        [HttpPost("Add")]
        [Authorize(Policy = ClaimStore.Product_Create)]
        public async Task<IActionResult> AddProduct([FromForm] ProductAdd productAdd)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.AddAsync(productAdd);
            return CreatedAtAction(nameof(GetAllProducts), new { id = product.Id }, product);
        }

        [HttpPut("Update")]
        [Authorize(Policy = ClaimStore.Product_Edit)]
        public async Task<IActionResult> UpdateProduct([FromForm] ProductUpdate productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var product = await _productService.UpdateAsync(productDto);
            return CreatedAtAction(nameof(GetAllProducts), new { id = product.Id }, product);
        }

        [HttpDelete("Delete")]
        [Authorize(Policy = ClaimStore.Product_Delete)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            await _productService.DeleteAsync(c => c.Id == id);
            return NoContent();
        }
        [HttpPost("add-images/{productId:int}")]
        [Authorize(Policy = ClaimStore.Product_Edit)]
        public async Task<IActionResult> AddImagesProduct([FromRoute] int productId, [FromForm] IFormFile imageFile)
        {
            var product = await _productService.AddImageAsync(productId, imageFile);
            return Ok(product);
        }

        [HttpDelete("remove-image/{productId:int}")]
        [Authorize(Policy = ClaimStore.Product_Edit)]
        public async Task<IActionResult> RemoveImagesProduct([FromRoute] int productId, int imageId)
        {
            await _productService.RemoveImageAsync(productId, imageId);
            var product = await _productService.GetAsync(r => r.Id == productId);
            return NoContent();
        }

        [HttpPut("revert-quantity/{orderId}")]
        public async Task<IActionResult> RevertQuantityProduct(int orderId)
        {
            if (orderId < 1)
            {
                throw new BadRequestException("order id phải lớn hơn 0");
            }

            await _productService.RevertQuantityProductAsync(orderId);
            return NoContent();
        }


    }
}
