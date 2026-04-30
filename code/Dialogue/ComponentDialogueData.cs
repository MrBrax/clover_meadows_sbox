using System;
using System.Globalization;

namespace Clover.Dialogue;

public sealed class ComponentDialogueData
{
	[Property] public DialogueData BaseDialogue { get; set; }

	[Property] public string NpcSpeakerName { get; set; } = "";
	[Property] public string PlayerSpeakerName { get; set; } = "";

	// [Property] public bool IgnoreIfQuestCompleted { get; set; } = false;
	// [Property, ShowIf( nameof( IgnoreIfQuestCompleted ), true )] public QuestData QuestToCheck { get; set; }
	// [Property, ShowIf( nameof( IgnoreIfQuestCompleted ), true )] public GameObject QuestionMarkIndicator { get; set; }

	[Property] public Dictionary<string, string> PropertyOverrides { get; set; } = new();

	// [Property]
	// public Dictionary<string, string> StaticMethodBindings { get; set; } = new();

	[Property] public Dictionary<string, Doo> Doos { get; set; } = new();

	public string ResolveSpeakerName( bool isPlayerLine, string fallbackNpcSpeaker )
	{
		if ( isPlayerLine )
		{
			return string.IsNullOrWhiteSpace( PlayerSpeakerName ) ? "You" : PlayerSpeakerName;
		}

		if ( !string.IsNullOrWhiteSpace( NpcSpeakerName ) )
		{
			return NpcSpeakerName;
		}

		return string.IsNullOrWhiteSpace( fallbackNpcSpeaker ) ? "NPC" : fallbackNpcSpeaker;
	}

	public bool TryGetPropertyOverride( string key, out string value )
	{
		value = null;
		if ( string.IsNullOrWhiteSpace( key ) ) return false;
		return PropertyOverrides.TryGetValue( key, out value );
	}

	public bool ApplyPropertyOverrides( object target )
	{
		if ( target == null ) return false;
		if ( PropertyOverrides.Count == 0 ) return false;

		var typeDescription = TypeLibrary.GetType( target.GetType() );
		if ( typeDescription == null ) return false;

		var changed = false;

		foreach ( var pair in PropertyOverrides )
		{
			if ( string.IsNullOrWhiteSpace( pair.Key ) ) continue;

			var property = typeDescription.Properties.FirstOrDefault( p => p.Name == pair.Key );
			if ( property == null ) continue;

			if ( !TryConvertStringValue( pair.Value, property.PropertyType, out var convertedValue ) )
			{
				continue;
			}

			property.SetValue( target, convertedValue );
			changed = true;
		}

		return changed;
	}

	/*public bool TryInvokeStaticMethod( string key, params object[] args )
	{
		if ( string.IsNullOrWhiteSpace( key ) ) return false;
		if ( !StaticMethodBindings.TryGetValue( key, out var methodPath ) ) return false;

		return TryInvokeStaticMethodByPath( methodPath, args );
	}*/

	public static bool TryInvokeStaticMethodByPath( string methodPath, params object[] args )
	{
		if ( string.IsNullOrWhiteSpace( methodPath ) ) return false;

		var separatorIndex = methodPath.LastIndexOf( '.' );
		if ( separatorIndex <= 0 || separatorIndex >= methodPath.Length - 1 ) return false;

		var typeName = methodPath[..separatorIndex];
		var methodName = methodPath[(separatorIndex + 1)..];

		var typeDescription = TypeLibrary.GetType( typeName );
		if ( typeDescription == null ) return false;

		var callArgs = args ?? Array.Empty<object>();

		foreach ( var method in typeDescription.Methods )
		{
			if ( !method.IsNamed( methodName ) ) continue;
			if ( method.Parameters.Length != callArgs.Length ) continue;

			try
			{
				method.Invoke( null, callArgs );
				return true;
			}
			catch
			{
				continue;
			}
		}

		return false;
	}

	private static bool TryConvertStringValue( string rawValue, Type targetType, out object value )
	{
		value = null;
		if ( targetType == null ) return false;

		if ( targetType == typeof( string ) )
		{
			value = rawValue ?? "";
			return true;
		}

		if ( targetType == typeof( bool ) && bool.TryParse( rawValue, out var boolResult ) )
		{
			value = boolResult;
			return true;
		}

		if ( targetType == typeof( int ) && int.TryParse( rawValue, NumberStyles.Integer,
			    CultureInfo.InvariantCulture, out var intResult ) )
		{
			value = intResult;
			return true;
		}

		if ( targetType == typeof( float ) && float.TryParse( rawValue, NumberStyles.Float,
			    CultureInfo.InvariantCulture, out var floatResult ) )
		{
			value = floatResult;
			return true;
		}

		if ( targetType == typeof( double ) && double.TryParse( rawValue, NumberStyles.Float,
			    CultureInfo.InvariantCulture, out var doubleResult ) )
		{
			value = doubleResult;
			return true;
		}

		if ( targetType.IsEnum && Enum.TryParse( targetType, rawValue, true, out var enumResult ) )
		{
			value = enumResult;
			return true;
		}

		return false;
	}
}
