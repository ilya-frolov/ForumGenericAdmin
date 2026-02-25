using AutoMapper;

namespace Dino.CoreMvc.Admin.AutoMapper
{
    public interface IAutoMapperSelfConfigurator
    {
        void AutoMappingConfiguration(Profile profile);
    }
}