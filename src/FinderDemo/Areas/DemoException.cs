namespace FinderDemo.Areas;

/// <summary>
/// A situation a feature cannot continue from, but the demo can: <see cref="MainMenu"/> shows the message and
/// returns to the menu (e.g. a select additional without options).
/// </summary>
internal sealed class DemoException(string message) : Exception(message);
