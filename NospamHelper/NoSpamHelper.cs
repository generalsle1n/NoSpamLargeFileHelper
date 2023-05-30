using NospamHelper.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace NospamHelper
{
    public class NoSpamHelper
    {
        private readonly ILogger<NoSpamHelper> _logger;
        private  string _argumentDownload = "-command " + '"' + "Get-NspLargeFile -FromDate ((Get-Date).AddHours(-10)) | ConvertTo-Json | Out-File xPath" + '"';
        private string _argumentApprove = "-command " + '"' + "Approve-NspLargeFile -Id xIDx" + '"';


        public NoSpamHelper(ILogger<NoSpamHelper> logger)
        { 
            _logger = logger; 
        }
        
        internal List<LargeFileEntry> GetUnprocessedLargeFiles()
        {
            _logger.LogInformation("Get unprocesd LargeFiles");
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

            List<LargeFileEntry> Result = JsonSerializer.Deserialize<List<LargeFileEntry>>(File.ReadAllText(tempPath));
            File.Delete(tempPath);
            _logger.LogInformation("Delete TempFile");
            Result = Result.Where(item => item.DownloadUrl != null).ToList();
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
