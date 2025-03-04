using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins;
using System.Threading.Tasks;

public class DataUpdatePlugin
{
    private readonly MongoDBHandler _dbHandler;

    public DataUpdatePlugin(MongoDBHandler dbHandler)
    {
        _dbHandler = dbHandler;
    }

    [KernelFunction]
    public async Task<string> UpdateElementProperty(string element, string property, double value)
    {
        var properties = await _dbHandler.GetElementPropertiesAsync(element);
        if (properties == null)
            return $"{element} does not exist.";

        var propertyExists = properties.Any(p => p.Name.Equals(property, StringComparison.OrdinalIgnoreCase));
        if (!propertyExists)
            return $"{element} does not contain '{property}'.";

        bool updated = await _dbHandler.UpdateElementPropertyAsync(element, property, value);
        return updated
            ? $"{element}'s '{property}' updated to {value}."
            : $"Failed to update {element}.";
    }
}
