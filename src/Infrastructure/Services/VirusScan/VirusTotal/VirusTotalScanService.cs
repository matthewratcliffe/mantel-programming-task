using Application.Interfaces;
using Domain;
 
namespace Infrastructure.Services.VirusScan.VirusTotal;
 
 public class VirusTotalScanService(VirusTotalClient virusTotalClient) : IVirusScan
 {
     public virtual Task<VirusScanResult> Scan(byte[] fileBytes)
     {
         return virusTotalClient.Scan(fileBytes);
     }
 }