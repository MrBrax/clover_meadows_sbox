using Clover.Persistence;
using Clover.Ui;
using Clover.Utilities;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Clover.Items;

public class DecalItem : Component, IPersistent, IPaintEvent
{
	private Decals.DecalData _decalData;
	protected Texture DecalTexture;

	private string _texturePath;


	[Sync]
	public string TexturePath
	{
		get => _texturePath;
		set
		{
			_texturePath = value;
			UpdateDecal();
		}
	}

	[Sync] public string DecalHash { get; set; }

	public void UpdateDecal()
	{
		if ( Scene.IsEditor ) return;
		if ( string.IsNullOrEmpty( TexturePath ) ) return;

		if ( IsProxy )
		{
			FileSystem.Data.CreateDirectory( "decalcache" );
			if ( FileSystem.Data.FileExists( $"decalcache/{DecalHash}.decal" ) )
			{
				_decalData = Decals.ReadDecal( $"decalcache/{DecalHash}.decal" );
				DecalTexture = Decals.GetDecalTexture( _decalData.ToRpc() );
				var material1 = Material.Create( $"{DecalHash}.vmat", "shaders/floor_decal.shader" );
				material1.Set( "Color", DecalTexture );
				// ModelRenderer.MaterialOverride = material1;
				OnMaterialUpdate( material1 );
				Log.Info( $"Updated cached decal '{_decalData.Name}' with texture: {TexturePath}" );
				return;
			}

			RequestDecal();
			return;
		}

		// Update decal
		var material = Material.Create( $"{TexturePath}.vmat", "shaders/floor_decal.shader" );

		try
		{
			_decalData = Decals.ReadDecal( TexturePath );
		}
		catch ( System.Exception e )
		{
			Log.Error( e.Message );
			return;
		}

		DecalTexture = _decalData.Texture;

		material.Set( "Color", _decalData.Texture );

		// ModelRenderer.MaterialOverride = material;
		OnMaterialUpdate( material );

		DecalHash = _decalData.GetHash();

		Log.Info( $"Updated decal '{_decalData.Name}' with texture: {TexturePath}" );
	}

	[Authority]
	private void RequestDecal()
	{
		Assert.True( Networking.IsHost );

		var caller = Rpc.Caller;

		if ( string.IsNullOrEmpty( _texturePath ) )
		{
			Log.Warning( "Texture path is null or empty" );
			return;
		}

		if ( string.IsNullOrEmpty( _decalData.Name ) )
		{
			Log.Warning( "Decal name is null or empty" );
			return;
		}

		var rpcDecal = _decalData.ToRpc();

		Log.Info( $"Sending decal '{rpcDecal.Name}' by '{rpcDecal.Author}' to {caller}" );

		using ( Rpc.FilterInclude( caller ) )
		{
			RecieveDecal( _texturePath, rpcDecal );
		}
	}


	[Broadcast]
	public void RecieveDecal( string filename, Decals.DecalDataRpc decal )
	{
		Log.Info( $"Recieved decal '{filename}':" );
		Log.Info( $"Size: {decal.Width}x{decal.Height}" );
		Log.Info( $"Name: {decal.Name}" );
		Log.Info( $"Author: {decal.Author}" );
		Log.Info( $"Palette: {decal.Palette}" );
		Log.Info( $"Image: {decal.Image?.Length} bytes" );

		if ( string.IsNullOrEmpty( decal.Name ) )
		{
			Log.Error( "Decal name is null or empty" );
			return;
		}

		_decalData = decal.ToDecalData();
		// _decalData = Decals.ToDecalData( decal );

		var hash = _decalData.GetHash();

		var material = Material.Create( $"{hash}.vmat", "shaders/floor_decal.shader" );
		material.Set( "Color", Decals.GetDecalTexture( decal ) );
		// ModelRenderer.MaterialOverride = material;
		OnMaterialUpdate( material );

		FileSystem.Data.CreateDirectory( "decalcache" );
		var file = FileSystem.Data.OpenWrite( $"decalcache/{hash}.decal" );
		Decals.WriteDecal( file, decal.ToDecalData() );
		file.Close();
	}

	public virtual void OnMaterialUpdate( Material material )
	{
	}

	public void OnSave( PersistentItem item )
	{
		item.SetArbitraryData( "TexturePath", TexturePath );
	}

	public void OnLoad( PersistentItem item )
	{
		TexturePath = item.GetArbitraryData<string>( "TexturePath" );
		// UpdateDecal();
	}

	void IPaintEvent.OnFileSaved( string path )
	{
		if ( TexturePath == path )
		{
			Log.Info( "Updating decal" );
			UpdateDecal();

			RecieveDecal( path, _decalData.ToRpc() );
		}
	}

	[ConCmd( "clover_delete_old_decals" )]
	public static void DeleteOldDecals()
	{
		var decals = Game.ActiveScene.GetAllComponents<DecalItem>();
		foreach ( var decal in decals )
		{
			if ( decal.DecalHash == null )
			{
				Log.Info( $"Deleting old decal: {decal.TexturePath}" );
				// decal.Delete();
			}
		}
	}
}
