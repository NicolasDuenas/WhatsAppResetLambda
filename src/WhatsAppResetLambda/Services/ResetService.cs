using Amazon.Lambda.Core;
using MongoDB.Driver;
using WhatsAppResetLambda.Models;

namespace WhatsAppResetLambda.Services;

/// <summary>
/// Resets WhatsAppMessagesRemaining = 400 for every non-Receptionist doctor
/// belonging to a company with an active Pro plan.
/// Triggered on the 1st of each month via EventBridge cron.
/// </summary>
public class ResetService
{
    private const int WA_MONTHLY_LIMIT = 400;
    private const string PRO_PLAN = "Pro";
    private const string RECEPTIONIST_ROLE = "Receptionist";

    private readonly IMongoCollection<Company> _companies;
    private readonly IMongoCollection<Doctor> _doctors;

    public ResetService(IMongoDatabase db)
    {
        _companies = db.GetCollection<Company>("Company");
        _doctors   = db.GetCollection<Doctor>("Doctor");
    }

    public async Task ResetAllAsync(ILambdaLogger logger)
    {
        var now = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Query only by Plan (string match). DueTo is stored as a string in MongoDB so we
        // cannot reliably use a BSON DateTime comparison in the query — filter it in memory.
        var proFilter       = Builders<Company>.Filter.Eq(c => c.Plan, PRO_PLAN);
        var allProCompanies = await _companies.Find(proFilter).ToListAsync();

        // Keep only companies whose plan covers the current month
        var proCompanies = allProCompanies
            .Where(c => c.DueTo >= startOfCurrentMonth)
            .ToList();

        logger.LogInformation(
            $"WhatsApp reset: found {proCompanies.Count} active Pro company/ies (month >= {startOfCurrentMonth:u}).");

        // Single UpdateManyAsync across all qualifying companies — one round-trip regardless of scale
        var companyIds    = proCompanies.Select(c => c.Id).ToList();
        var doctorFilter  = Builders<Doctor>.Filter.And(
            Builders<Doctor>.Filter.In(d => d.CompanyId, companyIds),
            Builders<Doctor>.Filter.Ne(d => d.Role, RECEPTIONIST_ROLE));
        var doctorUpdate  = Builders<Doctor>.Update
            .Set(d => d.WhatsAppMessagesRemaining, WA_MONTHLY_LIMIT);

        var result = await _doctors.UpdateManyAsync(doctorFilter, doctorUpdate);

        logger.LogInformation($"WhatsApp reset complete. {result.ModifiedCount} doctor(s) updated.");
    }
}
