using System;
using System.Globalization;
using Clover.Data;

namespace Clover;

[CustomEditor( typeof(DatePeriod) )]
public class DatePeriodEditor : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	public DatePeriodEditor( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();
		Layout.Spacing = 10;

		PaintBackground = false;

		// Serialize the property as a MyClass object
		var serializedObject = property.GetValue<DatePeriod>().GetSerialized();
		if ( serializedObject is null )
		{
			Log.Error( "Failed to get serialized object" );
			return;
		}

		// Get the Color and Name properties from the serialized object
		serializedObject.TryGetProperty( nameof(DatePeriod.StartMonth), out var startMonth );
		serializedObject.TryGetProperty( nameof(DatePeriod.StartDay), out var startDay );

		serializedObject.TryGetProperty( nameof(DatePeriod.EndMonth), out var endMonth );
		serializedObject.TryGetProperty( nameof(DatePeriod.EndDay), out var endDay );

		// Add some Controls to the Layout, both referencing their serialized properties
		// Layout.Add(new ColorSwatchWidget(color) { FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
		// Layout.Add(new StringControlWidget(name) { HorizontalSizeMode = SizeMode.Default });

		Layout.Add( new MonthControlWidget( startMonth ) { HorizontalSizeMode = SizeMode.Expand } );
		Layout.Add( new IntegerControlWidget( startDay )
		{
			HorizontalSizeMode = SizeMode.Default, Range = new Vector2( 1, 31 )
		} );

		Layout.Add( new Label( "to" ) { HorizontalSizeMode = SizeMode.Default } );

		Layout.Add( new MonthControlWidget( endMonth ) { HorizontalSizeMode = SizeMode.Expand } );
		Layout.Add( new IntegerControlWidget( endDay )
		{
			HorizontalSizeMode = SizeMode.Default, Range = new Vector2( 1, 31 )
		} );

		/*var row2 = Layout.Row();

		row2.Add( new Label(
			$"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName( startMonth.GetValue<int>() )} {startDay.GetValue<int>()}" )
		);

		Layout.Add( row2 );*/
	}
}

public class MonthControlWidget : DropdownControlWidget<int>
{
	public MonthControlWidget( SerializedProperty property ) : base( property )
	{
		// Layout = Layout.Row();
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		// yield return new Entry() { Label = "None", Value = 0 };
		for ( int i = 1; i <= 12; i++ )
		{
			yield return new Entry()
			{
				Label = new DateTime( 2021, i, 1 ).ToString( "MMMM", CultureInfo.InvariantCulture ), Value = i
			};
		}
	}

	protected override void PaintControl()
	{
		var value = SerializedProperty.GetValue<int>();
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = LocalRect;

		rect = rect.Shrink( 8, 0 );

		//var e = enumDesc.GetEntry( value );

		//if ( !string.IsNullOrEmpty( e.Icon ) )
		//{
		//	Paint.SetPen( color.WithAlpha( 0.5f ) );
		//	var i = Paint.DrawIcon( rect, e.Icon, 16, TextFlag.LeftCenter );
		//	rect.Left += i.Width + 8;
		//}

		Paint.SetPen( color );
		Paint.SetDefaultFont();

		var text = value switch
		{
			0 => "ERROR",
			_ => new DateTime( 2021, value, 1 ).ToString( "MMMM", CultureInfo.InvariantCulture )
		};

		Paint.DrawText( rect, text );

		Paint.SetPen( color );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}
}
