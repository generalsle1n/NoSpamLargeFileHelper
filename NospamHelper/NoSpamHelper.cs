using NospamHelper.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace NospamHelper
{
    public class NoSpamHelper
    {
        private readonly ILogger<NoSpamHelper> _logger;
        IConfiguration _configuration;
        private  string _argumentDownload = "-command " + '"' + "Get-NspLargeFile -FromDate ((Get-Date).AddMinutes(-20)) | ConvertTo-Json | Out-File xPath" + '"';
        private string _argumentApprove = "-command " + '"' + "Approve-NspLargeFile -Id xIDx" + '"';


        public NoSpamHelper(ILogger<NoSpamHelper> logger, IConfiguration configuration)
        { 
            _logger = logger; 
            _configuration = configuration;
        }
        
        private List<LargeFileEntry> ParseJson(string Json)
        {
            List<LargeFileEntry> Result = new List<LargeFileEntry>();

            try
            {
                Result = JsonSerializer.Deserialize<List<LargeFileEntry>>(Json);
            }catch(Exception ex)
            {
                _logger.LogInformation("No multiple Files found");
            }

            try
            {
                Result.Add(JsonSerializer.Deserialize<LargeFileEntry>(Json));
            }
            catch (Exception ex)
            {
                _logger.LogInformation("No single file");
            }

            Result = Result.Where(item =>
            {
                bool Matched = false;

                if (item.DownloadUrl != null)
                {
                    if (_configuration.GetSection("AllowedMime").Get<string[]>().Contains(item.ContentType))
                    {
                        Matched = true;
                    }
                }

                return Matched;
            }).ToList();

            return Result;
        }

        internal List<LargeFileEntry> GetUnprocessedLargeFiles(string Json = null)
        {
            _logger.LogInformation("Get unprocesd LargeFiles");
            if(Json == null)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                Process powershell = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = _argumentDownload.Replace("xPath", tempPath)
                    }
                };

                powershell.Start();
                powershell.WaitForExit();

                Json = File.ReadAllText(tempPath);
                File.Delete(tempPath);
                _logger.LogInformation("Delete TempFile");
            }
            List<LargeFileEntry> Result = ParseJson(Json);

            _logger.LogInformation($"Found {Result.Count} Processable files");
            return Result;
        }

        internal void ReleaseLargeFile(LargeFileEntry file)
        {
            _logger.LogInformation("Approve unprocesd LargeFiles");
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            Process powershell = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = _argumentApprove.Replace("xIDx", file.Id)
                }
            };

            powershell.Start();
            powershell.WaitForExit();
        }
    }
}
