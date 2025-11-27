using System;
using System.Security.Claims;
using FileSystem_Honeywell.Model;
using FileSystem_Honeywell.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FileSystem_Honeywell.Controllers
{
    [ApiController]
    [Route("files")]
    [Authorize]
    public class FileController : ControllerBase
	{
        private readonly IFileStorageService _fileService;
        private readonly FileSettings _fileSettings;
        //private readonly FSDBContext _dbContext;
        private readonly IFileMetadataStore _metadataStore;
        public FileController(IFileStorageService fileService, IOptions<FileSettings> fileSettings, IFileMetadataStore metadataStore)
        {
            _fileService = fileService;
            _fileSettings = fileSettings.Value;
            _metadataStore = metadataStore;
        }


        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("");

                if (file.Length > _fileSettings.MaxFileSizeBytes)
                    return BadRequest("");

                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(extension) || !_fileSettings.AllowedExtensions.Contains(extension))
                    return BadRequest("File type not allowed.");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await using var stream = file.OpenReadStream();
                var storedFileName = await _fileService.SaveFileAsync(stream, extension, cancellationToken);

                var record = new FileRecord
                {
                    UserId = userId,
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    Size = file.Length
                };

                //_dbContext.FileRecords.Add(record);
                //await _dbContext.SaveChangesAsync(cancellationToken);

                record = await _metadataStore.AddAsync(record, cancellationToken);

                return CreatedAtAction(nameof(Download), new { id = record.Id }, new
                {
                    record.Id,
                    record.OriginalFileName,
                    record.Size,
                    record.CreatedAt
                });
            }
            catch(Exception ex) {
                throw;
            }

        }

        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                //var record = await _dbContext.FileRecords
                //  .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
                var record = await _metadataStore.GetAsync(id, cancellationToken);

                if (record == null)
                return NotFound();

            if (record.UserId != userId)
                return Forbid(); // owner-only access

            var physicalPath = _fileService.GetPhysicalPath(record.StoredFileName);
            if (!System.IO.File.Exists(physicalPath))
                return NotFound("File not found on server.");

            // Secure download; File() handles stream/physical path
            return PhysicalFile(
                physicalPath,
                record.ContentType,
                fileDownloadName: record.OriginalFileName
            );
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListUserFiles(CancellationToken cancellationToken)
        {
            try { 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var files = await _metadataStore.GetByUserAsync(userId, cancellationToken);
                //var files = await _dbContext.FileRecords
                //.Where(f => f.UserId == userId)
                //.OrderByDescending(f => f.CreatedAt)
                //.Select(f => new
                //{
                  //  f.Id,
                    //f.OriginalFileName,
                    //f.Size,
                    //f.ContentType,
                    //f.CreatedAt
                //})
                //.ToListAsync(cancellationToken);

            return Ok(files);
        }
            catch(Exception ex) {
                throw;
            }
}

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try { 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                //var record = await _dbContext.FileRecords
                //  .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
                var record = await _metadataStore.GetAsync(id, cancellationToken);

                if (record == null)
                return NotFound();

            if (record.UserId != userId)
                return Forbid();


                var deleted = await _metadataStore.DeleteAsync(id, cancellationToken);
                if (!deleted)
                    return NotFound();
                //_dbContext.FileRecords.Remove(record);
                //await _dbContext.SaveChangesAsync(cancellationToken);

                await _fileService.DeleteFileAsync(record.StoredFileName, cancellationToken);

            return NoContent();
        }
            catch(Exception ex) {
                throw;
            }
}

    }
}

