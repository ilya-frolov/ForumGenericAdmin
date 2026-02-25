using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Dino.Mvc.Common.Helpers.AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Dino.Mvc.Common.Helpers
{
    public class DinoServerResponse<T>
    {
        public bool Result { get; set; }
        public string Error { get; set; }
        public T Data { get; set; }
    }

	public class DinoController : Controller
	{
        private IMapper _mapper;
        protected IMapper Mapper => _mapper ??= HttpContext?.RequestServices.GetService<IMapper>();

        private static BaseAutoMapperConfig _config = null;
		public static void SetAutoMapperConfig(BaseAutoMapperConfig config)
		{
			_config = config;
		}

        protected TDest Map<TSource, TDest>(TSource source, Action<IMappingOperationOptions<TSource, TDest>> opts = null)
		{
            var result = (opts != null) ? Mapper.Map<TSource, TDest>(source, opts) : Mapper.Map<TSource, TDest>(source);

			return result;
		}

		protected TDest Map<TSource, TDest>(TSource source, TDest dest, Action<IMappingOperationOptions<TSource, TDest>> opts = null)
		{
            var result = (opts != null) ? Mapper.Map<TSource, TDest>(source, dest, opts) : Mapper.Map<TSource, TDest>(source, dest);

            return result;
		}

        protected List<TDest> MapList<TSource, TDest>(List<TSource> source, Action<IMappingOperationOptions<List<TSource>, List<TDest>>> opts = null)
        {
            var result = (opts != null) ? Mapper.Map<List<TSource>, List<TDest>>(source, opts) : Mapper.Map<List<TSource>, List<TDest>>(source);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="allowGet">Obsolete!!!!</param>
        /// <param name="camelCaseNames"></param>
        /// <param name="useJsonNet"></param>
        /// <returns></returns>
        protected JsonResult CreateJsonResponse(dynamic data, bool allowGet = true, bool camelCaseNames = true, bool useJsonNet = true)
        {
            return JsonWithProperties(data, camelCaseNames, useJsonNet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="data"></param>
        /// <param name="error"></param>
        /// <param name="allowGet">Obsolete!!!!</param>
        /// <param name="camelCaseNames"></param>
        /// <param name="useJsonNet"></param>
        /// <returns></returns>
        protected JsonResult CreateJsonResponse(bool result, dynamic data, string error, bool allowGet = true, bool camelCaseNames = true, bool useJsonNet = true)
        {
            var jsonData = new
            {
                Result = result,
                Error = error,
                Data = data
            };

			return JsonWithProperties(jsonData, camelCaseNames, useJsonNet);
		}

		protected JsonResult JsonWithProperties(object data, bool camelCaseNames = true, bool useJsonNet = true)
		{
            if (useJsonNet)
            {
                return Json(data, new JsonSerializerSettings
                {
                    ContractResolver = (camelCaseNames ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()),
                });
            }

            return Json(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = (camelCaseNames ? JsonNamingPolicy.CamelCase : null)
            });
        }

        protected string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress.ToString();
        }
	}
}
