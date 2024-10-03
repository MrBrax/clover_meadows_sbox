using Clover.Player;
using Sandbox;

namespace Clover;

public sealed class NodeManager : Component
{
	public static NodeManager Instance { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		Instance = this;
	}

	/*public static UserInterface UserInterface => Instance.GetNode<UserInterface>( "/root/Main/UserInterface" );
	public static InventoryUi InventoryUi => Instance.GetNode<InventoryUi>( "/root/Main/UserInterface/Inventory" );
	public static WorldManager WorldManager => Instance.GetNode<WorldManager>( "/root/Main/WorldManager" );
	public static SettingsSaveData SettingsSaveData => Instance.GetNode<SettingsSaveData>( "/root/SettingsSaveData" );
	public static TimeManager TimeManager => Instance.GetNodeOrNull<TimeManager>( "/root/Main/TimeManager" );
	public static Camera3D PlayerCamera => Instance.GetNode<Camera3D>( "/root/Main/PlayerCamera" );
	public static PlayerController Player => Instance.GetNode<PlayerController>( "/root/Main/Player" );
	public static WeatherManager WeatherManager => Instance.GetNode<WeatherManager>( "/root/Main/WeatherManager" );*/
	
	public static WorldManager WorldManager => Game.ActiveScene.GetAllComponents<WorldManager>().FirstOrDefault();
	public static PlayerCharacter Player => Game.ActiveScene.GetAllComponents<PlayerCharacter>().FirstOrDefault();
	
}
