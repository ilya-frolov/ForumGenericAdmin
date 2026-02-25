using AutoMapper;

namespace Dino.Mvc.Common.Helpers.AutoMapper
{
	public abstract class BaseAutoMapperConfig
	{
		private MapperConfiguration MapperConfiguration { get; set; }

		protected void RegisterConfiguration(MapperConfiguration configuration)
		{
			MapperConfiguration = configuration;
		}

		protected abstract void RegisterMappings();

		public IMapper CreateMapper()
		{
			return MapperConfiguration.CreateMapper();
		}
	}
}
