namespace HNTAS.Core.Api.Helpers
{
    public static class HeatNetworkHelper
    {
        public static string GetStageFromPhase(string phase)
        {
            return phase switch
            {
                "Feasibility" => "Concept design",
                "Design" => "Developed design, technical design",
                "Construction" => "Construction design, installation, commissioning",
                "Operational" => "Operation, maintenance, ongoing monitoring",
                _ => "NA"
            };
        }
    }
}
