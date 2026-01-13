using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GhJSON.Grasshopper.TestComponents
{
    public sealed class GhJsonTestInfo : GH_AssemblyInfo
    {
        public override string Name => "GhJSON Test Components";

        public override Bitmap? Icon => null;

        public override string Description => "Grasshopper components for GhJSON integration testing";

        public override Guid Id => new Guid("7D7B6E11-3F7F-4E57-9B2D-8C2BDE2B7D91");

        public override string AuthorName => "GhJSON";

        public override string AuthorContact => "https://github.com/architects-toolkit/ghjson-dotnet";
    }
}
