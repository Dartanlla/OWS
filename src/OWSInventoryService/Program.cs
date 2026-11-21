using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using OWSInventoryService;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var options = builder.Configuration.GetSection("Inventory").Get<InventoryOptions>() ?? new();
if (options.FirstBucket < 0 || options.LastBucket >= InventoryOptions.BucketCount || options.LastBucket < options.FirstBucket
    || options.FlushMilliseconds is < 1 or > 1000 || options.ConfirmationMilliseconds is < 0 or > 5000
    || options.MaxPendingPerBucket is < 1 or > 100000 || options.IdleSeconds is < 5 or > 86400
    || options.RetrySeconds is < 60 or > 3600)
    throw new InvalidOperationException("Invalid inventory timing or partition configuration.");

var credentials = options.Credentials.Select(entry =>
{
    if (!SafeId(entry.Name) || entry.CustomerGuid == Guid.Empty || entry.Sha256 is null || entry.Sha256.Length != 64)
        throw new InvalidOperationException("Inventory credentials require a name, tenant UUID, and one SHA-256 token digest.");
    return (Principal: new InventoryPrincipal(entry.Name, entry.CustomerGuid, entry.Manage), Digest: Convert.FromHexString(entry.Sha256));
}).ToArray();
if (credentials.Select(entry => Convert.ToHexString(entry.Digest)).Distinct().Count() != credentials.Length)
    throw new InvalidOperationException("Inventory credentials must use unique tokens.");

string Connection(string key) => builder.Configuration.GetConnectionString(key)
    ?? throw new InvalidOperationException($"ConnectionStrings:{key} is required.");

builder.Services.ConfigureHttpJsonOptions(config =>
{
    config.SerializerOptions.DefaultIgnoreCondition = InventoryJson.Options.DefaultIgnoreCondition;
    foreach (var converter in InventoryJson.Options.Converters)
        config.SerializerOptions.Converters.Add(converter);
});
builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 16 * 1024);
builder.Services.AddSingleton(options);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(Connection("InventoryPostgres")));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration = ConfigurationOptions.Parse(Connection("InventoryRedis"));
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<InventoryDatabase>();
builder.Services.AddSingleton<InventoryRedis>();
builder.Services.AddSingleton<InventoryService>();
if (options.RunWorker)
    builder.Services.AddHostedService<InventorySyncWorker>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception error) when (error is RedisException or NpgsqlException or TimeoutException)
    {
        app.Logger.LogWarning("Inventory dependency request failed ({ErrorType}).", error.GetType().Name);
        if (!context.Response.HasStarted)
            await InventoryService.Error("unavailable", "inventory_unavailable").ExecuteAsync(context);
    }
});
app.MapGet("/health/live", () => Results.Ok());
app.MapGet("/health/ready", async (InventoryDatabase database, InventoryRedis redis, CancellationToken ct) =>
    await database.Ready(ct) && await redis.Ping() >= TimeSpan.Zero ? Results.Ok() : Results.StatusCode(503));

var api = app.MapGroup("/v1");
api.AddEndpointFilter(async (context, next) =>
{
    var http = context.HttpContext;
    http.Response.Headers.CacheControl = "no-store";
    var authorization = http.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || authorization.Length is < 39 or > 1024)
        return Results.Unauthorized();

    var digest = SHA256.HashData(Encoding.UTF8.GetBytes(authorization[7..]));
    InventoryPrincipal? principal = null;
    foreach (var credential in credentials)
    {
        if (CryptographicOperations.FixedTimeEquals(digest, credential.Digest))
            principal = credential.Principal;
    }
    if (principal is null)
        return Results.Unauthorized();

    if (http.Request.Headers.TryGetValue("X-CustomerGUID", out var scope)
        && (!Guid.TryParse(scope.ToString(), out var requested) || requested != principal.CustomerGuid))
        return InventoryService.Error("forbidden", "wrong_tenant");

    http.Items[typeof(InventoryPrincipal)] = principal;
    return await next(context);
});

api.MapPut("/definitions/{definitionId}", async (HttpContext http, string definitionId, ItemDefinition definition,
    InventoryDatabase database, CancellationToken ct) =>
{
    var principal = Principal(http);
    if (!principal.Manage)
        return InventoryService.Error("forbidden", "manage_required");
    if (!SafeId(definitionId) || definitionId != definition.DefinitionId || string.IsNullOrWhiteSpace(definition.Name)
        || definition.Name.Length > 160 || definition.MaxStack is < 1 or > 1000000
        || definition.Metadata is { } json && (json.ValueKind != JsonValueKind.Object || json.GetRawText().Length > 4096))
        return InventoryService.Error("invalid", "invalid_definition");
    return await database.PutDefinition(principal.CustomerGuid, definition, ct)
        ? Results.Ok(definition) : InventoryService.Error("conflict", "definition_is_immutable");
});

