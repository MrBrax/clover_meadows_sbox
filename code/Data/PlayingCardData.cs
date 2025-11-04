namespace Clover.Data;

[AssetType( Name = "PlayingCardData", Extension = "pcard" )]
public class PlayingCardData : GameResource
{
	public enum CardTypes
	{
		Red = 1,
		Yellow = 2,
		Green = 3,
		Cyan = 4,
		Blue = 5,
		Purple = 6,
		Pink = 7
	}

	public record CardPowerCombination
	{
		public CardTypes Type1;
		public CardTypes Type2;
		public int Power;
	}

	public static List<CardPowerCombination> PowerCombinations = new()
	{
		new CardPowerCombination { Type1 = CardTypes.Red, Type2 = CardTypes.Yellow, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Red, Type2 = CardTypes.Green, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Yellow, Type2 = CardTypes.Green, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Yellow, Type2 = CardTypes.Cyan, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Green, Type2 = CardTypes.Cyan, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Green, Type2 = CardTypes.Blue, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Cyan, Type2 = CardTypes.Blue, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Cyan, Type2 = CardTypes.Purple, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Blue, Type2 = CardTypes.Purple, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Blue, Type2 = CardTypes.Pink, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Purple, Type2 = CardTypes.Pink, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Purple, Type2 = CardTypes.Red, Power = 1 },
		new CardPowerCombination { Type1 = CardTypes.Pink, Type2 = CardTypes.Red, Power = 2 },
		new CardPowerCombination { Type1 = CardTypes.Pink, Type2 = CardTypes.Yellow, Power = 1 }
	};

	[Property] public CardTypes CardType { get; set; }

	[Property] public string Name { get; set; }

	[Property] public string Description { get; set; }

	[Property] public int BaseLevel { get; set; }

	public int GetBasePowerAgainst( PlayingCardData other )
	{
		if ( other == null )
			return 0;

		if ( CardType == other.CardType )
			return 0;

		var combo = PowerCombinations.FirstOrDefault( x =>
			(x.Type1 == CardType && x.Type2 == other.CardType) ||
			(x.Type1 == other.CardType && x.Type2 == CardType) );

		if ( combo == null )
			return 0;

		// if we're the other way around, the power goes into the negative
		if ( combo.Type1 == other.CardType )
			return -combo.Power;


		return combo.Power;
	}
}
