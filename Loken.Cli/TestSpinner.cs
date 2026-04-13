using System;
using System.Threading.Tasks;

namespace Loken.Cli;

public static class TestSpinner
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("Testing spinner functionality...");
        Console.WriteLine("This line should stay above the spinner.");
        
        // Test 1: Basic spinner
        await using var spinner1 = SpinnerExtensions.ShowAssistantSpinner("Thinking...");
        await Task.Delay(2000);
        
        Console.WriteLine("\nSpinner test 1 completed.");
        
        // Test 2: Spinner with message update
        await using var spinner2 = SpinnerExtensions.ShowAssistantSpinner("Starting process...");
        await Task.Delay(1000);
        spinner2.UpdateMessage("Processing data...");
        await Task.Delay(1000);
        spinner2.UpdateMessage("Finishing up...");
        await Task.Delay(1000);
        
        Console.WriteLine("\nSpinner test 2 completed.");
        
        // Test 3: Tool spinner
        await using var spinner3 = SpinnerExtensions.ShowToolSpinner("bash");
        await Task.Delay(2000);
        
        Console.WriteLine("\nSpinner test 3 completed.");
        
        // Test 4: WithInlineSpinnerAsync extension
        var result = await SpinnerExtensions.WithInlineSpinnerAsync(
            "Calculating...",
            async () => {
                await Task.Delay(1500);
                return 42;
            });
        
        Console.WriteLine($"\nCalculation result: {result}");
        Console.WriteLine("All spinner tests completed successfully!");
    }
}