api.MapGet("/definitions/{definitionId}", async (HttpContext http, string definitionId, InventoryService inventory,
    CancellationToken ct) =>
    !SafeId(definitionId) ? InventoryService.Error("invalid", "invalid_definition")
    : await inventory.GetDefinition(Principal(http).CustomerGuid, definitionId, ct) is { } definition
        ? Results.Ok(definition) : InventoryService.Error("missing", "definition_not_found"));

api.MapPut("/container-templates/{containerId}", async (HttpContext http, string containerId, ContainerTemplate template,
    InventoryDatabase database, CancellationToken ct) =>
{
    var principal = Principal(http);
    if (!principal.Manage)
        return InventoryService.Error("forbidden", "manage_required");
    if (!SafeId(containerId) || containerId == "_loot" || containerId != template.ContainerId
        || template.Capacity is < 1 or > 1000000 || template.Slotted && template.Capacity is null
        || template.GridWidth is { } width && (!template.Slotted || width < 1 || width > template.Capacity))
        return InventoryService.Error("invalid", "invalid_container_template");
    return await database.PutTemplate(principal.CustomerGuid, template, ct)
        ? Results.Ok(template) : InventoryService.Error("conflict", "template_is_immutable_or_limit_reached");
});

var characters = api.MapGroup("/characters/{characterId:int:min(1)}/inventory");
characters.MapGet("/authority", async (HttpContext http, int characterId, InventoryDatabase database, CancellationToken ct) =>
{
    if (!Principal(http).Manage)
        return InventoryService.Error("forbidden", "manage_required");
    return await database.State(Character(http, characterId), ct) is { } state
        ? Results.Ok(state) : InventoryService.Error("missing", "inventory_not_provisioned");
});

characters.MapGet("/containers", (HttpContext http, int characterId, InventoryService service, CancellationToken ct) =>
    service.Containers(Character(http, characterId), Principal(http), Generation(http), ct));

characters.MapGet("/containers/{containerId}/items", (HttpContext http, int characterId, string containerId, int? limit,
    string? cursor, InventoryService service, CancellationToken ct) =>
    !SafeId(containerId) || limit is < 1 or > 200 ? Task.FromResult(InventoryService.Error("invalid", "invalid_page"))
        : service.Page(Character(http, characterId), Principal(http), Generation(http), containerId, limit ?? 50, cursor, ct));

characters.MapPost("/commands", async (HttpContext http, int characterId, InventoryCommand command,
    InventoryService service, CancellationToken ct) =>
{
    var validation = Validate(command);
    var principal = Principal(http);
    if (validation is not null)
        return InventoryService.Error("invalid", validation);
    if (!principal.Manage && command.Operation is InventoryOperation.SetAuthority or InventoryOperation.Close)
        return InventoryService.Error("forbidden", "manage_required");
    if (command.Operation == InventoryOperation.SetAuthority && command.NewAuthority is not null
        && !credentials.Any(entry => entry.Principal.CustomerGuid == principal.CustomerGuid && entry.Principal.Name == command.NewAuthority))
        return InventoryService.Error("invalid", "unknown_authority");

    var result = await service.Submit(Character(http, characterId), principal, command, ct);
    return result.Status switch
    {
        "committed" => Results.Ok(result.Event!.Result()),
        "pending" => Results.Accepted($"{http.Request.Path}/{command.CommandId:D}",
            new { status = "pending", commandId = command.CommandId }),
        _ => InventoryService.Error(result.Status, result.Error ?? "inventory_unavailable")
    };
});

characters.MapGet("/commands/{commandId:guid}", async (HttpContext http, int characterId, Guid commandId,
    InventoryDatabase database, InventoryRedis redis, CancellationToken ct) =>
{
    var owner = Character(http, characterId);
    var principal = Principal(http);
    var state = await database.State(owner, ct);
    var rejected = InventoryService.RejectRead(state, principal, Generation(http));
    if (rejected is not null && !(principal.Manage && state is { Closed: true }))
        return rejected;
    var receipt = await database.Receipt(owner, commandId, ct);
    if (receipt is not null)
        return Results.Ok(receipt.Result());
    if (await redis.HasPendingReceipt(owner, commandId))
        return Results.Accepted(http.Request.Path, new { status = "pending", commandId });
    return InventoryService.Error("missing", "commit_not_found");
});

characters.MapGet("/changes", async (HttpContext http, int characterId, long afterRevision, int? limit,
    InventoryDatabase database, CancellationToken ct) =>
{
    if (afterRevision is < 0 or >= 10000000000000 || limit is < 1 or > 200)
        return InventoryService.Error("invalid", "invalid_change_cursor");
    var history = await database.Changes(Character(http, characterId), afterRevision, limit ?? 50, ct);
    if (InventoryService.RejectRead(history.State, Principal(http), Generation(http)) is { } rejected)
        return rejected;
    var latest = history.State!.Revision;
    if (afterRevision > latest || history.Events.Length == 0 && afterRevision < latest ||
        history.Events.Where((item, index) => item.Revision != afterRevision + index + 1).Any())
        return InventoryService.Error("gone", "refresh_inventory");
    return Results.Ok(new
    {
        revision = latest,
        changes = history.Events.Select(item => new
        {
            item.Revision, items = (item.Upserts ?? []).Where(row => row.ContainerId != "_loot"),
            removedItemIds = item.Deletes ?? [], containers = (item.Containers ?? []).Where(box => !box.Hidden)
        })
    });
});

