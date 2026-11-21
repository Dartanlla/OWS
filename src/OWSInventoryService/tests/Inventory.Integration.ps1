#Requires -Version 7.4
param([string]$PostgresImage = 'postgres:14.2-alpine3.15', [switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$project = Join-Path $repo 'src/OWSInventoryService'
$testId = 'inventory-it-' + [guid]::NewGuid().ToString('N').Substring(0, 12)
$pg = "$testId-pg"
$redis = "$testId-redis"
$logDir = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) $testId)
$tenant = 'eeeeeeee-0000-0000-0000-000000000001'
$other = 'eeeeeeee-0000-0000-0000-000000000002'
$character = 21414
$pgPassword = [guid]::NewGuid().ToString('N')
$adminToken = [guid]::NewGuid().ToString('N')
$regionToken = [guid]::NewGuid().ToString('N')
$nextToken = [guid]::NewGuid().ToString('N')
$otherToken = [guid]::NewGuid().ToString('N')
$generation = [guid]::NewGuid()
$processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$containers = [System.Collections.Generic.List[string]]::new()
$script:assertions = 0

function Assert($value, [string]$message) {
    if (-not $value) { throw $message }
    $script:assertions++
}

function Docker-Call {
    $output = & docker @args
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed with exit $LASTEXITCODE." }
    $output
}

function Sql([string]$query) {
    $output = $query | & docker exec -i $pg psql -XqAt -v ON_ERROR_STOP=1 -U postgres -d openworldserver
    if ($LASTEXITCODE -ne 0) { throw "Fixture SQL failed with exit $LASTEXITCODE." }
    $output
}

function Wait-For([scriptblock]$condition, [string]$message) {
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        if (& $condition) { return }
        Start-Sleep -Milliseconds 125
    }
    throw $message
}

function Free-Port {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $port
}

function Start-Inventory([bool]$worker, [int]$port, [string]$name, [int]$confirmationMilliseconds = 400) {
    $environment = @{
        ASPNETCORE_URLS = "http://127.0.0.1:$port"
        ConnectionStrings__InventoryPostgres = "Host=127.0.0.1;Port=$pgPort;Database=openworldserver;Username=postgres;Password=$pgPassword;Timeout=3;Command Timeout=5"
        ConnectionStrings__InventoryRedis = "127.0.0.1:$redisPort,abortConnect=false"
        Inventory__RunWorker = "$worker"
        Inventory__ConfirmationMilliseconds = "$confirmationMilliseconds"
        Logging__LogLevel__Default = 'Warning'
    }
    $index = 0
    foreach ($entry in @(
        @{ Name = 'pilot-admin'; Customer = $tenant; Token = $adminToken; Manage = 'true' }
        @{ Name = 'pilot-region'; Customer = $tenant; Token = $regionToken; Manage = 'false' }
        @{ Name = 'pilot-next'; Customer = $tenant; Token = $nextToken; Manage = 'false' }
        @{ Name = 'other-admin'; Customer = $other; Token = $otherToken; Manage = 'true' }
    )) {
        $prefix = 'Inventory__Credentials__' + $index + '__'
        $environment[$prefix + 'Name'] = $entry.Name
        $environment[$prefix + 'CustomerGuid'] = $entry.Customer
        $environment[$prefix + 'Manage'] = $entry.Manage
        $environment[$prefix + 'Sha256'] = [Convert]::ToHexString(
            [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($entry.Token)))
        $index++
    }
    $processSettings = @{
        FilePath = 'dotnet'
        ArgumentList = '"' + (Join-Path $project 'bin/Release/net10.0/OWSInventoryService.dll') + '"'
        Environment = $environment
        RedirectStandardOutput = (Join-Path $logDir "$name.out")
        RedirectStandardError = (Join-Path $logDir "$name.err")
        PassThru = $true
    }
    if ($IsWindows) { $processSettings.WindowStyle = 'Hidden' }
    $process = Start-Process @processSettings
    $processes.Add($process)
    $process
}

function Stop-Inventory([System.Diagnostics.Process]$process) {
    if (-not $process.HasExited) {
        Stop-Process -InputObject $process -Force
        $process.WaitForExit()
    }
}

