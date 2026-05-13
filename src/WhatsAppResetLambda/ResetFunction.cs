using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using WhatsAppResetLambda.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace WhatsAppResetLambda;

/// <summary>
/// Lambda handler for the monthly WhatsApp quota reset.
/// Triggered on the 1st of every month at 00:00 UTC via EventBridge cron.
/// </summary>
public class ResetFunction
{
    private readonly ResetService _resetService;

    public ResetFunction()
    {
        // BSON conventions must match the VitalityHub backend
        BsonSerializer.TryRegisterSerializer(typeof(Guid), new GuidSerializer(BsonType.String));
        BsonSerializer.TryRegisterSerializer(typeof(DateTime), new DateTimeSerializer(BsonType.String));

        var pack = new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true),
            new CamelCaseElementNameConvention(),
        };
        ConventionRegistry.Register("ResetLambdaConventions", pack, _ => true);

        var mongoConnStr = GetRequiredEnv("MONGODB_CONNECTION_STRING");
        var mongoDbName  = Env("MONGODB_DATABASE_NAME", "VitalityHub");

        var db = new MongoClient(mongoConnStr).GetDatabase(mongoDbName);
        _resetService = new ResetService(db);
    }

    public async Task FunctionHandler(object? input, ILambdaContext context)
    {
        context.Logger.LogInformation($"WhatsApp Reset Lambda triggered at {DateTime.UtcNow:u}");
        await _resetService.ResetAllAsync(context.Logger);
        context.Logger.LogInformation("WhatsApp Reset Lambda completed.");
    }

    private static string GetRequiredEnv(string name)
        => Environment.GetEnvironmentVariable(name)
           ?? throw new InvalidOperationException($"Required env var '{name}' is not set.");

    private static string Env(string name, string fallback)
        => Environment.GetEnvironmentVariable(name) ?? fallback;
}
