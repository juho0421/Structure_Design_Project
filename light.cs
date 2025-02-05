using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace SK_project3
{
    public class LightsPlugin
    {
        // Mock data for the lights
        private readonly List<LightModel> lights = new()
        {
            new LightModel { Id = 1, Name = "LED Light", IsOn = false, Brightness = 100, Hex = "FF0000" },
            new LightModel { Id = 2, Name = "Porch light", IsOn = false, Brightness = 50, Hex = "00FF00" },
            new LightModel { Id = 3, Name = "Chandelier", IsOn = true, Brightness = 75, Hex = "0000FF" }
        };

        [KernelFunction("get_lights")]
        [Description("Gets a list of lights and their current state")]
        [return: Description("An array of lights")]
        public Task<List<LightModel>> GetLightsAsync()
        {
            return Task.FromResult(lights);
        }

        [KernelFunction("get_state")]
        [Description("Gets the state of a particular light")]
        [return: Description("The state of the light")]
        public Task<LightModel?> GetStateAsync([Description("The ID of the light")] int id)
        {
            return Task.FromResult(lights.FirstOrDefault(light => light.Id == id));
        }

        [KernelFunction("change_state")]
        [Description("Changes the state of the light")]
        [return: Description("The updated state of the light; will return null if the light does not exist")]
        public Task<LightModel?> ChangeStateAsync(
            [Description("The ID of the light")] int id,
            [Description("The new state of the light")] LightModel lightModel)
        {
            var light = lights.FirstOrDefault(light => light.Id == id);

            if (light == null)
            {
                return Task.FromResult<LightModel?>(null);
            }

            // Update the light with the new state
            light.IsOn = lightModel.IsOn;
            light.Brightness = lightModel.Brightness;
            light.Hex = lightModel.Hex;

            return Task.FromResult<LightModel?>(light);
        }
    }


    public class LightModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("is_on")]
        public bool? IsOn { get; set; }

        [JsonPropertyName("brightness")]
        public byte? Brightness { get; set; }

        [JsonPropertyName("hex")]
        public string? Hex { get; set; }
    }
}
