using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins;
using System.Linq;
using System.Threading.Tasks;

public class StructuralPlugin
{
    private readonly MongoDBHandler _dbHandler;

    public StructuralPlugin(MongoDBHandler dbHandler)
    {
        _dbHandler = dbHandler;
    }

    [KernelFunction]
    public async Task<string> GetDesignCapacity(string element, string formulaName)
    {
        var properties = await _dbHandler.GetElementPropertiesAsync(element);
        if (properties == null)
            return $"{element} does not exist.";

        // 🟢 Calculate() 호출
        var (result, steps, errorMessage) = StructuralFormulaExecutor.Calculate(formulaName, properties);

        // 🛑 오류가 있는 경우
        if (errorMessage != null)
            return $"{element}: {errorMessage}";

        // ✅ 정상적으로 결과 반환
        return $"{element}'s {formulaName} capacity: {result:F2} kN";
    }
}