function Request([string]$method, [string]$path, [string]$token, $body, [guid]$lease = [guid]::Empty) {
    $headers = @{ Authorization = "Bearer $token" }
    if ($lease -ne [guid]::Empty) { $headers['X-Inventory-Authority'] = $lease.ToString() }
    $request = @{
        Uri = "$base$path"; Method = $method; Headers = $headers; SkipHttpErrorCheck = $true; TimeoutSec = 8
    }
    if ($null -ne $body) {
        $request.Body = $body | ConvertTo-Json -Depth 10 -Compress
        $request.ContentType = 'application/json'
    }
    $response = Invoke-WebRequest @request
    $json = if ($response.Content) { $response.Content | ConvertFrom-Json }
    @{ Status = [int]$response.StatusCode; Json = $json }
}

function New-Command([string]$operation, [hashtable]$fields) {
    @{ commandId = [guid]::NewGuid(); operation = $operation; issuedAt = [datetimeoffset]::UtcNow.ToString('o') } + $fields
}

function Submit($command, [string]$token, [string]$path = $ownerPath) {
    Request Post "$path/commands" $token $command
}

function Commit($command, [string]$token, [guid]$lease = [guid]::Empty, [string]$path = $ownerPath) {
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        $response = Submit $command $token $path
        if ($response.Status -eq 200) { return $response.Json }
        if ($response.Status -eq 202) {
            $status = Request Get "$path/commands/$($command.commandId)" $token $null $lease
            if ($status.Status -eq 200) { return $status.Json }
        } elseif ($response.Status -ne 503) {
            throw "$($command.operation) failed with $($response.Status) $($response.Json | ConvertTo-Json -Compress)"
        }
        Start-Sleep -Milliseconds 100
    }
    throw "$($command.operation) did not commit."
}

