using Editor.NodeEditor;
namespace Clover.Dialogue;

// ============================================================
//  Main editor window
// ============================================================
[EditorForAssetType( "dialogue" )]
public class DialogueEditorWindow : Window, IAssetEditor
{
	public bool CanOpenMultipleAssets => true;

	private Asset _asset;
	private DialogueData _dialogueData;
	private DialogueGraphView _graphView;
	private Widget _propertiesPanel;
	private Widget _propertiesContent;

	public DialogueEditorWindow()
	{
		DeleteOnClose = true;
		Title = "Dialogue Editor";
		Size = new Vector2( 1280, 800 );
		SetWindowIcon( "forum" );

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Spacing = 0;

		// Toolbar row
		var toolbar = Canvas.Layout.AddRow();
		toolbar.Spacing = 4;
		toolbar.Margin = 4;

		var saveBtn = new Button( "Save", "save", Canvas );
		saveBtn.Clicked += SaveAsset;
		saveBtn.ToolTip = "Save dialogue";
		toolbar.Add( saveBtn );

		var addBtn = new Button( "Add Node", "add", Canvas );
		addBtn.Clicked += AddNewNode;
		addBtn.ToolTip = "Add a new dialogue node";
		toolbar.Add( addBtn );

		var centerBtn = new Button( "Center", "center_focus_strong", Canvas );
		centerBtn.Clicked += () => _graphView.CenterOnSelection();
		centerBtn.ToolTip = "Center view on selection";
		toolbar.Add( centerBtn );

		// Main content row: graph + properties panel
		var contentRow = new Widget( Canvas );
		contentRow.Layout = Layout.Row();
		contentRow.Layout.Spacing = 0;
		Canvas.Layout.Add( contentRow, 1 );

		_graphView = new DialogueGraphView( contentRow );
		contentRow.Layout.Add( _graphView, 1 );

		_propertiesPanel = new Widget( contentRow );
		_propertiesPanel.Layout = Layout.Column();
		_propertiesPanel.Layout.Spacing = 0;
		_propertiesPanel.FixedWidth = 300;
		contentRow.Layout.Add( _propertiesPanel );

		var headerRow = _propertiesPanel.Layout.AddRow();
		headerRow.Spacing = 4;
		headerRow.Margin = 8;
		headerRow.Add( new Label( "Node Properties", _propertiesPanel ) );

		_propertiesContent = new Widget( _propertiesPanel );
		_propertiesContent.Layout = Layout.Column();
		_propertiesContent.Layout.Spacing = 0;
		_propertiesPanel.Layout.Add( _propertiesContent, 1 );

		_graphView.OnSelectionChanged += UpdatePropertiesPanel;
		_graphView.OnSaveRequested = SaveAsset;

		Show();
	}

	public void AssetOpen( Asset asset )
	{
		_asset = asset;
		_dialogueData = asset.LoadResource<DialogueData>();
		_graphView.LoadDialogue( _dialogueData, asset.Path );

		Title = $"Dialogue Editor — {asset.Name}";
		Focus();
	}

	public void SelectMember( string memberName ) {}

	private void AddNewNode()
	{
		if ( _graphView.Graph == null ) return;

		var center = _graphView.Center;
		_graphView.CreateNewNode( new DialogueNodeType(), center );
	}

	private void SaveAsset()
	{
		if ( _asset == null || _dialogueData == null ) return;
		_asset.SaveToDisk( _dialogueData );
	}

	protected override bool OnClose()
	{
		SaveAsset();
		return true;
	}

	private void UpdatePropertiesPanel()
	{
		foreach ( var child in _propertiesContent.Children.ToArray() )
			child.Destroy();

		var selected = _graphView.SelectedItems
			.OfType<NodeUI>()
			.Select( n => n.Node as DialogueNodeData )
			.Where( n => n != null )
			.FirstOrDefault();

		if ( selected == null )
		{
			var hint = new Label( "Select a node to edit its properties", _propertiesContent );
			hint.SetStyles( "color: #666; margin: 12px; font-style: italic;" );
			_propertiesContent.Layout.Add( hint );
			return;
		}

		ShowNodeProperties( selected );
	}

	private void ShowNodeProperties( DialogueNodeData nodeData )
	{
		var serialized = EditorTypeLibrary.GetSerializedObject( nodeData.DialogueNode );

		serialized.OnPropertyChanged += _ =>
		{
			nodeData.FireChanged();
			// SaveAsset();
		};

		var sheet = new ControlSheet();
		sheet.AddObject( serialized );
		_propertiesContent.Layout.Add( sheet );
	}
}
