using AchApi.Entities;

namespace AchApi.Gateways;

public interface INachaGeneratorGateway
{
    string Generate(AchFile file);
}