try {
    if (-not $NoBuild) {
        & dotnet build (Join-Path $project 'OWSInventoryService.csproj') -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Inventory build failed.' }
    }
    $schema = Join-Path $repo 'src/.docker/postgres'
    $setupMount = "$schema/setup.sql" + ':/docker-entrypoint-initdb.d/setup.sql:ro'
    $inventoryMount = "$schema/z-inventory.sql" + ':/docker-entrypoint-initdb.d/z-inventory.sql:ro'
    Docker-Call run -d --rm --name $pg -e "POSTGRES_PASSWORD=$pgPassword" -p 127.0.0.1::5432 -v $setupMount -v $inventoryMount $PostgresImage | Out-Null
    $containers.Add($pg)
    Docker-Call run -d --rm --name $redis -p 127.0.0.1::6379 redis:7-alpine redis-server --appendonly yes --maxmemory-policy noeviction | Out-Null
    $containers.Add($redis)
    $pgPort = (Docker-Call port $pg 5432/tcp).Split(':')[-1]
    $redisPort = (Docker-Call port $redis 6379/tcp).Split(':')[-1]
    Wait-For {
        $ready = & docker exec -e "PGPASSWORD=$pgPassword" $pg psql -h 127.0.0.1 -U postgres -d openworldserver -Atc "SELECT to_regclass('ows_inventory.buckets') IS NOT NULL" 2>$null
        $ready -eq 't'
    } 'PostgreSQL clean initialization did not finish.'
    Sql @"
INSERT INTO customers (customerguid, customername, customeremail) VALUES
 ('$tenant', 'Inventory pilot', 'pilot@example.test'), ('$other', 'Other tenant', 'other@example.test');
INSERT INTO characters (customerguid, characterid, email, charname, x, y, z, perception, acrobatics, climb, stealth, classid)
VALUES ('$tenant', $character, 'pilot@example.test', 'Pilot', 0, 0, 0, 0, 0, 0, 0, 0),
 ('$other', $character, 'other@example.test', 'Other', 0, 0, 0, 0, 0, 0, 0, 0),
 ('$tenant', $($character + 1), 'large@example.test', 'Large', 0, 0, 0, 0, 0, 0, 0, 0);
"@
    $apiPort = Free-Port
    $api = Start-Inventory $false $apiPort api
    $worker = Start-Inventory $true (Free-Port) worker
    $base = "http://127.0.0.1:$apiPort/v1"
    $ownerPath = "/characters/$character/inventory"
    Wait-For {
        try { (Invoke-WebRequest "http://127.0.0.1:$apiPort/health/ready" -TimeoutSec 2 -SkipHttpErrorCheck).StatusCode -eq 200 }
        catch { $false }
    } 'Inventory API did not start.'
    foreach ($box in @(@{ containerId = 'bag'; slotted = $false }, @{ containerId = 'vault'; slotted = $false; capacity = 1 },
        @{ containerId = 'grid'; slotted = $true; capacity = 2 })) {
        Assert ((Request Put "/container-templates/$($box.containerId)" $adminToken $box).Status -eq 200) 'Template failed.'
    }
    foreach ($definition in @(@{ definitionId = 'ore'; name = 'Ore'; maxStack = 20 },
        @{ definitionId = 'sword'; name = 'Sword'; maxStack = 1 })) {
        Assert ((Request Put "/definitions/$($definition.definitionId)" $adminToken $definition).Status -eq 200) 'Definition failed.'
    }
    Assert ((Request Get '/definitions/sword' $otherToken $null).Status -eq 404) 'A different tenant could read the catalog.'
    $set = New-Command setAuthority @{ newAuthority = 'pilot-region'; newAuthorityGeneration = $generation }
    $initial = Commit $set $adminToken
    Assert ($initial.revision -eq 1) 'Initial authority revision was incorrect.'
    Assert ((Request Get "$ownerPath/containers" $otherToken $null).Json.containers.Count -eq 0) 'Same character ID leaked across tenants.'
    Assert ((Request Get "$ownerPath/containers" $regionToken $null).Status -eq 403) 'Missing authority was accepted.'

    $grant = New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'ore'; quantity = 3 }
    $first = Commit $grant $regionToken $generation
    $retry = Commit $grant $regionToken $generation
    Assert ($first.revision -eq $retry.revision -and $first.items[0].itemInstanceId -eq $retry.items[0].itemInstanceId) 'Retry duplicated a grant.'
    $grant.quantity = 1
    Assert ((Submit $grant $regionToken).Status -eq 409) 'Command ID reuse with different quantity was accepted.'
    $stacked = Commit (New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'ore'; quantity = 1 }) $regionToken $generation
    Assert ($stacked.items.Count -eq 1 -and $stacked.items[0].itemInstanceId -eq $first.items[0].itemInstanceId -and $stacked.items[0].quantity -eq 4) 'Slotless grant did not update one compatible stack.'
    $split = Commit (New-Command split @{ authorityGeneration = $generation; containerId = 'grid'; slotNumber = 0; quantity = 1
        itemInstanceId = $stacked.items[0].itemInstanceId; expectedItemRevision = $stacked.revision }) $regionToken $generation
    Assert ($split.items.Count -eq 2) 'Cross-mode split did not update both affected rows.'
    $gridOre = $split.items | Where-Object { $_.containerId -eq 'grid' }
    $swordGrant = New-Command grant @{ authorityGeneration = $generation; containerId = 'grid'; slotNumber = 1; quantity = 1; definitionId = 'sword' }
    $sword = (Commit $swordGrant $regionToken $generation).items[0]
    $blocked = New-Command grant @{ authorityGeneration = $generation; containerId = 'grid'; slotNumber = 1; quantity = 1; definitionId = 'sword' }
    Assert ((Submit $blocked $regionToken).Status -eq 409) 'Occupied slot accepted another item.'
    $swap = Commit (New-Command swap @{ authorityGeneration = $generation; itemInstanceId = $gridOre.itemInstanceId
        expectedItemRevision = $gridOre.revision; targetItemInstanceId = $sword.itemInstanceId; expectedTargetRevision = $sword.revision }) $regionToken $generation
    Assert (($swap.items | Where-Object { $_.definitionId -eq 'sword' }).slotNumber -eq 0) 'Numbered slots were not swapped.'
    $page = Request Get "$ownerPath/containers/grid/items?limit=1" $regionToken $null $generation
    Assert ($page.Json.items[0].slotNumber -eq 0 -and $page.Json.nextCursor) 'Slotted page did not return a slot-ordered cursor.'
    $cursor = [Uri]::EscapeDataString($page.Json.nextCursor)
    Assert ((Request Get "$ownerPath/containers/grid/items?limit=1&cursor=$cursor" $regionToken $null $generation).Json.items[0].slotNumber -eq 1) 'Second slot page was wrong.'
    $move = Commit (New-Command move @{ authorityGeneration = $generation; itemInstanceId = $sword.itemInstanceId
        expectedItemRevision = $swap.revision; containerId = 'vault' }) $regionToken $generation
    Assert ($move.items[0].containerId -eq 'vault' -and $null -eq $move.items[0].slotNumber) 'Cross-mode move retained the old slot.'
    Assert ((Request Get "$ownerPath/containers/grid/items?cursor=$cursor" $regionToken $null $generation).Status -eq 409) 'Changed container accepted an old cursor.'
    $bagOre = $split.items | Where-Object { $_.containerId -eq 'bag' }
    $merge = Commit (New-Command merge @{ authorityGeneration = $generation; itemInstanceId = $gridOre.itemInstanceId
        expectedItemRevision = $swap.revision; targetItemInstanceId = $bagOre.itemInstanceId; expectedTargetRevision = $bagOre.revision
        quantity = 1 }) $regionToken $generation
    Assert ($merge.items[0].quantity -eq 4 -and $merge.removedItemIds[0] -eq $gridOre.itemInstanceId) 'Merge did not preserve total quantity.'
    $remove = Commit (New-Command remove @{ authorityGeneration = $generation; itemInstanceId = $bagOre.itemInstanceId
        expectedItemRevision = $merge.revision; quantity = 1 }) $regionToken $generation
    Assert ($remove.items[0].quantity -eq 3) 'Consume did not update the selected stack.'
    $stale = New-Command remove @{ authorityGeneration = $generation; itemInstanceId = $bagOre.itemInstanceId; expectedItemRevision = $merge.revision; quantity = 1 }
    Assert ((Submit $stale $regionToken).Status -eq 409) 'Stale item revision was accepted.'

    $reward = [guid]::NewGuid()
    $stage = New-Command stageLoot @{ authorityGeneration = $generation; rewardId = $reward; definitionId = 'sword'; quantity = 1 }
    $staged = Commit $stage $regionToken $generation
    Assert ((Request Get "$ownerPath/containers/_loot/items" $regionToken $null $generation).Status -eq 404) 'Hidden loot appeared in normal inventory.'
    Assert ((Request Get "$ownerPath/changes?afterRevision=$($staged.revision - 1)" $regionToken $null $generation).Json.changes[0].items.Count -eq 0) 'Staged loot leaked into normal changes.'
    Assert ((Sql "SELECT count(*) FROM ows_inventory.loot WHERE customer_guid = '$tenant' AND reward_id = '$reward' AND NOT claimed") -eq '1') 'Staged loot was announced before it was durable.'

    $digest = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("$($tenant):$character"))
    $bucket = [BitConverter]::ToUInt32($digest, 0) % 64
    $bucketTag = 'ows:inventory:v1:{bucket-' + $bucket.ToString('D2') + '}'
    Wait-For { [long](Docker-Call exec $redis redis-cli PTTL "$($bucketTag):ready") -gt 3900 } 'Worker did not refresh its readiness lease.'
    Stop-Inventory $worker
    $queued = New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'ore'; quantity = 1 }
    Assert ((Submit $queued $regionToken).Status -eq 202) 'Stopped worker did not leave a pending command.'
    Assert ((Request Get "$ownerPath/commands/$($queued.commandId)" $regionToken $null $generation).Status -eq 202) 'Pending receipt could not be resolved.'
    Assert ((Request Get "$ownerPath/containers/bag/items" $regionToken $null $generation).Json.items[0].quantity -eq 3) 'Pending change leaked into a committed read.'

    # Commit the pending event without updating Redis to recreate a crash after SQL commit.
    $journal = Docker-Call exec $redis redis-cli --json XREVRANGE "$($bucketTag):journal" + - COUNT 1
    $entry = (ConvertFrom-Json -InputObject $journal -NoEnumerate)[0]
    $payload = $entry[1][1].Replace("'", "''")
    Assert ((Sql "SELECT ows_inventory.apply_batch(bucket, epoch, sequence, '[$payload]'::jsonb) FROM ows_inventory.buckets WHERE bucket = $bucket") -eq 't') 'The fixture could not persist the queued event.'
    $durable = Request Get "$ownerPath/commands/$($queued.commandId)" $regionToken $null $generation
    Assert ($durable.Status -eq 200 -and $durable.Json.items[0].quantity -eq 4) 'Commit was lost before Redis acknowledgement.'
    $worker = Start-Inventory $true (Free-Port) restarted-worker
    Assert ((Commit $queued $regionToken $generation).revision -eq $durable.Json.revision) 'Replay applied a committed grant twice.'

    $racePort = Free-Port
    $null = Start-Inventory $false $racePort confirmation-race-api 4000
    Wait-For {
        try {
            (Invoke-WebRequest "http://127.0.0.1:$racePort/health/ready" -TimeoutSec 2 -SkipHttpErrorCheck).StatusCode -eq 200
        } catch { $false }
    } 'Confirmation race API did not start.'
    $durableSequence = Sql "SELECT sequence FROM ows_inventory.buckets WHERE bucket = $bucket"
    Wait-For {
        (Docker-Call exec $redis redis-cli HGET "$($bucketTag):control" confirmed) -eq $durableSequence -and
        [long](Docker-Call exec $redis redis-cli PTTL "$($bucketTag):ready") -gt 4000
    } 'Worker did not publish the durable checkpoint for the confirmation race.'
    Stop-Inventory $worker

    $lost = New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'ore'; quantity = 1 }
    $http = [Net.Http.HttpClient]::new()
    $http.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $regionToken)
    $body = [Net.Http.StringContent]::new(($lost | ConvertTo-Json -Compress), [Text.Encoding]::UTF8, 'application/json')
    try {
        $inFlight = $http.PostAsync("http://127.0.0.1:$racePort/v1$ownerPath/commands", $body)
        $lostSequence = [long]$durableSequence + 1
        Wait-For {
            $journal = Docker-Call exec $redis redis-cli --json XRANGE "$($bucketTag):journal" "$lostSequence-0" "$lostSequence-0"
            $journal -ne '[]'
        } 'The original command was never queued.'
        $journal = Docker-Call exec $redis redis-cli --json XRANGE "$($bucketTag):journal" "$lostSequence-0" "$lostSequence-0"
        $original = (ConvertFrom-Json -InputObject $journal -NoEnumerate)[0][1][1] | ConvertFrom-Json -AsHashtable
        Assert ($original.commandId -eq $lost.commandId) 'The fixture did not capture the original command.'
        $replacementId = [guid]::NewGuid()
        $original.commandId = $replacementId
        $original.eventId = [guid]::NewGuid()
        $original.fingerprint = 'replacement-command'
        $original.upserts[0].quantity++
        $replacement = $original | ConvertTo-Json -Depth 10 -Compress

        # Model promotion of a replica that retained the durable anchor but lost the pending tail.
        $replaceTail = @'
local anchor = redis.call('XREVRANGE', KEYS[1], '(' .. ARGV[1], '-', 'COUNT', 1)[1]
redis.call('DEL', KEYS[1])
redis.call('XADD', KEYS[1], anchor[1], unpack(anchor[2]))
redis.call('XADD', KEYS[1], ARGV[1], 'event', ARGV[2])
redis.call('UNLINK', KEYS[2], KEYS[3])
return 1
'@
        $ownerKey = "$($bucketTag):$($tenant):$character"
        $replacement | & docker exec -i $redis redis-cli -x EVAL $replaceTail 3 "$($bucketTag):journal" "$($ownerKey):data" "$($ownerKey):index" "$lostSequence-0" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not replace the Redis tail.' }
        $escaped = $replacement.Replace("'", "''")
        Assert ((Sql "SELECT ows_inventory.apply_batch(bucket, epoch, sequence, '[$escaped]'::jsonb) FROM ows_inventory.buckets WHERE bucket = $bucket") -eq 't') 'The replacement command did not commit.'
        Assert (-not $inFlight.IsCompleted) 'The original request timed out before the replacement could signal confirmation.'
        Docker-Call exec $redis redis-cli HSET "$($bucketTag):control" confirmed $lostSequence | Out-Null

        $response = $inFlight.GetAwaiter().GetResult()
        Assert ([int]$response.StatusCode -eq 202) 'The original request incorrectly confirmed the replacement command.'
        $response.Dispose()
        Assert ((Request Get "$ownerPath/commands/$($lost.commandId)" $regionToken $null $generation).Status -eq 404) 'The lost command unexpectedly has a receipt.'
        $replacementStatus = Request Get "$ownerPath/commands/$replacementId" $regionToken $null $generation
        Assert ($replacementStatus.Status -eq 200 -and $replacementStatus.Json.commandId -eq $replacementId) 'The replacement receipt was missing.'
    }
    finally {
        $body.Dispose()
        $http.Dispose()
    }
    $worker = Start-Inventory $true (Free-Port) post-race-worker

    Docker-Call exec $redis redis-cli FLUSHDB SYNC | Out-Null
    $nextGeneration = [guid]::NewGuid()
    $handoff = New-Command setAuthority @{ authorityGeneration = $generation; newAuthority = 'pilot-next'; newAuthorityGeneration = $nextGeneration }
    $null = Commit $handoff $adminToken
    Assert ((Request Get "$ownerPath/containers" $regionToken $null $generation).Status -eq 403) 'Old Region can still read after handoff.'
    $oldWrite = New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'ore'; quantity = 1 }
    Assert ((Submit $oldWrite $regionToken).Status -eq 403) 'Old Region can still write after handoff.'
    Assert ((Request Get "$ownerPath/loot" $nextToken $null $nextGeneration).Json.items[0].itemInstanceId -eq $staged.items[0].itemInstanceId) 'Loot was lost after Redis reset and reconnect.'
    $full = New-Command claimLoot @{ authorityGeneration = $nextGeneration; rewardId = $reward
        itemInstanceId = $staged.items[0].itemInstanceId; containerId = 'vault' }
    Assert ((Submit $full $nextToken).Status -eq 409) 'Claim succeeded into a full destination.'
    Assert ((Request Get "$ownerPath/loot" $nextToken $null $nextGeneration).Json.items[0].rewardId -eq $reward) 'A full destination lost staged loot.'
    $claim = New-Command claimLoot @{ authorityGeneration = $nextGeneration; rewardId = $reward
        itemInstanceId = $staged.items[0].itemInstanceId; containerId = 'bag' }
    $claimed = Commit $claim $nextToken $nextGeneration
    Assert ($claimed.items[0].itemInstanceId -eq $staged.items[0].itemInstanceId -and $claimed.items[0].containerId -eq 'bag') 'Claim did not preserve identity.'
    Assert ((Request Get "$ownerPath/loot/$reward" $nextToken $null $nextGeneration).Json.claimed) 'Claim was not recorded durably.'
    $restage = New-Command stageLoot @{ authorityGeneration = $nextGeneration; rewardId = $reward; definitionId = 'sword'; quantity = 1 }
    Assert ((Submit $restage $nextToken).Status -eq 409) 'The same personal reward could be granted twice.'

    $http = [Net.Http.HttpClient]::new()
    $http.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $nextToken)
    $concurrent = @(1..32 | ForEach-Object {
        New-Command grant @{ authorityGeneration = $nextGeneration; containerId = 'bag'; definitionId = 'ore'; quantity = 1; condition = 17 }
    })
    $bodies = @()
    try {
        # Interleave a duplicate of every command with the original requests.
        $requests = foreach ($command in ($concurrent + $concurrent)) {
            $body = [Net.Http.StringContent]::new(($command | ConvertTo-Json -Compress), [Text.Encoding]::UTF8, 'application/json')
            $bodies += $body
            $http.PostAsync("$base$ownerPath/commands", $body)
        }
        foreach ($request in $requests) {
            $response = $request.GetAwaiter().GetResult()
            Assert ([int]$response.StatusCode -in 200, 202) 'Concurrent grant or retry failed.'
            $response.Dispose()
        }
    }
    finally {
        foreach ($body in $bodies) { $body.Dispose() }
        $http.Dispose()
    }
    Wait-For {
        (Sql "SELECT coalesce(sum(quantity), 0) FROM ows_inventory.items WHERE customer_guid = '$tenant' AND character_id = $character AND condition = 17") -eq '32'
    } 'Concurrent grants did not preserve exact quantities.'
    Assert ((Sql "SELECT count(*) FROM ows_inventory.items WHERE customer_guid = '$tenant' AND character_id = $character AND condition = 17") -eq '2') 'Concurrent grants did not consolidate into legal stacks.'

    $large = $character + 1
    Sql @"
