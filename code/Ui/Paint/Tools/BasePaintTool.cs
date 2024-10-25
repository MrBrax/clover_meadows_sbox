using Sandbox.UI;

namespace Clover.Ui.Tools;

public class BasePaintTool
{
	protected PaintUi PaintUi;

	public BasePaintTool( PaintUi paintUi )
	{
		PaintUi = paintUi;
	}

	public virtual void OnMouseDown( MouseButtons buttons )
	{
	}

	public virtual void OnMouseUp()
	{
	}

	public virtual void OnMouseMove( Vector2Int position )
	{
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void OnSelected()
	{
	}

	public virtual void OnDeselected()
	{
	}

	public virtual void DrawCrosshair( Panel crosshair, Vector2Int position )
	{
	}
}
