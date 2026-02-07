using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// State of a trade session
/// </summary>
public enum TradeState
{
	Pending,    // Trade request sent, awaiting acceptance
	Open,       // Both players in trade window, setting offers
	Ready,      // Both players clicked Ready
	Locked,     // 5-second lock-in countdown active
	Completed,  // Trade executed successfully
	Cancelled   // Trade cancelled by either party
}

/// <summary>
/// One player's offer in a trade
/// </summary>
public class TradeOffer
{
	public List<Guid> OfferedMonsterIds { get; set; } = new();
	public Dictionary<string, int> OfferedItems { get; set; } = new();
	public bool IsReady { get; set; } = false;
	public bool IsLocked { get; set; } = false;
}

/// <summary>
/// Full trade session between two players
/// </summary>
public class TradeSession
{
	public string Player1ConnectionId { get; set; }
	public string Player2ConnectionId { get; set; }
	public string Player1Name { get; set; }
	public string Player2Name { get; set; }
	public long Player1SteamId { get; set; }
	public long Player2SteamId { get; set; }

	public TradeOffer Player1Offer { get; set; } = new();
	public TradeOffer Player2Offer { get; set; } = new();
	public TradeState State { get; set; } = TradeState.Pending;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public string GetPartnerConnectionId( string myConnectionId )
	{
		return myConnectionId == Player1ConnectionId ? Player2ConnectionId : Player1ConnectionId;
	}

	public string GetPartnerName( string myConnectionId )
	{
		return myConnectionId == Player1ConnectionId ? Player2Name : Player1Name;
	}

	public TradeOffer GetMyOffer( string myConnectionId )
	{
		return myConnectionId == Player1ConnectionId ? Player1Offer : Player2Offer;
	}

	public TradeOffer GetPartnerOffer( string myConnectionId )
	{
		return myConnectionId == Player1ConnectionId ? Player2Offer : Player1Offer;
	}

	public bool BothReady => Player1Offer.IsReady && Player2Offer.IsReady;
	public bool BothLocked => Player1Offer.IsLocked && Player2Offer.IsLocked;
}

/// <summary>
/// Compact monster data for trade network serialization.
/// Includes ALL monster data since trades are permanent transfers.
/// </summary>
public class TradeMonsterData
{
	public string S { get; set; }         // SpeciesId
	public string N { get; set; }         // Nickname
	public int Lv { get; set; }           // Level
	public int XP { get; set; }           // CurrentXP
	public int HP { get; set; }           // MaxHP
	public int Atk { get; set; }          // ATK
	public int Def { get; set; }          // DEF
	public int SpA { get; set; }          // Special Attack
	public int SpD { get; set; }          // Special Defense
	public int Spe { get; set; }          // Speed

	// Genetics
	public int GHP { get; set; }          // HPGene
	public int GAtk { get; set; }         // ATKGene
	public int GDef { get; set; }         // DEFGene
	public int GSpA { get; set; }         // SpAGene
	public int GSpD { get; set; }         // SpDGene
	public int GSpe { get; set; }         // SPDGene
	public int Nat { get; set; }          // NatureType (int)

	// Moves, traits, items
	public List<string> M { get; set; }   // MoveIds
	public List<int> PP { get; set; }     // Move PPs
	public List<string> T { get; set; }   // Traits
	public string I { get; set; }         // HeldItemId

	// Lineage
	public int Gen { get; set; }          // Generation
	public bool Bred { get; set; }        // IsBred (no contract)

	// Original Trainer
	public string OTN { get; set; }       // OriginalTrainerName
	public long OTI { get; set; }         // OriginalTrainerId

	// Veteran stats
	public int BF { get; set; }           // BattlesFought
	public int TD { get; set; }           // TotalDamageDealt
	public int TK { get; set; }           // TotalKnockouts
	public int BD { get; set; }           // BossesDefeated
	public int EC { get; set; }           // ExpeditionsCompleted
}
