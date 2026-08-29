using System;

// Marks a method as a Unity Event target (i.e. it may be referenced by name for an event callback
// in a prefab or scene i.e. UnityEvent, Animation Event, LODs). Methods with this attribute will not have their names obfuscated.
[AttributeUsage(AttributeTargets.Method)]
public class FlexEventAttribute : Attribute
{
}
