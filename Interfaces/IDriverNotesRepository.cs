using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface IDriverNotesRepository
    {
        // CREATE
        Task<DriverNote> Create(DriverNote note);

        // READ
        Task<List<DriverNote>> GetAll();
        Task<DriverNote?> GetById(string id);
        Task<List<DriverNote>> GetByDriverId(string driverId);
        Task<List<DriverNote>> GetWithIssuesFaced();

        // UPDATE
        Task<DriverNote?> Update(string id, DriverNote updatedNote);

        // DELETE
        Task<DriverNote?> Delete(string id);

        // APPROVAL
        Task<DriverNote?> Approve(string id);
        Task<List<DriverNote>> BulkApprove(List<string> ids);
    }
}
