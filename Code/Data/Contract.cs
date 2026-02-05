using System;
using System.Collections.Generic;

namespace Beastborne.Data;

public enum ContractDemandType
{
	Bloodthirsty,   // Must participate in battles regularly
	Greedy,         // Requires gold tributes
	Ambitious,      // Wants to level up / evolve
	Social,         // Wants to be in party with companions
	Lazy,           // Wants rest between expeditions
	Competitive     // Wants to win arena battles
}

/// <summary>
/// A single demand that a contracted monster has
/// </summary>
public class ContractDemand
{
	public ContractDemandType Type { get; set; }
	public int Intensity { get; set; } = 1;  // 1-3, how demanding

	// Progress tracking for demand completion
	public int RequiredAmount { get; set; } = 10;
	public int CurrentProgress { get; set; } = 0;

	public string GetDescription()
	{
		return Type switch
		{
			ContractDemandType.Bloodthirsty => Intensity switch
			{
				1 => "Wants occasional battles",
				2 => "Demands regular combat",
				3 => "Craves constant warfare",
				_ => "Wants battles"
			},
			ContractDemandType.Greedy => Intensity switch
			{
				1 => "Appreciates small tributes",
				2 => "Expects regular payment",
				3 => "Demands lavish offerings",
				_ => "Wants gold"
			},
			ContractDemandType.Ambitious => Intensity switch
			{
				1 => "Hopes to grow stronger",
				2 => "Expects steady progress",
				3 => "Demands rapid advancement",
				_ => "Wants to level up"
			},
			ContractDemandType.Social => Intensity switch
			{
				1 => "Enjoys company of others",
				2 => "Needs companions nearby",
				3 => "Must always have allies",
				_ => "Wants companions"
			},
			ContractDemandType.Lazy => Intensity switch
			{
				1 => "Appreciates occasional rest",
				2 => "Requires regular downtime",
				3 => "Demands frequent breaks",
				_ => "Wants rest"
			},
			ContractDemandType.Competitive => Intensity switch
			{
				1 => "Enjoys winning matches",
				2 => "Craves arena victories",
				3 => "Must dominate the arena",
				_ => "Wants to win"
			},
			_ => "Unknown demand"
		};
	}

	public string GetIcon()
	{
		return Type switch
		{
			ContractDemandType.Bloodthirsty => "ui/icons/sword.png",
			ContractDemandType.Greedy => "ui/icons/gold.png",
			ContractDemandType.Ambitious => "ui/icons/levelup.png",
			ContractDemandType.Social => "ui/icons/social.png",
			ContractDemandType.Lazy => "ui/icons/rest.png",
			ContractDemandType.Competitive => "ui/icons/trophy.png",
			_ => "ui/icons/unknown.png"
		};
	}
}

/// <summary>
/// Contract that caught (non-bred) monsters have
/// Represents the agreement between tamer and monster
/// </summary>
public class Contract
{
	public ContractDemand PrimaryDemand { get; set; }
	public List<ContractDemand> SecondaryDemands { get; set; } = new();

	// Satisfaction level (0-100)
	// Drops over time if demands aren't met
	// Rises when demands are satisfied
	public int Satisfaction { get; set; } = 75;

	public bool IsAtRisk => Satisfaction < 20;
	public bool IsHappy => Satisfaction >= 80;

	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

	public void UpdateSatisfaction( int delta )
	{
		Satisfaction = Math.Clamp( Satisfaction + delta, 0, 100 );
		LastUpdated = DateTime.UtcNow;
	}

	public string GetSatisfactionText()
	{
		return Satisfaction switch
		{
			>= 90 => "Devoted",
			>= 75 => "Content",
			>= 50 => "Neutral",
			>= 25 => "Unhappy",
			_ => "Rebellious"
		};
	}

	public string GetSatisfactionColor()
	{
		return Satisfaction switch
		{
			>= 75 => "#4ade80",  // Green
			>= 50 => "#fbbf24",  // Yellow
			>= 25 => "#fb923c",  // Orange
			_ => "#f87171"       // Red
		};
	}
}
