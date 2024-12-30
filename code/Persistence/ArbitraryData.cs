using System;

namespace Braxnet.Persistence;

/// <summary>
///   Marks a property as arbitrary data, which automatically gets serialized and deserialized when saving and loading
/// </summary>
public class ArbitraryDataAttribute : Attribute
{
}
