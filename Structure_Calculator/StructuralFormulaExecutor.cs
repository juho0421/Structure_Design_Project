using System;
using System.Collections.Generic;
using System.Linq;

public static class StructuralFormulaExecutor
{
    public static (double Result, Dictionary<string, double> Steps, string ErrorMessage) Calculate(string formulaType, List<ElementProperty> properties)
    {
        var dict = properties.ToDictionary(p => p.Name, p => p.Value);
        var steps = new Dictionary<string, double>();

        try
        {
            switch (formulaType)
            {
                case "KDS_Compression_Capacity":
                    steps["A_g"] = dict["B"] * dict["H"];
                    steps["A_st"] = dict["num_bar"] * dict["area_bar"];
                    steps["phi_comp"] = 0.65; // Compressive reduction factor
                    steps["alpha_1"] = 0.85; // Equivalent concrete force location factor
                    steps["beta_1"] = 0.8;

                    steps["phi_Pn_max"] = 0.80 * steps["phi_comp"] *
                        ((steps["alpha_1"] * dict["fck"] * (steps["A_g"] - steps["A_st"])) +
                        (dict["fy"] * steps["A_st"])) / 1000;

                    return (steps["phi_Pn_max"], steps, null);

                case "CSA_Compression_Capacity":
                    steps["A_g"] = dict["B"] * dict["H"];
                    steps["A_st"] = dict["num_bar"] * dict["area_bar"];
                    steps["alpha_1"] = Math.Max(0.85 - 0.0015 * dict["fck"], 0.67);
                    steps["beta_1"] = Math.Max(0.97 - 0.0025 * dict["fck"], 0.67);
                    steps["phi_c"] = 0.65; // Concrete reduction factor
                    steps["phi_s"] = 0.85; // Steel rebar reduction factor
                    steps["phi_comp"] = 0.65; // Compressive reduction factor

                    steps["phi_Pr_max"] = 0.80 * (
                        (steps["alpha_1"] * steps["phi_c"] * dict["fck"] * (steps["A_g"] - steps["A_st"])) +
                        (steps["phi_s"] * dict["fy"] * steps["A_st"])) / 1000;

                    return (steps["phi_Pr_max"], steps, null);

                case "KDS_Shear_Capacity":
                    steps["phi_shear"] = 0.75; // Shear reduction factor
                    steps["A_v"] = dict["num_legs"] * dict["area_bar"]; // Total stirrup area (2 bars)
                    steps["phi_V_c"] = steps["phi_shear"] * (1.0 / 6.0) * dict["lambda_conc"] * Math.Sqrt(dict["fck"]) * dict["b_w"] * dict["d"] / 1000;
                    steps["phi_V_s"] = steps["phi_shear"] * (steps["A_v"] * dict["fy"] * dict["d"] / dict["s_bar"]) / 1000;
                    steps["phi_V_s_max"] = steps["phi_shear"] * 0.2 * (1 - dict["fck"] / 250) * dict["fck"] * dict["b_w"] * dict["d"] / 1000;
                    steps["phi_V_n"] = steps["phi_V_c"] + Math.Min(steps["phi_V_s"], steps["phi_V_s_max"]);

                    return (steps["phi_V_n"], steps, null);

                case "CSA_Shear_Capacity":
                    steps["d_v"] = Math.Max(0.9 * dict["d"], 0.72 * dict["h"]); // Effective Shear Depth
                    steps["beta"] = 0.18; // Shear resistance factor
                    steps["theta"] = 35; // Default shear crack angle (degrees)
                    steps["cot_value"] = Math.Cos(steps["theta"] * Math.PI / 180.0) / Math.Sin(steps["theta"] * Math.PI / 180.0); // Cotangent calculation
                    steps["phi_conc"] = 0.65; // Concrete reduction factor
                    steps["phi_steel"] = 0.85; // Steel Rebar reduction factor
                    steps["A_v"] = dict["num_legs"] * dict["area_bar"]; // Total stirrup area (2 bars)

                    steps["phi_V_c"] = steps["phi_conc"] * dict["lambda_conc"] * steps["beta"] * Math.Sqrt(dict["fck"]) * dict["b_w"] * steps["d_v"] / 1000;
                    steps["phi_V_s"] = steps["phi_steel"] * steps["A_v"] * dict["fy"] * steps["d_v"] * steps["cot_value"] / dict["s_bar"] / 1000;
                    steps["phi_V_r"] = steps["phi_V_c"] + steps["phi_V_s"];
                    steps["phi_V_r_max"] = 0.25 * steps["phi_conc"] * dict["fck"] * dict["b_w"] * steps["d_v"] / 1000;

                    return (Math.Min(steps["phi_V_r"], steps["phi_V_r_max"]), steps, null);

                default:
                    return (0, null, "Unsupported formula type.");
            }
        }
        catch (KeyNotFoundException keyEx)
        {
            return (0, null, $"Calculation failed. Missing required variable: {keyEx.Message}");
        }
        catch (Exception ex)
        {
            return (0, null, $"Calculation failed due to an error: {ex.Message}");
        }
    }
}