INSERT INTO ows_inventory.owners (customer_guid, character_id, revision) VALUES ('$tenant', $large, 1);
INSERT INTO ows_inventory.containers (customer_guid, character_id, container_id, slotted, occupied, revision)
VALUES ('$tenant', $large, 'bag', false, 4000, 1);
INSERT INTO ows_inventory.items (customer_guid, character_id, item_instance_id, container_id, slotted, definition_id, max_stack, quantity, revision)
SELECT '$tenant', $large, gen_random_uuid(), 'bag', false, 'sword', 1, 1, 1 FROM generate_series(1, 4000);
"@
    $largePath = "/characters/$large/inventory"
    $largeAuthority = New-Command setAuthority @{ newAuthority = 'pilot-region'; newAuthorityGeneration = $generation }
    $null = Commit $largeAuthority $adminToken ([guid]::Empty) $largePath
    $largeGrant = New-Command grant @{ authorityGeneration = $generation; containerId = 'bag'; definitionId = 'sword'; quantity = 1 }
    $single = Commit $largeGrant $regionToken $generation $largePath
    Assert ($single.items.Count -eq 1 -and
        (Sql "SELECT count(*) FROM ows_inventory.items WHERE customer_guid='$tenant' AND character_id=$large AND revision = $($single.revision)") -eq '1') 'Pickup rewrote more than one item in the 4,000-item inventory.'
    $largePage = Request Get "$largePath/containers/bag/items?limit=100" $regionToken $null $generation
    Assert ($largePage.Json.items.Count -eq 100 -and $largePage.Json.nextCursor) 'Large inventory did not return a bounded page.'

    $dollar = '$$'
    Sql @"
