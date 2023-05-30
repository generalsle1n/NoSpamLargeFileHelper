using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;
using VirusTotalNet.Results;

namespace NospamHelper
{
    public class VirustotalHelper
    {
        private const string _api = "86cab9f34bcd9542f07e7ec3b66a9933ceea130074b5e4c271de1df31a19c0ed";
        VirusTotal _virusTotal = new VirusTotal(_api);
        private readonly ILogger<VirustotalHelper> _logger;

        public VirustotalHelper(ILogger<VirustotalHelper> logger)
        { 
            _logger = logger; 
        }


        internal async Task<ScanResult> UploadFile(byte[] Data, string FileName)
        {
            ScanResult Result = await _virusTotal.ScanFileAsync(Data, FileName);
            _logger.LogInformation($"Upload File {FileName}");
            return Result;
        }

        internal async Task<FileReport> CheckIfFileIsClean(byte[] Data)
        {
            FileReport Result = await _virusTotal.GetFileReportAsync(Data);
            _logger.LogInformation($"Try to get File Report");
            return Result;
        }

        internal async Task<bool> ProccessFile(byte[] Data, string FileName)
        {
            FileReport report = await CheckIfFileIsClean(Data);

            bool Result = false;

            if((report.ResponseCode == FileReportResponseCode.Present) && report.Positives == 0)
            {
                Result = true;
            }else if(report.ResponseCode == FileReportResponseCode.NotPresent)
            {
                await UploadFile(Data, FileName);
                int loopcont = 0;
                int timecount = 20;
                while(loopcont <= 5)
                {
                    FileReport reportScanned = await CheckIfFileIsClean(Data);
                    if((report.ResponseCode == FileReportResponseCode.Present) && report.Positives == 0){
                        Result = true;
                    }
                    loopcont++;
                    await Task.Delay(timecount * 1000);
                    timecount += timecount;
                }
            }
            return Result;
        }
    }
}
