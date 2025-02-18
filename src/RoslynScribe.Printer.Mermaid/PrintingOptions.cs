namespace RoslynScribe.Printer.Mermaid
{
    public class PrintingOptions
    {
        public string Title { get; set; }

        public string ScribeFlowId { get; set; }

        public string OutputPath { get; set; }

        public string ScribeResultPath { get; set; }

        /// <summary>
        /// Declares the direction of the Flowchart e.g. LR, TD
        /// </summary>
        public string Mermaid_FlowChartOrientation { get; set; }
    }
}
