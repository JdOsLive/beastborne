using System;
using System.Collections.Generic;

namespace Beastborne.Data;

/// <summary>
/// Represents a chat message in the multiplayer chat system
/// </summary>
public class ChatMessage
{
	// Developer Steam IDs
	private static readonly HashSet<long> DeveloperSteamIds = new()
	{
		76561198088759073  // jscho
	};

	public Guid Id { get; set; } = Guid.NewGuid();
	public long SteamId { get; set; }
	public string PlayerName { get; set; }
	public string Content { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public ChatMessageType Type { get; set; } = ChatMessageType.Player;

	// For system messages (achievements, catches, etc.)
	public string IconPath { get; set; }

	// For beast showcase messages
	public string ShowcaseSpeciesId { get; set; }
	public string ShowcaseNickname { get; set; }
	public string ShowcaseSpeciesName { get; set; }
	public int ShowcaseLevel { get; set; }
	public int ShowcasePower { get; set; }
	public int ShowcaseGenes { get; set; }
	public string ShowcaseRarity { get; set; }
	public string ShowcaseVeteranRank { get; set; }
	public string ShowcaseElement { get; set; }
	public Guid ShowcaseMonsterId { get; set; } // For clicking to view own beasts

	// Extended showcase stats
	public int ShowcaseBattles { get; set; }
	public int ShowcaseKnockouts { get; set; }
	public int ShowcaseExpeditions { get; set; }
	public int ShowcaseHP { get; set; }
	public int ShowcaseATK { get; set; }
	public int ShowcaseDEF { get; set; }
	public int ShowcaseSpA { get; set; }  // Special Attack
	public int ShowcaseSpD { get; set; }  // Special Defense
	public int ShowcaseSPD { get; set; }  // Speed
	public string ShowcaseTraits { get; set; } // Comma-separated trait names

	// Display helpers
	public string FormattedTime => Timestamp.ToLocalTime().ToString( "HH:mm" );
	public bool IsSystem => Type != ChatMessageType.Player && Type != ChatMessageType.BeastShowcase;
	public bool IsDeveloper => DeveloperSteamIds.Contains( SteamId );
}

public enum ChatMessageType
{
	Player,        // Regular player message
	System,        // System announcement
	Achievement,   // Player achievement (caught monster, etc.)
	Join,          // Player joined
	Leave,         // Player left
	BeastShowcase  // Player showing off a beast
}
