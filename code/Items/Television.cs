using System;
using Clover.Interactable;
using Clover.Player;

namespace Clover.Items;

public class Television : Component, IInteract
{
	[Property] public ModelRenderer Model { get; set; }

	[Property] public string ScreenMaterialAttribute { get; set; } = "tv_screen";

	[Property] public SpotLight SpotLight { get; set; }
	[Property] public Sandbox.Light Light { get; set; }

	private Material _screenMaterial;
	// private Texture _renderTexture;

	private VideoPlayer _videoPlayer;

	protected override void OnStart()
	{
		base.OnStart();

		_videoPlayer = new VideoPlayer();

		_screenMaterial = Material.Create( Random.Shared.Next( 1000, 5000 ).ToString(), "shaders/simple.shader" );
		// _screenMaterial = Material.Load( "items/furniture/electronics/tv_crt/tv_screen.vmat" );
		// _screenMaterial.Set( "Color", _renderTexture );
		// _screenMaterial.Set( "Normal", Color.White );
		// _screenMaterial.Set( "Roughness", Color.White );

		_screenMaterial.Set( "Color", _videoPlayer.Texture );
		_screenMaterial.Set( "Normal", Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );
		_screenMaterial.Set( "Roughness", Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );

		if ( Model.IsValid() )
		{
			// Model.SetMaterial( _screenMaterial );
			// Model.MaterialOverride = _screenMaterial;
			Model.SetMaterialOverride( _screenMaterial, ScreenMaterialAttribute );
		}

		if ( Light.IsValid() )
		{
			Light.Enabled = false;
		}

		// PlayVideo( "videos/test.mp4" );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_videoPlayer?.Stop();
		_videoPlayer?.Dispose();
	}

	[Broadcast]
	public void PlayVideo( string url )
	{
		_videoPlayer.Play( FileSystem.Data, url );

		_screenMaterial.Set( "Color", _videoPlayer.Texture );
		// _screenMaterial.Set( "Normal", Texture.Load( FileSystem.Mounted, "materials/default/default_normal.tga" ) );
		// _screenMaterial.Set( "Roughness", Texture.Load( FileSystem.Mounted, "materials/default/default_rough.tga" ) );

		if ( SpotLight.IsValid() )
		{
			SpotLight.Cookie = _videoPlayer.Texture;
		}

		if ( Light.IsValid() )
		{
			Light.Enabled = true;
		}

		_videoPlayer.OnLoaded += () =>
		{
			Log.Info( "Video loaded" );
		};

		_videoPlayer.OnAudioReady += () =>
		{
			Log.Info( "Audio ready" );
		};

		Log.Info( "Playing video" );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// Log.Info( $"{_videoPlayer?.PlaybackTime}/{_videoPlayer?.Duration}" );

		if ( _videoPlayer != null )
		{
			_videoPlayer.Present();
			_screenMaterial.Set( "Color", _videoPlayer.Texture );
		}

		if ( Light.IsValid() )
		{
			Light.LightColor = _videoPlayer.Texture.GetPixel( 4, 4, 3 );
		}

		// _screenMaterial.Set( "Color", _videoPlayer.Texture );
	}


	void IInteract.StartInteract( PlayerCharacter player )
	{
		PlayVideo( "videos/mono.mp4" );
	}

	/*void IInteract.StartInteractHost( PlayerCharacter player )
	{
		PlayVideo( "videos/mono.mp4" );
	}*/

	public string GetInteractName()
	{
		return "Watch";
	}
}