characters.MapGet("/loot", async (HttpContext http, int characterId, Guid? afterRewardId, int? limit,
    InventoryDatabase database, CancellationToken ct) =>
{
    if (limit is < 1 or > 200)
        return InventoryService.Error("invalid", "invalid_page");
    var owner = Character(http, characterId);
    if (InventoryService.RejectRead(await database.State(owner, ct), Principal(http), Generation(http)) is { } rejected)
        return rejected;
    var size = limit ?? 50;
    var items = await database.StagedLoot(owner, afterRewardId, size + 1, ct);
    return Results.Ok(new { items = items.Take(size), nextRewardId = items.Length > size ? items[size - 1].RewardId : null });
});

characters.MapGet("/loot/{rewardId:guid}", async (HttpContext http, int characterId, Guid rewardId,
    InventoryDatabase database, CancellationToken ct) =>
{
    var owner = Character(http, characterId);
    if (InventoryService.RejectRead(await database.State(owner, ct), Principal(http), Generation(http)) is { } rejected)
        return rejected;
    return await database.Loot(owner, rewardId, ct) is { } receipt
        ? Results.Ok(receipt) : InventoryService.Error("missing", "loot_not_found");
});

app.Run();

static bool SafeId(string? value) => value is not null && Regex.IsMatch(value, @"\A[A-Za-z0-9_.-]{1,80}\z",
    RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
static InventoryPrincipal Principal(HttpContext http) => (InventoryPrincipal)http.Items[typeof(InventoryPrincipal)]!;
static Owner Character(HttpContext http, int characterId) => new(Principal(http).CustomerGuid, characterId);
static Guid? Generation(HttpContext http) =>
    Guid.TryParse(http.Request.Headers["X-Inventory-Authority"], out var generation) && generation != Guid.Empty ? generation : null;

static string? Validate(InventoryCommand c)
{
    if (c.CommandId == Guid.Empty || c.IssuedAt == default || c.SlotNumber is < 0 or >= 1000000
        || c.Quantity is < 1 or > 1000000 || c.Condition is < 0 or > 100 || c.UsesLeft is < 0
        || c.AuthorityGeneration == Guid.Empty || c.NewAuthorityGeneration == Guid.Empty || c.RewardId == Guid.Empty
        || c.ItemInstanceId == Guid.Empty || c.TargetItemInstanceId == Guid.Empty
        || c.ContainerId is not null && !SafeId(c.ContainerId) || c.DefinitionId is not null && !SafeId(c.DefinitionId))
        return "invalid_command";

    if (c.Operation is not (InventoryOperation.SetAuthority or InventoryOperation.Close) && c.AuthorityGeneration is null)
        return "authority_required";

    var valid = c.Operation switch
    {
        InventoryOperation.SetAuthority => c.NewAuthority is null && c.NewAuthorityGeneration is null
            ? c.AuthorityGeneration.HasValue
            : SafeId(c.NewAuthority) && c.NewAuthorityGeneration.HasValue && c.NewAuthorityGeneration != c.AuthorityGeneration,
        InventoryOperation.Close => true,
        InventoryOperation.Grant => c.ContainerId is not null && c.DefinitionId is not null && c.Quantity.HasValue && c.RewardId is null,
        InventoryOperation.StageLoot => c.RewardId.HasValue && c.DefinitionId is not null && c.Quantity.HasValue && c.SlotNumber is null,
        InventoryOperation.ClaimLoot => c.RewardId.HasValue && c.ItemInstanceId.HasValue && c.ContainerId is not null,
        InventoryOperation.Remove => HasSource(c) && c.Quantity.HasValue,
        InventoryOperation.Move => HasSource(c) && c.ContainerId is not null,
        InventoryOperation.Split => HasSource(c) && c.ContainerId is not null && c.Quantity.HasValue,
        InventoryOperation.Merge => HasSource(c) && HasTarget(c) && c.Quantity.HasValue,
        InventoryOperation.Swap => HasSource(c) && HasTarget(c),
        _ => false
    };
    return valid ? null : "missing_command_fields";
}

static bool HasSource(InventoryCommand c) => c.ItemInstanceId.HasValue && c.ExpectedItemRevision is > 0 and < 10000000000000;
static bool HasTarget(InventoryCommand c) => c.TargetItemInstanceId.HasValue && c.ExpectedTargetRevision is > 0 and < 10000000000000;

