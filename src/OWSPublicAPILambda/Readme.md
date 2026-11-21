# OWS Public API Lambda

This project exposes selected OWS Public API actions through the API Gateway proxy handler in `Function.cs`. The handler does not automatically inherit routes from the ASP.NET `OWSPublicAPI` controller.

## Connection ticket endpoint

Route API Gateway `POST /api/Users/ExternalConnectionTicket` to this Lambda function. Include the existing `X-CustomerGUID` header and a JSON body:

```json
{
  "UserSessionGUID": "00000000-0000-0000-0000-000000000000",
  "ServerId": 1,
  "MapInstanceId": 42
}
```

The selected character must be assigned to the ready `MapInstanceId`. `ServerId`
identifies an active, credentialed Edge in the same tenant. Issuance and
redemption both return `MapInstanceId`, which Edge can use for routing. An
ineligible Edge or destination returns 403. The ticket and its Base64 session
secret expire after 45 seconds. Use HTTPS.

## Edge redemption

Route `POST /api/Instance/RedeemExternalConnectionTicket` through the existing
OWS Instance Management HTTP API Gateway and Lambda handler (payload format 2.0).
Configure the same scope and Valkey settings on the issuer and redeemer.
The route requires an Edge credential scoped to the ticket's tenant and server.
When the existing gateway is public, the route is reachable over the internet
and credential checks control redemption.

Edge sends `X-Edge-Redemption-Credential` with the secret for its scope.
It sends `Claim: false` to retrieve the secret and verify the UDP handshake,
then `Claim: true` with a stable `ClaimantId` for the atomic claim.

## Lambda environment variables

In the AWS Lambda function configuration, set:

- `OWS_DB_CONNECTION_STRING`: existing OWS PostgreSQL connection string (the handler also accepts `OWSDBConnectionString` or `OWSStorageConfig__OWSDBConnectionString`).
- `OWSTicketConfig__ConnectionString`: Valkey connection string used by `TicketRepository`, for example `valkey-endpoint:6379,password=REPLACE_ME,ssl=true,abortConnect=false`. Use the actual endpoint, port, authentication, and TLS settings of the AWS Valkey deployment. Do not commit the real connection string to source or `aws-lambda-tools-defaults.json`.
- `OWS_INSTANCE_ASSIGNMENT_CREDENTIAL`: random service secret of at least 32
  characters, configured on OWSPublicAPI and OWSInstanceManagement. It grants
  access to `POST /api/Instance/CheckReadyAssignment`.
- `INSTANCE_MANAGEMENT_API_URL`: base URL of the Instance Management gateway
  used by OWSPublicAPI Lambda. Include the stage and trailing slash if applicable.
- `OWS_INSTANCE_LAUNCHER_CREDENTIAL`: separate random service secret of at least
  32 characters, configured on Instance Management and used by trusted callers
  for map management, Edge registration, and Edge credential provisioning.

Apply the PostgreSQL or SQL Server `20261114CreateEdgeServers.sql` and
`20261114MoveServerCredentialToEdgeServers.sql` migrations in that order before
deploying Instance Management. Provision a fresh credential per shared Edge.
Existing per-map hashes are retired because they have no shared Edge identity.

## Edge lifecycle

Run the shared Edge independently of the map launcher. Create an `EdgeServers`
record, generate a random 32-byte credential, and provision the hex-encoded
SHA-256 hash of the credential string. Pass the original credential to Edge
through its deployment secret configuration. After Edge can accept player
connections, call `SetEdgeServerReady` with that credential. Use the same
credential for redemption. Revoke it when stopping Edge or before rotating it.
See the [Instance Management contract](../OWSInstanceManagementLambda/Readme.md#edge-server-registry)
for the request bodies and headers. The regular map launcher and map-status
updates continue to manage map instances.

For service operations, send a JSON body with `CustomerGuid`. Creation also
takes `WorldServerId`, `MapId`, and `Port` and returns the allocated
`MapInstanceId`. The ready-instance list returns no credential material.
