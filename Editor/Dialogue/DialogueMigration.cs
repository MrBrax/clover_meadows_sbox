// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FileSystem=Editor.FileSystem;

namespace Clover.Dialogue
{
	/// <summary>
	/// Migrates .dialogue files from the old nested-tree format (Children embedded)
	/// to the new flat format (NextNodeId connections, EditorPosition).
	/// </summary>
	public static class DialogueMigration
	{
		[Menu( "Editor", "Braxnet/Migrate Dialogue to Flat Format" )]
		public static void RunMigration()
		{
			var dialogueDir = Path.Combine( FileSystem.Mounted.GetFullPath( "." ), "dialogue" );
			if ( !Directory.Exists( dialogueDir ) )
			{
				Log.Warning( "DialogueMigration: dialogue directory not found." );
				return;
			}

			var files = Directory.GetFiles( dialogueDir, "*.dialogue", SearchOption.AllDirectories );
			var migrated = 0;
			var skipped = 0;
			var errors = new List<string>();

			foreach ( var file in files )
			{
				try
				{
					if ( MigrateFile( file ) )
						migrated++;
					else
						skipped++;
				}
				catch ( Exception ex )
				{
					errors.Add( $"{Path.GetFileName( file )}: {ex.Message}" );
					Log.Error( $"DialogueMigration: failed to migrate {file}: {ex}" );
				}
			}

			var msg = $"Migrated {migrated} file(s), skipped {skipped} (already up to date).";
			if ( errors.Count > 0 )
				msg += $"\n\nErrors ({errors.Count}):\n" + string.Join( "\n", errors );

			Log.Info( $"DialogueMigration: {msg}" );

			var popup = new Window( null );
			popup.Title = "Dialogue Migration Complete";
			popup.Size = new Vector2( 420, 180 );
			popup.Canvas = new Widget( null );
			popup.Canvas.Layout = Layout.Column();
			popup.Canvas.Layout.Margin = 16;
			popup.Canvas.Layout.Spacing = 12;
			popup.Canvas.Layout.Add( new Label( msg ) );
			var closeBtn = popup.Canvas.Layout.Add( new Button( "OK", popup.Canvas ) );
			closeBtn.Clicked += popup.Close;
			popup.Show();
		}

		/// <summary>Returns true if the file was migrated, false if it was already up to date.</summary>
		private static bool MigrateFile( string filePath )
		{
			var json = File.ReadAllText( filePath );
			var root = JsonNode.Parse( json )?.AsObject();
			if ( root == null ) return false;

			var nodesArray = root["Nodes"]?.AsArray();
			if ( nodesArray == null || nodesArray.Count == 0 ) return false;

			if ( !NeedsMigration( nodesArray ) )
				return false;

			var oldNodes = ParseOldNodes( nodesArray );
			var flatNodes = new List<OldNode>();
			var xPos = 0f;

			for ( var i = 0; i < oldNodes.Count; i++ )
			{
				var nextRootId = i < oldNodes.Count - 1 ? oldNodes[i + 1].Id : null;
				FlattenNode( oldNodes[i], nextRootId, flatNodes );
			}

			// Assign editor positions in a left-to-right layout
			foreach ( var node in flatNodes )
			{
				if ( node.EditorPosition == null )
				{
					node.EditorPosition = $"{xPos},0";
					xPos += 300f;
				}
			}

			var newNodesArray = new JsonArray();
			foreach ( var node in flatNodes )
				newNodesArray.Add( BuildNodeWrapper( node ) );

			root["Nodes"] = newNodesArray;

			var output = root.ToJsonString( new JsonSerializerOptions
			{
				WriteIndented = true
			} );
			File.WriteAllText( filePath, output, Encoding.UTF8 );

			Log.Info( $"DialogueMigration: migrated {Path.GetFileName( filePath )} ({oldNodes.Count} root → {flatNodes.Count} flat nodes)" );
			return true;
		}

		/// <summary>Returns true if any node still has Children, GoToNodeIdOnExit, or is missing EditorPosition/NextNodeId keys.</summary>
		private static bool NeedsMigration( JsonArray nodesArray )
		{
			foreach ( var wrapper in nodesArray )
			{
				var data = wrapper?["data"]?.AsObject();
				if ( data == null ) continue;

				var children = data["Children"]?.AsArray();
				if ( children != null && children.Count > 0 ) return true;

				if ( data["GoToNodeIdOnExit"] is JsonValue gotoVal && gotoVal.TryGetValue<string>( out var gotoStr ) && !string.IsNullOrEmpty( gotoStr ) )
					return true;

				if ( !data.ContainsKey( "EditorPosition" ) || !data.ContainsKey( "NextNodeId" ) )
					return true;
			}
			return false;
		}

		private static void FlattenNode( OldNode node, string nextAfterSubtree, List<OldNode> result )
		{
			result.Add( node );

			if ( !string.IsNullOrEmpty( node.GoToNodeIdOnExit ) )
			{
				// GoToNodeIdOnExit was checked before Children in old AdvanceDialogue
				node.NextNodeId = node.GoToNodeIdOnExit;
			}
			else if ( node.Choices.Count > 0 )
			{
				// Choice nodes: flow is entirely through choices, no sequential NextNodeId.
				// Children in the old format were embedded copies of the choice targets; flatten them.
				foreach ( var child in node.Children )
					FlattenNode( child, nextAfterSubtree, result );
			}
			else if ( node.Children.Count > 0 )
			{
				// Sequential children — wire first child as NextNodeId, chain the rest
				node.NextNodeId = node.Children[0].Id;

				for ( var i = 0; i < node.Children.Count; i++ )
				{
					var nextSiblingId = i < node.Children.Count - 1
						? node.Children[i + 1].Id
						: nextAfterSubtree;
					FlattenNode( node.Children[i], nextSiblingId, result );
				}
			}
			else if ( !node.ExitsDialogue )
			{
				node.NextNodeId = nextAfterSubtree;
			}
		}