DO $dollar BEGIN
    BEGIN
        DELETE FROM characters WHERE customerguid='$tenant' AND characterid=$character;
        RAISE EXCEPTION 'Inventory deletion unexpectedly succeeded';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
END $dollar;
"@
    $revocation = New-Command setAuthority @{ authorityGeneration = $nextGeneration }
    $null = Commit $revocation $adminToken
    Assert ((Request Get "$ownerPath/containers" $nextToken $null $nextGeneration).Status -eq 403) 'Revoked Region can still read.'
    $close = New-Command close @{}
    $null = Commit $close $adminToken
    Assert ((Request Get "$ownerPath/containers" $adminToken $null).Status -eq 410) 'Closed inventory remained accessible.'
    Sql "DELETE FROM characters WHERE customerguid='$tenant' AND characterid=$character"
    Assert ((Sql "SELECT count(*) FROM ows_inventory.items WHERE customer_guid='$tenant' AND character_id=$character") -eq '0') 'Character removal left orphan inventory.'
    Write-Output "$script:assertions inventory assertions passed with $PostgresImage and Redis 7."
}
catch {
    foreach ($file in Get-ChildItem -LiteralPath $logDir) {
        Get-Content -LiteralPath $file.FullName -Tail 12
    }
    if ($containers.Contains($pg)) { & docker logs --tail 12 $pg }
    throw
}
finally {
    foreach ($process in $processes) { Stop-Inventory $process }
    foreach ($container in $containers) { & docker stop $container | Out-Null }
    foreach ($file in Get-ChildItem -LiteralPath $logDir -File) { Remove-Item -LiteralPath $file.FullName }
    Remove-Item -LiteralPath $logDir.FullName
}

