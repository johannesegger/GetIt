namespace GetIt.Models
{
    public class GeometryPath
    {
        public GeometryPath(RGBA fill, string data)
        {
            Fill = fill;
            Data = data;
        }

        public RGBA Fill { get; }
        public string Data { get; }
    }
}