using System.IO;
using System.Text;
using Clover.Persistence;
using Clover.Ui;
using Clover.Utilities;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Clover.Items;

[Category( "Clover/Items" )]
public class FloorDecal : Component, IPersistent, IPaintEvent
{
	[RequireComponent] public WorldItem WorldItem { get; private set; }

	[Property] public DecalRenderer DecalRenderer { get; set; }

	[Property] public ModelRenderer ModelRenderer { get; set; }

	private Decals.DecalData _decalData;

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


	public void UpdateDecal()
	{
		if ( IsProxy )
		{
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

		material.Set( "Color", _decalData.Texture );

		ModelRenderer.MaterialOverride = material;

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

		// _decalData = decal.ToDecalData();
		_decalData = Decals.ToDecalData( decal );

		var hash = Crc64.FromBytes( decal.Image );

		var material = Material.Create( $"{hash}.vmat", "shaders/floor_decal.shader" );
		material.Set( "Color", Decals.GetDecalTexture( decal ) );
		ModelRenderer.MaterialOverride = material;
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

	/*protected override void OnUpdate()
	{
		DebugOverlay.Text( WorldPosition + Vector3.Up * 8f, _texturePath, 16f );
	}*/
}
