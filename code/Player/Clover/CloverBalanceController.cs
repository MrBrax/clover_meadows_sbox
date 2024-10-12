namespace Clover.Player.Clover;

public class CloverBalanceController : Component
{
	[Property, Sync, ReadOnly] private int Clovers { get; set; }

	private List<string> Transactions { get; set; } = new List<string>();
	private static readonly int StartingClovers = 100;

	internal void SetClovers( int clovers )
	{
		Clovers = clovers;
	}
	
	internal void SetStartingClovers()
	{
		Clovers = StartingClovers;
	}

	public bool DeductClover( int deductAmount, string deductMessage = "" )
	{
		if ( Clovers - deductAmount < 0 )
		{
			return false;
		}

		Clovers -= deductAmount;
		LogTransaction( deductAmount, deductMessage, TransactionLogType.Deducted );

		return true;
	}

	public void AddClover( int addAmount, string addMessage = "" )
	{
		Clovers += addAmount;
		LogTransaction( addAmount, addMessage, TransactionLogType.Added );
	}

	public int GetBalance()
	{
		return Clovers;
	}

	private void LogTransaction( int amount, string transactionMessage, TransactionLogType transactionLogType )
	{
		var logMessage =
			$"{amount} {transactionLogType.ToString().ToLower()}";

		if ( !string.IsNullOrEmpty( transactionMessage ) )
		{
			logMessage += $" | Transaction: {transactionMessage}";
		}

		Transactions.Add( logMessage );
	}

	private enum TransactionLogType
	{
		Deducted,
		Added,
	}
}
