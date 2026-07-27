namespace Conqueror.Recipes.Signalling.TestingHandlers;

[Signal]
public partial record CounterIncremented(string CounterName, int NewValue);
