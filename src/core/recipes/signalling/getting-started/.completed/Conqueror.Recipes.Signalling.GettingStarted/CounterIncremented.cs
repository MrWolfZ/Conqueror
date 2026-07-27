namespace Conqueror.Recipes.Signalling.GettingStarted;

[Signal]
public partial record CounterIncremented(string CounterName, int NewValue);
