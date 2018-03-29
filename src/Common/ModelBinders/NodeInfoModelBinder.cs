using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Common.CLightning;

namespace Common.ModelBinders
{
	public class NodeInfoModelBinder : IModelBinder
	{
		#region IModelBinder Members

		public Task BindModelAsync(ModelBindingContext bindingContext)
		{
			if(!typeof(NodeInfo).GetTypeInfo().IsAssignableFrom(bindingContext.ModelType))
			{
				return Task.CompletedTask;
			}

			ValueProviderResult val = bindingContext.ValueProvider.GetValue(
				bindingContext.ModelName);
			if(val == null)
			{
				return Task.CompletedTask;
			}

			string key = val.FirstValue as string;
			if(key == null)
			{
				bindingContext.Model = null;
				return Task.CompletedTask;
			}

			if(NodeInfo.TryParse(key, out var nodeInfo))
				bindingContext.Result = ModelBindingResult.Success(nodeInfo);
			else
				bindingContext.Result = ModelBindingResult.Failed();
			return Task.CompletedTask;
		}

		#endregion
	}
}
