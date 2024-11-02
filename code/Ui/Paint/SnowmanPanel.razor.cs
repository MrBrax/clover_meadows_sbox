﻿using Sandbox.UI;

namespace Clover.Ui;

public partial class SnowmanPanel : Panel
{
	[Property] public Texture Texture { get; set; }

	private SceneWorld _sceneWorld;
	private ScenePanel _scenePanel;
	private SceneModel _pumpkin;

	private float _yaw = 0;

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );

		if ( firstTime )
		{
			LoadPumpkin();
		}
	}

	public override void Tick()
	{
		base.Tick();
		SetYaw( _yaw + Time.Delta * 60f );
	}

	private void LoadPumpkin()
	{
		_sceneWorld = new SceneWorld();
		_pumpkin = new SceneModel( _sceneWorld, "items/seasonal/winter/snowman_piece/snowman_piece.vmdl",
			new Transform( Vector3.Zero, Rotation.From( 0, _yaw, 0 ) ) );

		_sceneWorld.AmbientLightColor = Color.White;

		_scenePanel.World = _sceneWorld;
		_scenePanel.Camera.FieldOfView = 15f;
		// _scenePanel.Camera.Position =
		// 	_scenePanel.Camera.Position + _scenePanel.Camera.Rotation.Forward * -300f
		// 	                            + _scenePanel.Camera.Rotation.Down * 50f;
		// ;
		_scenePanel.Camera.Position = new Vector3( -80f, 0f, 25f );
		_scenePanel.Camera.Rotation = Rotation.From( 10f, 0, 0 );

		UpdatePumpkinMaterial();
	}

	public override void SetProperty( string name, string value )
	{
		base.SetProperty( name, value );

		Log.Info( $"SetProperty {name} {value}" );

		if ( name == "Texture" )
		{
			UpdatePumpkinMaterial();
		}
	}

	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdatePumpkinMaterial();
	}

	private void UpdatePumpkinMaterial()
	{
		if ( !_pumpkin.IsValid() )
		{
			return;
		}

		if ( Texture == null )
		{
			return;
		}

		var snowmanMaterial = Material.Create( $"snowman_preview.vmat", "shaders/snowman.shader" );

		snowmanMaterial.Set( "Overlay", Texture );

		_pumpkin.SetMaterialOverride( snowmanMaterial, "paint" );
	}

	public override void OnHotloaded()
	{
		base.OnHotloaded();
		LoadPumpkin();
	}

	public void SetYaw( float yaw )
	{
		_yaw = yaw % 360;
		// _arrow.Transform = new Transform( Vector3.Zero, Rotation.From( 0, _yaw, 0 ) );
		_pumpkin.Rotation = Rotation.From( 0, _yaw, 0 );
		// Log.Info( $"Arrow yaw set to {_yaw}" );
		_scenePanel.StateHasChanged();
		// StateHasChanged();
	}
}
