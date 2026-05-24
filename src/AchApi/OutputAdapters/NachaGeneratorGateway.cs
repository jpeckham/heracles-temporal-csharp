using AchApi.Entities;
using AchApi.Gateways;
using AchApi.Services;

namespace AchApi.OutputAdapters;

public class NachaGeneratorGateway : INachaGeneratorGateway
{
    public string Generate(AchFile file) => NachaFileGenerator.Generate(file);
}