		private static List<OldNode> ParseOldNodes( JsonArray array )
		{
			var result = new List<OldNode>();
			foreach ( var wrapper in array )
			{
				var node = ParseNodeWrapper( wrapper );
				if ( node != null ) result.Add( node );
			}
			return result;
		}

		private static OldNode ParseNodeWrapper( JsonNode wrapper )
		{
			if ( wrapper == null ) return null;
			var data = wrapper["data"];
			if ( data == null ) return null;
			return ParseNodeData( data );
		}

		private static OldNode ParseNodeData( JsonNode data )
		{
			var node = new OldNode
			{
				Id = data["Id"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
				Text = data["Text"]?.GetValue<string>() ?? "",
				IsYou = data["IsYou"]?.GetValue<bool>() ?? false,
				Speaker = data["Speaker"]?.GetValue<string>(),
				ExitsDialogue = data["ExitsDialogue"]?.GetValue<bool>() ?? false,
				GoToNodeIdOnExit = data["GoToNodeIdOnExit"]?.GetValue<string>(),
				NextNodeId = data["NextNodeId"]?.GetValue<string>(),
				EditorPosition = data["EditorPosition"]?.GetValue<string>(),
				StartsQuestRef = data["StartsQuest"]?.ToJsonString()?.Trim( '"' ),
				CameraNodeId = data["CameraNodeId"]?.GetValue<string>(),
				StartsNavigation = data["StartsNavigation"]?.GetValue<bool>() ?? false,
				NavigationTarget = data["NavigationTarget"]?.GetValue<string>() ?? "0,0,0",
				NavigationLabel = data["NavigationLabel"]?.GetValue<string>() ?? "Go here",
			};

			// Parse Choices
			var choicesArr = data["Choices"]?.AsArray();
			if ( choicesArr != null )
			{
				foreach ( var choice in choicesArr )
				{
					node.Choices.Add( new OldChoice
					{
						Text = choice?["Text"]?.GetValue<string>() ?? "", TargetNodeId = choice?["TargetNodeId"]?.GetValue<string>()
					} );
				}
			}

			// Parse Children recursively
			var childrenArr = data["Children"]?.AsArray();
			if ( childrenArr != null )
			{
				foreach ( var childWrapper in childrenArr )
				{
					var child = ParseNodeWrapper( childWrapper );
					if ( child != null ) node.Children.Add( child );
				}
			}

			return node;
		}

		private static JsonObject BuildNodeWrapper( OldNode node )
		{
			var choices = new JsonArray();
			foreach ( var choice in node.Choices )
			{
				choices.Add( new JsonObject
				{
					["Text"] = choice.Text,
					["OnSelected"] = JsonValue.Create<object>( null ),
					["TargetNodeId"] = choice.TargetNodeId
				} );
			}

			// Build StartsQuest value — preserve as raw value (null or resource path string)
			JsonNode startsQuestNode = null;
			if ( !string.IsNullOrEmpty( node.StartsQuestRef ) && node.StartsQuestRef != "null" )
				startsQuestNode = JsonValue.Create( node.StartsQuestRef );

			var data = new JsonObject
			{
				["Id"] = node.Id,
				["Text"] = node.Text,
				["IsYou"] = node.IsYou,
				["Speaker"] = node.Speaker,
				["Choices"] = choices,
				["ExitsDialogue"] = node.ExitsDialogue,
				["NextNodeId"] = node.NextNodeId,
				["StartsQuest"] = startsQuestNode,
				["CameraNodeId"] = node.CameraNodeId,
				["StartsNavigation"] = node.StartsNavigation,
				["NavigationTarget"] = node.NavigationTarget,
				["NavigationLabel"] = node.NavigationLabel,
				["EditorPosition"] = node.EditorPosition,
				["__references"] = JsonValue.Create<object>( null )
			};

			return new JsonObject
			{
				["$compiler"] = "embed",
				["$source"] = JsonValue.Create<object>( null ),
				["data"] = data,
				["compiled"] = JsonValue.Create<object>( null )
			};
		}

		private class OldNode
		{
			public string Id { get; set; } = "";
			public string Text { get; set; } = "";
			public bool IsYou { get; set; }
			public string Speaker { get; set; }
			public bool ExitsDialogue { get; set; }
			public string GoToNodeIdOnExit { get; set; }
			public string NextNodeId { get; set; }
			public string EditorPosition { get; set; }
			public string StartsQuestRef { get; set; }
			public string CameraNodeId { get; set; }
			public bool StartsNavigation { get; set; }
			public string NavigationTarget { get; set; } = "0,0,0";
			public string NavigationLabel { get; set; } = "Go here";
			public List<OldChoice> Choices { get; } = new();
			public List<OldNode> Children { get; } = new();
		}

		private class OldChoice
		{
			public string Text { get; set; } = "";
			public string TargetNodeId { get; set; }
		}
	}
}
