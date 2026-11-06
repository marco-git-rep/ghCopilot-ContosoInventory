using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Provides operations for managing inventory categories.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CategoryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService"/> class.
    /// </summary>
    public CategoryService(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            return categories.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CategoryResponseDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: false, cancellationToken);
            return category is null ? null : MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CategoryResponseDto> CreateCategoryAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            if (await _categoryRepository.NameExistsAsync(dto.Name, null, cancellationToken))
            {
                throw new InvalidOperationException($"A category with the name '{dto.Name}' already exists.");
            }

            var now = DateTime.UtcNow;
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true,
                CreatedDate = now,
                LastModifiedDate = now
            };

            await _categoryRepository.AddAsync(category, cancellationToken);
            _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId}).", category.Name, category.Id);
            return MapToDto(category);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category '{CategoryName}'.", dto.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CategoryResponseDto?> UpdateCategoryAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (category is null)
            {
                return null;
            }

            if (await _categoryRepository.NameExistsAsync(dto.Name, id, cancellationToken))
            {
                throw new InvalidOperationException($"A category with the name '{dto.Name}' already exists.");
            }

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;
            category.IsActive = dto.IsActive;
            category.LastModifiedDate = DateTime.UtcNow;

            await _categoryRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId}).", category.Name, category.Id);
            return MapToDto(category);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (category is null)
            {
                return false;
            }

            if (await _productRepository.HasProductsInCategoryAsync(id, cancellationToken))
            {
                throw new CategoryInUseException(id);
            }

            await _categoryRepository.DeleteAsync(category, cancellationToken);
            _logger.LogInformation("Category deleted: {CategoryName} (ID: {CategoryId}).", category.Name, category.Id);
            return true;
        }
        catch (DbUpdateException ex)
        {
            try
            {
                if (await _productRepository.HasProductsInCategoryAsync(id, cancellationToken))
                {
                    _logger.LogWarning(
                        ex,
                        "Category {CategoryId} acquired product references before deletion completed.",
                        id);
                    throw new CategoryInUseException(id);
                }
            }
            catch (CategoryInUseException)
            {
                throw;
            }
            catch (Exception checkException)
            {
                _logger.LogError(
                    checkException,
                    "Unable to verify product references after category {CategoryId} deletion failed.",
                    id);
                throw;
            }

            _logger.LogError(ex, "Error deleting category with ID {CategoryId}.", id);
            throw;
        }
        catch (CategoryInUseException)
        {
            _logger.LogWarning("Category {CategoryId} cannot be deleted because products reference it.", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CategoryResponseDto?> ToggleActiveAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (category is null)
            {
                return null;
            }

            category.IsActive = !category.IsActive;
            category.LastModifiedDate = DateTime.UtcNow;

            await _categoryRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Category toggled: {CategoryName} (ID: {CategoryId}) is now {Status}.",
                category.Name,
                category.Id,
                category.IsActive ? "active" : "inactive");
            return MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for category with ID {CategoryId}.", id);
            throw;
        }
    }

    private static CategoryResponseDto MapToDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            CreatedDate = category.CreatedDate,
            LastModifiedDate = category.LastModifiedDate
        };
    }
}
