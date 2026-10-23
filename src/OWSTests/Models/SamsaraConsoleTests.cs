using System;
using System.Collections.Generic;
using System.Text.Json;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.Postgres;
using OWSManagement.Auth;
using Xunit;

namespace OWSTests.Models
{
    /// <summary>
    /// The staff console's pure logic: Steam sign-in assertions, zone server events, sanction messages players see,
    /// item state decoding and role ranks.
    /// </summary>
    public class SamsaraConsoleTests
    {
        private const string ReturnTo = "http://localhost:44410/auth/steam/callback?state=AB12";
        private static readonly DateTime Now = new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc);

        private static Dictionary<string, string> SteamAssertion() => new()
        {
            ["openid.ns"] = "http://specs.openid.net/auth/2.0",
            ["openid.mode"] = "id_res",
            ["openid.op_endpoint"] = SteamOpenId.Endpoint,
            ["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.return_to"] = ReturnTo,
            ["openid.response_nonce"] = "2026-10-22T09:59:00Zabc123",
            ["openid.assoc_handle"] = "1234567890",
            ["openid.signed"] = "signed,op_endpoint,claimed_id,identity,return_to,response_nonce,assoc_handle",
            ["openid.sig"] = "c2lnbmF0dXJl"
        };

        [Fact]
        public void SteamAssertion_ReadsSteamId()
        {
            Assert.Equal("76561198000000001", SteamOpenId.ReadSteamId(SteamAssertion(), ReturnTo, Now, out string error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("openid.return_to", "http://evil.example/auth/steam/callback?state=AB12", "bad_return_to")]
        [InlineData("openid.op_endpoint", "https://evil.example/openid/login", "bad_endpoint")]
        [InlineData("openid.claimed_id", "https://steamcommunity.com/openid/id/123", "bad_claimed_id")]
        [InlineData("openid.response_nonce", "2026-10-22T09:30:00Zabc123", "stale_nonce")]
        [InlineData("openid.mode", "cancel", "not_signed_in")]
        [InlineData("openid.signed", "signed,op_endpoint,identity,return_to,response_nonce", "unsigned_claimed_id")]
        public void SteamAssertion_RefusesTamperedFields(string key, string value, string expectedError)
        {
            Dictionary<string, string> assertion = SteamAssertion();
            assertion[key] = value;
            Assert.Null(SteamOpenId.ReadSteamId(assertion, ReturnTo, Now, out string error));
            Assert.Equal(expectedError, error);
        }

        [Fact]
        public void SteamAssertion_RefusesIdentityThatDiffersFromClaimedId()
        {
            Dictionary<string, string> assertion = SteamAssertion();
            assertion["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000002";
            Assert.Null(SteamOpenId.ReadSteamId(assertion, ReturnTo, Now, out string error));
            Assert.Equal("bad_claimed_id", error);
        }

        [Fact]
        public void GameEvent_ReadsSsEventObject()
        {
            using JsonDocument document = JsonDocument.Parse(@"{""id"":""7f2b6c3e-5a1d-4c8e-9f00-112233445566"",""event"":""currency_changed"",
                ""ts"":""2026-10-22T10:00:00.000Z"",""schema"":1,""character"":""Aria"",""map"":""L_Foraas"",""delta"":-150,""balance"":850,""source"":""npc_shop_buy""}");
            GameEventIn parsed = GameEventIn.FromJson(document.RootElement);

            Assert.NotNull(parsed);
            Assert.Equal(Guid.Parse("7f2b6c3e-5a1d-4c8e-9f00-112233445566"), parsed.EventGUID);
            Assert.Equal("currency_changed", parsed.Event);
            Assert.Equal("Aria", parsed.CharacterName);
            Assert.Equal("L_Foraas", parsed.MapName);
            Assert.Equal(DateTimeKind.Utc, parsed.EventTime.Kind);
            Assert.Equal(new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc), parsed.EventTime);
            Assert.Contains("\"source\":\"npc_shop_buy\"", parsed.Fields);
        }

        [Theory]
        [InlineData(@"{""event"":""level_up"",""ts"":""2026-10-22T10:00:00Z""}")]
        [InlineData(@"{""id"":""not-a-guid"",""event"":""level_up"",""ts"":""2026-10-22T10:00:00Z""}")]
        [InlineData(@"{""id"":""7f2b6c3e-5a1d-4c8e-9f00-112233445566"",""ts"":""2026-10-22T10:00:00Z""}")]
        [InlineData(@"{""id"":""7f2b6c3e-5a1d-4c8e-9f00-112233445566"",""event"":""level_up""}")]
        [InlineData(@"[1,2,3]")]
        public void GameEvent_SkipsObjectsWithoutIdEventOrTime(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.Null(GameEventIn.FromJson(document.RootElement));
        }

        [Fact]
        public void SanctionMessage_NamesExpiryAndReason()
        {
            var ban = new Sanction { Kind = SanctionKinds.Ban, Reason = "Gold selling", ExpiresAt = new DateTime(2026, 11, 1, 14, 30, 0, DateTimeKind.Utc) };
            Assert.Equal("This account is banned until 2026-11-01 14:30 UTC. Reason: Gold selling", ban.PlayerMessage());

            var mute = new Sanction { Kind = SanctionKinds.Mute, Reason = "" };
            Assert.Equal("You are muted.", mute.PlayerMessage());
        }

        [Fact]
        public void ItemState_DecodesPersistedCustomData()
        {
            var item = new ConsoleItem
            {
                InSlotNumber = 1003,
                CustomData = "{\r\n\t\"bLocked\": true,\r\n\t\"refinementLevel\": 7,\r\n\t\"starLevel\": 2,\r\n\t\"bDamaged\": false,\r\n\t\"equippedCards\": [\"Item.Card.Yggling\", \"\"]\r\n}"
            };
            ConsoleQueriesRepository.DecodeItemState(item);

            Assert.True(item.Equipped);
            Assert.Equal(3, item.Slot);
            Assert.Equal(7, item.Refine);
            Assert.Equal(2, item.Stars);
            Assert.True(item.Locked);
            Assert.False(item.Damaged);
            Assert.Equal(new[] { "Item.Card.Yggling" }, item.Cards);
        }

        [Fact]
        public void ItemState_LeavesLegacyTextAlone()
        {
            var item = new ConsoleItem { InSlotNumber = 4, CustomData = "legacy free text" };
            ConsoleQueriesRepository.DecodeItemState(item);

            Assert.False(item.Equipped);
            Assert.Equal(0, item.Refine);
            Assert.Empty(item.Cards);
        }

        [Theory]
        [InlineData("Player", 0)]
        [InlineData("mod", 1)]
        [InlineData("GameMaster", 2)]
        [InlineData("admin", 3)]
        [InlineData("superuser", 0)]
        [InlineData(null, 0)]
        public void RoleRank_OrdersStaffRoles(string role, int rank)
        {
            Assert.Equal(rank, UserRoles.Rank(role));
        }
    }
}
