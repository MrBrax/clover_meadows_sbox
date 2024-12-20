﻿using Clover.Player;

namespace Clover.Interactable;

public interface IInteract
{
	bool CanInteract( PlayerCharacter player ) { return true; }

	/// <summary>
	///  Called when the player presses down the interact button. Only called once.
	/// </summary>
	/// <param name="player"></param>
	void StartInteract( PlayerCharacter player );

	/// <summary>
	///  Called when the player releases the interact button. Only called once.
	/// </summary>
	/// <param name="player"></param>
	void FinishInteract( PlayerCharacter player ) { }

	string GetInteractName();

	void StartInteractHost( PlayerCharacter player ) { }
	void FinishInteractHost( PlayerCharacter player ) { }
}
