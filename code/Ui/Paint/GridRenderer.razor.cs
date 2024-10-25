using Sandbox.UI;

namespace Clover.Ui;

public partial class GridRenderer
{
	[Property] public int TotalGridDivisions { get; set; } = 32;
	[Property] public int GridDivisionSize { get; set; } = 8;
	[Property] public int GridLineThickness { get; set; } = 1;

	[Property] public Color GridColor { get; set; } = Color.Black;
	[Property] public Color GridSubdivisionColor { get; set; } = Color.Gray;

	private Material _material => Material.UI.Basic;

	public override void DrawBackground( ref RenderState state )
	{
		base.DrawBackground( ref state );

		var baseOffset = new Vector2( Box.Left, Box.Top );
		var size = new Vector2( Box.Rect.Width, Box.Rect.Height );

		var gridCellSize = size.x / TotalGridDivisions;

		/*
			DRAWING USAGE:
			var rect = new Rect( 0, 0, 100, 100 );
			Graphics.DrawQuad( rect, Material.UI.Box, Color.Black );
		*/

		var bgAttribs = new RenderAttributes();

		/*bgAttribs.SetCombo( "D_BACKGROUND_IMAGE", 0 );
		bgAttribs.SetComboEnum( "D_BLENDMODE", BlendMode.Normal );
		bgAttribs.Set( "BgTint", Color.White );

		bgAttribs.Set( "HasBorder", 1 );
		bgAttribs.Set( "BorderSize", 5 );
		bgAttribs.Set( "BorderColorL", Color.Red );*/

		bgAttribs.SetComboEnum( "D_BLENDMODE", BlendMode.Normal );

		bgAttribs.Set( "Texture", Texture.White );

		// bgAttribs.Set( "BgTint", Color.Red );

		// bgAttribs.SetCombo( "D_BACKGROUND_IMAGE", 1 );

		// Draw grid
		for ( int i = 0; i < TotalGridDivisions; i++ )
		{
			var x = (int)(baseOffset.x + i * gridCellSize);
			var y = (int)(baseOffset.y + i * gridCellSize);

			var color = i % GridDivisionSize == 0 ? GridColor : GridSubdivisionColor;

			// Draw vertical line
			Graphics.DrawQuad( new Rect( x, baseOffset.y, GridLineThickness, size.y ), _material, color,
				bgAttribs );

			// Draw horizontal line
			Graphics.DrawQuad( new Rect( baseOffset.x, y, size.x, GridLineThickness ), _material, color,
				bgAttribs );
		}
	}
}
