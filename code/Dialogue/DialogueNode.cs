// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online
using System;
namespace Clover.Dialogue;

[AssetType( Name = "Base Dialogue Node", Extension = "dgnode" )]
public class BaseDialogueNode : GameResource
{
	public string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	///  The text to display for this dialogue node. Newlines (\n) are supported.
	///  Variables like %PLAYERNAME% will be replaced with the appropriate values.
	/// </summary>
	[TextArea]
	public string Text { get; set; } = "";

	[Property] public bool IsYou { get; set; } = false;

	/// <summary>
	///  The name of the speaker for this dialogue node. If empty, the NPC's name will be used.
	/// </summary>
	public string Speaker { get; set; }

	/// <summary>
	///  The choices available to the player at this dialogue node.
	///  Optimally, these should point to child nodes via TargetNodeId.
	/// </summary>
	[Property, InlineEditor, WideMode]
	public List<DialogueChoice> Choices { get; set; } = new();

	/// <summary>
	///  If true, when advancing from this node, the dialogue will exit instead of going to the next node.
	/// </summary>
	[Property]
	public bool ExitsDialogue { get; set; } = false;

	/// <summary>
	///  The ID of the next node to advance to after this one. Used for sequential flow.
	/// </summary>
	[Property]
	public string NextNodeId { get; set; }

	/// <summary>
	///  If set, this node will start the specified quest when entered.
	/// </summary>
	// [Property]
	// public QuestData StartsQuest { get; set; }

	/// <summary>
	///  If set, this node will activate the specified camera node when entered.
	/// </summary>
	[Property]
	public string CameraNodeId { get; set; }

	[Property]
	public Variant ReturnValue { get; set; }


	[Property] public bool StartsNavigation { get; set; } = false;

	[Property, ShowIf( nameof( StartsNavigation ), true )]
	public Vector3 NavigationTarget { get; set; } = Vector3.Zero;

	[Property, ShowIf( nameof( StartsNavigation ), true )]
	public string NavigationLabel { get; set; } = "Go here";

	/// <summary>
	///  The position of this node in the dialogue editor canvas. Editor-only, not used at runtime.
	/// </summary>
	[Hide]
	public Vector2 EditorPosition { get; set; } = Vector2.Zero;

	public override string ToString()
	{
		return $"{Id}: {Text.Truncate( 30 )}";
	}

	public void OnNodeEntered()
	{
		Log.Info( $"Entered dialogue node {Id}." );

		/*if ( StartsQuest.IsValid() )
		{
			QuestManager.Instance.RpcStartQuest( StartsQuest );
		}*/

		// TODO: CameraNode won't work because the reference is lost when serialized
		/*if ( CameraNode != null )
		{
			if ( CameraNode.IsValid() )
			{
				Log.Info( $"Activating camera node {CameraNode.GameObject?.Name}." );
				CameraNode.Priority = 30;
			}
			else
			{
				XLog.Warning( this, $"Camera node {CameraNodeId} is not valid." );
			}
		}
		else
		{
			Log.Info( "No camera node to activate." );
		}*/

		/*if ( !string.IsNullOrEmpty( CameraNodeId ) )
		{
			var cameraNode = CameraMan.Instance.GetCameraNodeById( CameraNodeId );
			if ( cameraNode.IsValid() )
			{
				Log.Info( $"Activating camera node {cameraNode.GameObject?.Name}." );
				cameraNode.Priority = 30;
			}
			else
			{
				XLog.Warning( this, $"Camera node {CameraNodeId} not found or is not valid." );
			}
		}*/

		/*if ( StartsNavigation )
		{
			CompassPanel.Instance.AddNavigationMarker( NavigationLabel, null, NavigationTarget );
		}*/

	}

	public void OnNodeExited()
	{
		Log.Info( $"Exited dialogue node {Id}." );

		/*if ( CameraNode != null && CameraNode.IsValid() )
		{
			CameraNode.Priority = 0;
		}*/

		/*if ( !string.IsNullOrEmpty( CameraNodeId ) )
		{
			var cameraNode = CameraMan.Instance.GetCameraNodeById( CameraNodeId );
			if ( cameraNode.IsValid() )
			{
				cameraNode.Priority = 0;
			}
			else
			{
				XLog.Warning( this, $"Camera node {CameraNodeId} not found or is not valid." );
			}
		}*/
	}
}
