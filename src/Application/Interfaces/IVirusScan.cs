using Domain;

namespace Application.Interfaces;

public interface IVirusScan
{
    Task<VirusScanResult> Scan(byte[] fileBytes);
}