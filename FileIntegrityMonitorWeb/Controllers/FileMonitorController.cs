using FileIntegrityMonitor.Models;
using FileIntegrityMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FileIntegrityMonitor.Controllers
{
    [Authorize]
    public class FileMonitorController : Controller
    {
        private readonly FileIntegrityService _fileIntegrityService;

        public FileMonitorController(FileIntegrityService fileIntegrityService)
        {
            _fileIntegrityService = fileIntegrityService;
        }

        public async Task<IActionResult> Index()
        {
            var files = await _fileIntegrityService.GetAllFilesAsync();
            return View(files);
        }

        [HttpGet]
        public IActionResult MonitorDirectory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MonitorDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                ModelState.AddModelError("", "Directory path cannot be empty");
                return View();
            }

            try
            {
                await _fileIntegrityService.MonitorDirectoryAsync(directoryPath, User);
                TempData["Success"] = "Directory monitored successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                ModelState.AddModelError("", $"Directory not found: {directoryPath}");
                return View();
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Error monitoring directory: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> FileDetails(int id)
        {
            var file = await _fileIntegrityService.GetFileByIdAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }
    }
}