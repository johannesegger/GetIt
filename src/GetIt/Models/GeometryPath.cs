namespace GetIt.Models
{
    public class GeometryPath
    {
        public GeometryPath(RGB fill, string data)
        {
            Fill = fill;
            Data = data;
        }

        public RGB Fill { get; }
        public string Data { get; }
    }
}