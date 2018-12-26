namespace GetIt
{
    /// <summary>
    /// Defines a geometry with a fill color and path data using path markup syntax. 
    /// </summary>
    public class GeometryPath
    {
        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public GeometryPath(RGBA fillColor, string data)
        {
            FillColor = fillColor;
            Data = data;
        }

        /// <summary>
        /// Color that is used to fill the path.
        /// </summary>
        public RGBA FillColor { get; }

        /// <summary>
        /// Data in path markup syntax that defines the path.
        /// </summary>
        public string Data { get; }
    }
}