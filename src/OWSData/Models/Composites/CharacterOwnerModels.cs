using System;
using System.Collections.Generic;

namespace OWSData.Models.Composites
{
    public enum CharacterClaimKind : short
    {
        Login = 1,
        Travel = 2
    }

    public class ClaimCharacterResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string CharacterName { get; set; }
        public Guid OwnerToken { get; set; }
        // The token this claim replaced, or null; the zone server removes a local copy that holds it.
        public Guid? ReplacedOwnerToken { get; set; }
        // Above 0 when a live owner was replaced: its saves still land this long, and the character loads after it.
        public int HandoverSeconds { get; set; }
    }

    public class CancelJoinTicketResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        // True when the ticket can never be claimed (voided now or earlier, or unknown); false when it was claimed.
        public bool Cancelled { get; set; }
        public bool Claimed { get; set; }
    }

    public class CharacterOwnerCheck
    {
        public string CharacterName { get; set; }
        public Guid OwnerToken { get; set; }
    }

    public class StaleCharacterOwner
    {
        public string CharacterName { get; set; }
        public Guid OwnerToken { get; set; }
        // How the current owner claimed the character: "login" or "travel".
        public string Reason { get; set; }
    }

    // The reply to a save whose OwnerToken is not the character's current owner.
    public class StaleOwnerResult
    {
        public bool Success => false;
        public string ErrorMessage => "stale_owner";
        public string ReasonCode => "stale_owner";
    }

    public class CheckOwnersResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<StaleCharacterOwner> Stale { get; set; } = new List<StaleCharacterOwner>();
    }
}
