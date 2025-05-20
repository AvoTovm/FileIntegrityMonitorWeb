using FileIntegrityMonitor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FileIntegrityMonitor.Services
{
    public class FileIntegrityService
    {
        private readonly ApplicationDbContext _context;

        public FileIntegrityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FileRecord>> GetAllFilesAsync()
        {
            return await _context.FileRecords
                .Include(f => f.ChangeLogs)
                .ThenInclude(l => l.User)
                .ToListAsync();
        }

        public async Task<FileRecord?> GetFileByPathAsync(string filePath)
        {
            return await _context.FileRecords
                .Include(f => f.ChangeLogs)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(f => f.FilePath == filePath);
        }

        public async Task<FileRecord?> GetFileByIdAsync(int id)
        {
            return await _context.FileRecords
                .Include(f => f.ChangeLogs)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task MonitorDirectoryAsync(string directory, ClaimsPrincipal currentUser)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
            }

            var userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID not found in claims.");
            }

            // Get all current file records
            var existingRecords = await _context.FileRecords.ToDictionaryAsync(f => f.FilePath, f => f);

            // Process all files in the directory
            foreach (var filePath in Directory.GetFiles(directory))
            {
                try
                {
                    string hash = SHA256Helper.ComputeHash(filePath);

                    if (existingRecords.TryGetValue(filePath, out var existingFile))
                    {
                        // File exists in database
                        if (existingFile.Sha256Hash != hash)
                        {
                            // File modified
                            await LogFileChangeAsync(existingFile, hash, userId, ChangeType.Modified);
                            existingFile.Sha256Hash = hash;
                            existingFile.LastModified = DateTime.Now;
                        }
                    }
                    else
                    {
                        // New file
                        var newFile = new FileRecord
                        {
                            FilePath = filePath,
                            Sha256Hash = hash,
                            LastModified = DateTime.Now
                        };
                        _context.FileRecords.Add(newFile);
                        await _context.SaveChangesAsync();

                        await LogFileChangeAsync(newFile, hash, userId, ChangeType.New);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }

            // Check for deleted files
            foreach (var record in existingRecords.Values)
            {
                if (!File.Exists(record.FilePath))
                {
                    await LogFileChangeAsync(record, string.Empty, userId, ChangeType.Deleted);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task LogFileChangeAsync(FileRecord fileRecord, string newHash, string userId, ChangeType changeType)
        {
            var changelog = new FileChangeLog
            {
                FileRecordId = fileRecord.Id,
                UserId = userId,
                ChangeTimestamp = DateTime.Now,
                PreviousHash = fileRecord.Sha256Hash,
                NewHash = newHash,
                ChangeType = changeType
            };

            _context.FileChangeLogs.Add(changelog);
            await _context.SaveChangesAsync();
        }
    }
}