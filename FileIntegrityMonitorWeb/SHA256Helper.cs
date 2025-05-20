using System;
using System.IO;
using System.Security.Cryptography;

namespace FileIntegrityMonitor
{
    public static class SHA256Helper
    {
        public static string ComputeHash(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing hash for {filePath}: {ex.Message}");
                throw;
            }
        }
    }
}