using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// The staff console's Insight page and its trade and drop views, read from GameEvents (Postgres only). Trades come
    /// from the zone's "trade" events (partner, juriGiven, juriReceived, itemsGiven, itemsReceived) and drops from its
    /// "loot_stats" events (mob, kills, items[item, chance, multiplier, rolls, drops]).
    /// </summary>
    public interface IConsoleInsightRepository
    {
        /// <summary>One row per signup day for the last 30 days, newest first.</summary>
        Task<IEnumerable<NewPlayerCohort>> GetNewPlayers(Guid customerGUID);
        Task<IEnumerable<StoppedPlayer>> GetStoppedPlayers(Guid customerGUID);
        /// <summary>Quests accepted in the last 30 days.</summary>
        Task<IEnumerable<QuestFunnelRow>> GetQuestFunnel(Guid customerGUID);
        Task<IEnumerable<OutlierRow>> GetOutliers(Guid customerGUID);
        Task<IEnumerable<TradePartnerRow>> GetTradePartners(Guid customerGUID, string characterName);
        Task<IEnumerable<TradePairRow>> GetTradePairs(Guid customerGUID);
        Task<IEnumerable<DropMobRow>> GetDropMobs(Guid customerGUID);
        Task<IEnumerable<DropItemRow>> GetDropItems(Guid customerGUID);
    }
}
