namespace Structure_Calculator.Utilities
{
    public static class StructuralFormulaExecutor
    {
        public static double Calculate(string formulaName, Dictionary<string, double> properties)
        {
            return formulaName switch
            {
                "KDS_Compression_Capacity" => CalculateKDSCompressionCapacity(properties),
                "CSA_Compression_Capacity" => CalculateCSACompressionCapacity(properties),
                "KDS_Shear_Capacity" => CalculateKDSShearCapacity(properties),
                "CSA_Shear_Capacity" => CalculateCSAShearCapacity(properties),
                _ => throw new InvalidOperationException($"Unknown formula: {formulaName}")
            };
        }

        // 1. KDS_Compression_Capacity
        private static double CalculateKDSCompressionCapacity(Dictionary<string, double> p)
        {
            double A_g = p["b_w"] * p["h"];
            double A_st = p["num_legs"] * p["area_bar"];
            double phi_comp = 0.65;
            double alpha_1 = 0.85;
            double beta_1 = 0.8;

            double phi_Pn_max = (0.80) * phi_comp * ((alpha_1 * p["fck"]) * (A_g - A_st) + p["fy"] * A_st) / 1000;
            return phi_Pn_max;
        }

        // 2. CSA_Compression_Capacity
        private static double CalculateCSACompressionCapacity(Dictionary<string, double> p)
        {
            double A_g = p["b_w"] * p["h"];
            double A_st = p["num_legs"] * p["area_bar"];
            double alpha_1 = Math.Max(0.85 - 0.0015 * p["fck"], 0.67);
            double beta_1 = Math.Max(0.97 - 0.0025 * p["fck"], 0.67);
            double phi_c = 0.65; // Concrete reduction factor
            double phi_s = 0.85; // Steel rebar reduction factor

            double phi_Pr_max = (0.80) * ((alpha_1 * phi_c * p["fck"] * (A_g - A_st)) + (phi_s * p["fy"] * A_st)) / 1000;
            return phi_Pr_max;
        }

        // 3. KDS_Shear_Capacity
        private static double CalculateKDSShearCapacity(Dictionary<string, double> p)
        {
            double phi_shear = 0.75;
            double A_v = p["num_legs"] * p["area_bar"];
            double phi_V_c = (phi_shear * (1.0 / 6.0) * p["lambda_conc"] * Math.Sqrt(p["fck"]) * p["b_w"] * p["d"]) / 1000;
            double phi_V_s = (phi_shear * (A_v * p["fy"] * p["d"]) / p["s_bar"]) / 1000;
            double phi_V_s_max = (phi_shear * 0.2 * (1 - p["fck"] / 250) * p["fck"] * p["b_w"] * p["d"]) / 1000;

            double phi_V_n = phi_V_c + Math.Min(phi_V_s, phi_V_s_max);
            return phi_V_n;
        }

        // 4. CSA_Shear_Capacity
        private static double CalculateCSAShearCapacity(Dictionary<string, double> p)
        {
            double d_v = Math.Max(0.9 * p["d"], 0.72 * p["h"]);
            double beta = 0.18;
            double theta = 35.0;
            double cot_theta = 1 / Math.Tan(theta * Math.PI / 180.0); // cot(θ)
            double phi_conc = 0.65;
            double phi_steel = 0.85;

            double A_v = p["num_legs"] * p["area_bar"];
            double phi_V_c = (phi_conc * p["lambda_conc"] * beta * Math.Sqrt(p["fck"]) * p["b_w"] * d_v) / 1000;
            double phi_V_s = (phi_steel * A_v * p["fy"] * d_v * cot_theta / p["s_bar"]) / 1000;
            double phi_V_r = phi_V_c + phi_V_s;
            double phi_V_r_max = (0.25 * phi_conc * p["fck"] * p["b_w"] * d_v) / 1000;

            return Math.Min(phi_V_r, phi_V_r_max);
        }
    }
}
