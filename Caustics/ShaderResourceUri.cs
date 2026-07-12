namespace Caustics;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/Caustics;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
